using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using spapp_backend.Core;
using spapp_backend.Core.Controllers;
using spapp_backend.Core.Dtos;
using spapp_backend.Core.Enums;
using spapp_backend.Core.Models;
using spapp_backend.Db;
using spapp_backend.Modules.Crowdfunding.Dtos;
using spapp_backend.Modules.Crowdfunding.Models;
using spapp_backend.Utils;
using System.IO;
using System.Net;
using System.Security.Claims;

namespace spapp_backend.Modules.Crowdfunding.Controllers
{
  public class CrowdfundController : IWebController
  {
    public static readonly double CREATE_COMPANY_PRICE = 4;
    static readonly int MAX_COMMENTS_PER_20_MINUTES = 25;

    [ProducesResponseType(typeof(ItemList<CrowdfundCompanyItemDto>), (int)HttpStatusCode.OK)]
    public async Task<IResult> GetCompanies(SQLiteDbContext db, int from, int count, string sort, string? filter, uint? user, bool strip, MCServer mcs)
    {
      if (count <= 0 || count > 50)
      {
        return new ErrorResult(ResponseError.Unknown, HttpStatusCode.BadRequest);
      }

      IQueryable<CrowdfundCompany> q = filter switch
      {
        "over" => db.CrowdCompanies.Where(c => c.IsOver && c.CurrentAmount >= c.Goal && c.Mcs == mcs),
        "cancelled" => db.CrowdCompanies.Where(c => c.IsOver && c.CurrentAmount < c.Goal && c.Mcs == mcs),
        "active" => db.CrowdCompanies.Where(c => !c.IsOver && c.Mcs == mcs),
        _ => db.CrowdCompanies.Where(c => c.Mcs == mcs),
      };

      if (user != null)
      {
        q = q.Include(c => c.User).Where(c => c.User.Id == user);
      }

      var itemCount = await q.CountAsync();

      q = sort switch
      {
        "dateAsc" => q.OrderBy(c => c.CreatedAt),
        "dateDesc" => q.OrderByDescending(c => c.CreatedAt),
        "amountAsc" => q.OrderBy(c => c.CurrentAmount / c.Goal),
        "amountDesc" => q.OrderByDescending(c => c.CurrentAmount / c.Goal),
        _ => q,
      };

      var items = await q
        .Skip(from)
        .Take(count)
        .Include(c => c.User)
        .Include(c => c.Preview)
        .Include(c => c.Images)
        .Select(c => new CrowdfundCompanyItemDto
        {
          Id = c.Id,
          User = new UserInfoDto
          {
            Id = c.User.Id,
            UserName = c.User.UserName ?? "",
            MinecraftUuid = c.User.MinecraftUUID
          },
          Title = c.Title,
          ShortDescription = c.ShortDescription,
          Preview = c.Preview == null ? null : c.Preview.FullPath,
          Images = strip ? Array.Empty<string>() : c.Images.Select(i => i.FullPath).ToArray(),
          Content = strip ? c.Content : "",
          CurrentAmount = c.CurrentAmount,
          Goal = c.Goal,
          IsOver = c.IsOver,
          EndDate = c.EndDate,
          CommentCount = c.Comments.Count,
          UpdatedAt = c.UpdatedAt
        })
        .ToListAsync();

      return Results.Ok(new ItemList<CrowdfundCompanyItemDto> { Items = items, Count = itemCount });
    }

    [ProducesResponseType(typeof(CrowdfundCompanyItemDto), (int)HttpStatusCode.OK)]
    public async Task<IResult> GetCompany(SQLiteDbContext db, uint id, MCServer mcs)
    {

      var company = await db.CrowdCompanies
        .Where(c => c.Mcs == mcs && c.Id == id)
        .Include(c => c.User)
        .Include(c => c.Preview)
        .Include(c => c.Images)
        .Select(c => new CrowdfundCompanyItemDto
        {
          Id = c.Id,
          User = new UserInfoDto
          {
            Id = c.User.Id,
            UserName = c.User.UserName ?? "",
            MinecraftUuid = c.User.MinecraftUUID
          },
          Title = c.Title,
          ShortDescription = c.ShortDescription,
          Preview = c.Preview == null ? null : c.Preview.FullPath,
          Images = c.Images.Select(i => i.FullPath).ToArray(),
          Content = c.Content,
          CurrentAmount = c.CurrentAmount,
          Goal = c.Goal,
          IsOver = c.IsOver,
          EndDate = c.EndDate,
          CommentCount = c.Comments.Count,
          UpdatedAt = c.UpdatedAt,
        })
        .FirstOrDefaultAsync();

      if (company == null)
      {
        return new ErrorResult(ResponseError.CrowdCompanyNotFound, HttpStatusCode.NotFound);
      }
      else
      {
        return Results.Ok(company);
      }
    }

    [Authorize(Roles = AuthRoles.User)]
    [ProducesResponseType(typeof(CrowdfundCompanyItemDto), (int)HttpStatusCode.OK)]
    public async Task<IResult> CreateCompany(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, CreateCrowdfundCompanyDto dto, MCServer mcs)
    {
      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      var forbidden = await App.GetModule<BaseController>("base").CheckForbiddenAction(db, user, ForbiddenUserAction.CREATE_CROWDFUNDINGS, mcs);
      if (forbidden != null)
      {
        return forbidden;
      }

      var paymentModule = App.GetModule<PaymentController>("payment");
      var fileModule = App.GetModule<FileController>("file");

      var files = new List<FileMeta>();
      var previewFile = dto.Preview != null ? (await fileModule.GetFileMetaById(db, (Guid)dto.Preview)) : null;

      foreach (var img in dto.Images)
      {
        files.Add(await fileModule.GetFileMetaById(db, img));
      }

      if (dto.Goal < 8 || dto.EndDate <= DateTime.UtcNow || dto.EndDate > DateTime.UtcNow.AddMonths(2))
      {
        if (previewFile != null)
        {
          await fileModule.RemoveFile(db, user, previewFile.Id);
        }

        await fileModule.RemoveFiles(db, user, dto.Images);
        return new ErrorResult(ResponseError.ValidationFailed, HttpStatusCode.BadRequest);
      }

      var trans = new AccountTransaction
      {
        Initiator = user,
        Amount = CREATE_COMPANY_PRICE,
        Destination = "SPApp/Starter",
      };

      var pRes = await paymentModule.CreateInternalTransaction(db, user, trans, mcs);

      if (!trans.IsSuccess)
      {
        if (previewFile != null)
        {
          await fileModule.RemoveFile(db, user, previewFile.Id);
        }

        await fileModule.RemoveFiles(db, user, dto.Images);
        return pRes;
      }

      var crowdCompany = new CrowdfundCompany
      {
        Title = dto.Title.Length > 48 ? dto.Title[..48] : dto.Title,
        ShortDescription = dto.ShortDescription.Length > 512 ? dto.ShortDescription[..512] : dto.ShortDescription,
        Goal = dto.Goal,
        EndDate = dto.EndDate,
        Images = files,
        Preview = previewFile,
        Content = dto.Content.Length > 16321 ? dto.Content[..16321] : dto.Content,
        User = user,
        Mcs = mcs
      };

      db.Add(crowdCompany);
      await db.SaveChangesAsync();

      return Results.Ok(new CrowdfundCompanyItemDto
      {
        Id = crowdCompany.Id,
        ShortDescription = crowdCompany.ShortDescription,
        Goal = crowdCompany.Goal,
        EndDate = crowdCompany.EndDate,
        CommentCount = 0,
        Content = crowdCompany.Content,
        CurrentAmount = 0,
        Images = crowdCompany.Images.Select(i => i.FullPath).ToArray(),
        IsOver = crowdCompany.IsOver,
        Preview = crowdCompany.Preview?.FullPath,
        Title = crowdCompany.Title,
        UpdatedAt = crowdCompany.UpdatedAt,
        User = new UserInfoDto
        {
          Id = user.Id,
          UserName = user.UserName ?? "",
          MinecraftUuid = user.MinecraftUUID
        }
      });
    }

    [ProducesResponseType(typeof(CommentDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> CreateComment(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, CreateCommentDto dto, uint id, MCServer mcs)
    {
      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      var forbidden = await App.GetModule<BaseController>("base").CheckForbiddenAction(db, user, ForbiddenUserAction.CREATE_COMMENTS, mcs);
      if (forbidden != null)
      {
        return forbidden;
      }

      var company = await db.CrowdCompanies.Where(c => c.Id == id).FirstOrDefaultAsync();

      if (company == null)
      {
        return new ErrorResult(ResponseError.CrowdCompanyNotFound, HttpStatusCode.NotFound);
      }

      if (await db.CrowdComments
        .Include(c => c.User)
        .Where(c => c.CreatedAt >= DateTime.UtcNow.AddMinutes(-20) && c.User.Id == user.Id)
        .Take(MAX_COMMENTS_PER_20_MINUTES + 1).CountAsync() > MAX_COMMENTS_PER_20_MINUTES)
      {
        return new ErrorResult(ResponseError.TooManyComments, HttpStatusCode.TooManyRequests);
      }

      if (dto.Text.Length == 0)
      {
        return new ErrorResult(ResponseError.ValidationFailed, HttpStatusCode.BadRequest);
      }

      var text = dto.Text.Length > 512 ? dto.Text[..512] : dto.Text;

      var comment = new CrowdfundComment { Comment = text, CrowdCompany = company, User = user };
      db.Add(comment);
      await db.SaveChangesAsync();
      return Results.Ok(new CommentDto
      {
        Id = comment.Id,
        Text = comment.Comment,
        User = new UserInfoDto
        {
          Id = comment.User.Id,
          UserName = comment.User.UserName ?? "",
          MinecraftUuid = comment.User.MinecraftUUID
        },
        CreatedAt = comment.CreatedAt,
      });
    }

    [ProducesResponseType(typeof(ItemList<CommentDto>), (int)HttpStatusCode.OK)]
    public async Task<IResult> GetComments(SQLiteDbContext db, int from, int count, string sort, uint id, MCServer mcs)
    {
      if (count <= 0 || count > 50)
      {
        return new ErrorResult(ResponseError.Unknown, HttpStatusCode.BadRequest);
      }

      IQueryable<CrowdfundComment> q = db.CrowdComments
        .Include(c => c.CrowdCompany)
        .Include(c => c.User)
        .Where(c => c.CrowdCompany.Id == id);

      var itemCount = await q.CountAsync();


      q = sort switch
      {
        "dateAsc" => q.OrderBy(c => c.CreatedAt),
        "dateDesc" => q.OrderByDescending(c => c.CreatedAt),
        _ => q,
      };

      var items = await q
        .Skip(from)
        .Take(count)
        .Select(c => new CommentDto
        {
          Id = c.Id,
          Text = c.Comment,
          User = new UserInfoDto
          {
            Id = c.User.Id,
            UserName = c.User.UserName ?? "",
            MinecraftUuid = c.User.MinecraftUUID
          },
          CreatedAt = c.CreatedAt,
        })
        .ToListAsync();

      return Results.Ok(new ItemList<CommentDto> { Items = items, Count = itemCount });
    }

    [ProducesResponseType(typeof(CommentDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> SupportCompany(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, SupportCrowdfundCompanyDto dto, uint id, MCServer mcs)
    {
      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      var forbidden = await App.GetModule<BaseController>("base").CheckForbiddenAction(db, user, ForbiddenUserAction.CREATE_TRANSACTIONS, mcs);
      if (forbidden != null)
      {
        return forbidden;
      }

      var company = await db.CrowdCompanies.Include(c => c.User).Where(c => c.Id == id && c.Mcs == mcs).FirstOrDefaultAsync();

      if (company == null)
      {
        return new ErrorResult(ResponseError.CrowdCompanyNotFound, HttpStatusCode.NotFound);
      }

      if (company.User.Id == user.Id)
      {
        return new ErrorResult(ResponseError.CantSupportYourOwnCompany, HttpStatusCode.BadRequest);
      }

      if (dto.Amount <= 0)
      {
        return new ErrorResult(ResponseError.ValidationFailed, HttpStatusCode.BadRequest);
      }

      var paymentModule = App.GetModule<PaymentController>("payment");
      var trans = new AccountTransaction
      {
        Initiator = user,
        Amount = dto.Amount,
        Destination = $"SPApp/Starter/c/{id}",
      };

      var pRes = await paymentModule.CreateInternalTransaction(db, user, trans, mcs);

      if (!trans.IsSuccess)
      {
        return pRes;
      }

      company.Transactions.Add(trans);
      company.CurrentAmount += trans.Amount;
      db.Update(company);
      await db.SaveChangesAsync();

      return Results.Ok();
    }

    [ProducesResponseType(typeof(CommentDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> CancelCompany(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, uint id, MCServer mcs)
    {
      var user = await userMgr.GetUserAsync(claims);
      if (user ==  null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }
      var isAdmin = await userMgr.IsInRoleAsync(user, AuthRoles.Admin);
      var company = await db.CrowdCompanies.Where(c => c.Id == id && c.Mcs == mcs).FirstOrDefaultAsync();

      if (company == null)
      {
        return new ErrorResult(ResponseError.CrowdCompanyNotFound, HttpStatusCode.NotFound);
      }

      if (!isAdmin && company.User.Id != user.Id)
      {
        return new ErrorResult(ResponseError.Forbidden, HttpStatusCode.Forbidden);
      }

      await CloseCompany(db, id, false, mcs);

      return Results.Ok();
    }

    [ProducesResponseType(typeof(CommentDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> AcceptCompany(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, uint id, MCServer mcs)
    {
      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }
      var isAdmin = await userMgr.IsInRoleAsync(user, AuthRoles.Admin);
      var company = await db.CrowdCompanies.Where(c => c.Id == id && c.Mcs == mcs).FirstOrDefaultAsync();

      if (company == null)
      {
        return new ErrorResult(ResponseError.CrowdCompanyNotFound, HttpStatusCode.NotFound);
      }

      if (!isAdmin && company.User.Id != user.Id)
      {
        return new ErrorResult(ResponseError.Forbidden, HttpStatusCode.Forbidden);
      }

      if (!isAdmin && company.CurrentAmount < company.Goal)
      {
        return new ErrorResult(ResponseError.Forbidden, HttpStatusCode.Forbidden);
      }

      await CloseCompany(db, id, true, mcs);

      return Results.Ok();
    }

    public async Task<ErrorResult?> CloseCompany(SQLiteDbContext db, uint id, bool success, MCServer mcs)
    {
      var company = await db.CrowdCompanies
        .Where(c => c.Id == id && c.Mcs == mcs)
        .Include(c => c.Transactions)
        .Include(c => c.User)
        .FirstOrDefaultAsync();

      if (company == null)
      {
        return new ErrorResult(ResponseError.CrowdCompanyNotFound, HttpStatusCode.NotFound);
      }

      var transactions = company.Transactions.Where(t => t.IsSuccess);
      var userAccount = await db.Accounts.Where(a => a.Mcs == mcs && a.User.Id == company.User.Id).FirstOrDefaultAsync();
      
      if (userAccount == null)
      {
        return new ErrorResult(ResponseError.AccountNotFound, HttpStatusCode.NotFound);
      }

      foreach (var t in transactions)
      {
        if (success)
        {
          t.Destination = company.User.Id.ToString();
          userAccount.Balance += t.Amount;
          db.Update(t);
        }
        else
        {
          t.Destination = t.Initiator.Id.ToString();
          var supporterAcc = await db.Accounts
            .Include(a => a.User)
            .Where(a => a.Mcs == mcs && a.User.Id == t.Initiator.Id)
            .FirstOrDefaultAsync();

          if (supporterAcc != null) {
            supporterAcc.Balance += t.Amount;
            t.Destination = t.Initiator.Id.ToString();
            db.Update(supporterAcc);
          }
        }
      }

      company.IsOver = true;
      db.Update(company);
      db.Update(userAccount);
      await db.SaveChangesAsync();
      return null;
    }

    [Authorize(Roles = AuthRoles.User)]
    [ProducesResponseType(typeof(CrowdfundCompanyItemDto), (int)HttpStatusCode.OK)]
    public async Task<IResult> EditCompany(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, PreviewGen pGen, uint id, EditCrowdfundCompanyDto dto, MCServer mcs)
    {
      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      var forbidden = await App.GetModule<BaseController>("base").CheckForbiddenAction(db, user, ForbiddenUserAction.CREATE_CROWDFUNDINGS, mcs);
      if (forbidden != null)
      {
        return forbidden;
      }

      var company = await db.CrowdCompanies
        .Include(c => c.User)
        .Include(c => c.Images)
        .Include(c => c.Preview)
        .Where(c => c.Id == id && c.User.Id == user.Id && c.Mcs == mcs && !c.IsOver)
        .FirstOrDefaultAsync();

      if (company == null)
      {
        return new ErrorResult(ResponseError.CrowdCompanyNotFound, HttpStatusCode.NotFound);
      }

      var fileModule = App.GetModule<FileController>("file");

      var files = new List<FileMeta>();
      var filesForRemove = new List<FileMeta>();

      // Проверка обновления превью
      var previewFile = dto.Preview != null ? (await fileModule.GetFileMetaById(db, (Guid)dto.Preview)) : null;
      if (previewFile != null && company.Preview != null && previewFile.Id != company.Preview.Id) {
        filesForRemove.Add(company.Preview);
      }
      else if (dto.Preview == null && company.Preview != null)
      {
        filesForRemove.Add(company.Preview);
        previewFile = null;
      }

      company.Preview = previewFile;

      // Обновление картинок
      if (dto.Images != null)
      {
        foreach (var img in dto.Images)
        {
          if (files.Where(f => f.Id == img).IsNullOrEmpty())
          {
            files.Add(await fileModule.GetFileMetaById(db, img));
          }
        }

        foreach (var existingImg in company.Images)
        {
          if (files.Where(f => f.Id == existingImg.Id).IsNullOrEmpty())
          {
            filesForRemove.Add(existingImg);
          }
        }
      }

      company.Images = files;

      company.Title = dto.Title ?? company.Title;
      company.ShortDescription = dto.ShortDescription ?? company.ShortDescription;
      company.Content = dto.Content ?? company.Content;

      if (await userMgr.IsInRoleAsync(user, AuthRoles.Admin))
      {
        company.Goal = dto.Goal ?? company.Goal;
        company.EndDate = dto.EndDate ?? company.EndDate;
      }

      db.Update(company);
      await db.SaveChangesAsync();

      foreach (var file in filesForRemove)
      {
        await fileModule.RemoveFile(db, user, file.Id);
      }
      await db.SaveChangesAsync();

      _ = Task.Run(() => pGen.MakePreview("crowd", id, mcs));
      return Results.Ok();
    }

    void IWebController.Inject(WebApplication server)
    {
      server.MapGet("/crowd/companies/all", GetCompanies);
      server.MapGet("/crowd/companies/{id}", GetCompany);
      server.MapPut("/crowd/companies/{id}", EditCompany);
      server.MapPost("/crowd/companies/create", CreateCompany);
      server.MapGet("/crowd/companies/{id}/comments", GetComments);
      server.MapPost("/crowd/companies/{id}/comments/create", CreateComment);
      server.MapPost("/crowd/companies/{id}/support", SupportCompany);
      server.MapPost("/crowd/companies/{id}/cancel", CancelCompany);
      server.MapPost("/crowd/companies/{id}/accept", AcceptCompany);
    }

    async Task IWebController.RunTimingTask(SQLiteDbContext db)
    {
      var companies = await db.CrowdCompanies.Where(c => c.IsOver == false && c.EndDate <= DateTime.UtcNow).ToListAsync();

      foreach(var company in companies)
      {
        await CloseCompany(db, company.Id, company.CurrentAmount >= company.Goal, company.Mcs);
      }
    }
  }
}
