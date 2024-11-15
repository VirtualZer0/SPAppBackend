using Discord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spapp_backend.Core;
using spapp_backend.Core.Dtos;
using spapp_backend.Core.Enums;
using spapp_backend.Core.Models;
using spapp_backend.Db;
using spapp_backend.Modules.Admin.Dtos;
using spapp_backend.Modules.Crowdfunding.Dtos;
using spapp_backend.Modules.Crowdfunding.Models;
using spapp_backend.Utils;
using System.Net;
using System.Security.Claims;

namespace spapp_backend.Modules.Admin.Controllers
{
  public class MainAdminController : IWebController
  {
    [ProducesResponseType(typeof(ItemList<UserProfileDto>), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IResult> GetUsers(SQLiteDbContext db, UserManager<User> userMgr, int from, int count, string sort, string? search, MCServer mcs)
    {
      IQueryable<User> q = sort switch
      {
        "abcAsc" => db.Users.OrderBy(c => c.UserName),
        "abcDesc" => db.Users.OrderByDescending(c => c.UserName),
        _ => db.Users,
      };

      if (search != null && search != "")
      {
        q = q
          .Where(
            c => c.NormalizedUserName != null &&
            (c.NormalizedUserName.Contains(search.ToUpper()) || c.DiscordName.ToUpper().Contains(search.ToUpper()) || c.Id.ToString() == search)
          );
      }

      var itemCount = await q.CountAsync();

      var items = await q
        .Skip(from)
        .Take(count)
        .Select(u => new UserProfileDto
        {
          Id = u.Id,
          UserName = u.UserName ?? "",
          DiscordName = u.DiscordName,
          DiscordId = u.DiscordId,
          MinecraftUuid = u.MinecraftUUID,
          LatestActivity = u.LatestActivity,
          CreatedAt = u.CreatedAt,
          Accounts = null!
        })
        .ToListAsync();

      foreach (var item in items)
      {
        var user = await userMgr.FindByIdAsync(item.Id.ToString());

        if (user != null)
        {
          item.Roles = (await userMgr.GetRolesAsync(user)).ToArray();
        }

        item.Accounts = db.Accounts
          .Include(acc => acc.User)
          .Where(acc => acc.User.Id == item.Id && acc.IsDefault)
          .ToDictionary(acc => (uint)acc.Mcs, acc => new UserAccountDto { Id = acc.Id, Balance = acc.Balance, IsDefault = acc.IsDefault, Mcs = acc.Mcs });
      }

      return Results.Ok(new ItemList<UserProfileDto> { Items = items, Count = itemCount });
    }

    [ProducesResponseType(typeof(ItemList<PaymentDto>), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IResult> GetPayments(SQLiteDbContext db, int from, int count, string sort, uint? userId, PaymentStatus? filter, MCServer mcs)
    {
      IQueryable<PaymentRequest> q = sort switch
      {
        "dateAsc" => db.Payments.OrderBy(p => p.CreatedAt),
        "dateDesc" => db.Payments.OrderByDescending(p => p.CreatedAt),
        "amountAsc" => db.Payments.OrderBy(p => p.Amount),
        "amountDesc" => db.Payments.OrderByDescending(p => p.Amount),
        "statusAsc" => db.Payments.OrderBy(p => p.Status),
        "statusDesc" => db.Payments.OrderByDescending(p => p.Status),
        _ => db.Payments,
      };

      var itemCount = filter switch
      {
        null => await db.Payments
          .Include(p => p.Initiator)
          .Where(p => p.Mcs == mcs && (userId == null || p.Initiator.Id == userId))
          .CountAsync(),

        _ => await db.Payments
          .Include(p => p.Initiator)
          .Where(p => p.Status == filter && p.Mcs == mcs && (userId == null || p.Initiator.Id == userId))
          .CountAsync(),
      };

      var items = await q
        .Skip(from)
        .Take(count)
        .Where(p => p.Mcs == mcs && (userId == null || p.Initiator.Id == userId) && (filter == null || p.Status == filter))
        .Include(p => p.Initiator)
        .Include(p => p.Destination)
        .Select(p => new PaymentDto
        {
          Id = p.Id,
          User = new UserInfoDto { Id = p.Initiator.Id, MinecraftUuid = p.Initiator.MinecraftUUID, UserName = p.Initiator.UserName ?? "" },
          Payer = p.Payer,
          DestinationId = p.Destination.Id,
          Amount = p.Amount,
          ExpirationDate = p.ExpirationDate,
          CreatedAt = p.CreatedAt,
          PayUrl = p.PayUrl,
          Status = p.Status
        })
        .ToListAsync();

      return Results.Ok(new ItemList<PaymentDto> { Items = items, Count = itemCount });
    }

    [ProducesResponseType(typeof(PaymentDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IResult> GetPayment(SQLiteDbContext db, Guid paymentId)
    {
      var pEntity = await db.Payments
        .Where(p => p.Id == paymentId)
        .Include(p => p.Initiator)
        .Include(p => p.Destination)
        .Select(p => new PaymentDto
        {
          Id = p.Id,
          User = new UserInfoDto { Id = p.Initiator.Id, MinecraftUuid = p.Initiator.MinecraftUUID, UserName = p.Initiator.UserName ?? "" },
          Payer = p.Payer,
          DestinationId = p.Destination.Id,
          Amount = p.Amount,
          ExpirationDate = p.ExpirationDate,
          CreatedAt = p.CreatedAt,
          PayUrl = p.PayUrl,
          Status = p.Status
        })
        .FirstOrDefaultAsync();

      if (pEntity == null)
      {
        return new ErrorResult(ResponseError.PaymentNotFound, HttpStatusCode.NotFound);
      }

      return Results.Ok(pEntity);
    }

    [ProducesResponseType((int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IResult> EditPayment(SQLiteDbContext db, UserManager<User> userMgr, ClaimsPrincipal claims, Guid paymentId, EditPaymentDto payment)
    {
      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      var pEntity = await db.Payments.Include(p => p.Destination).Where(p => p.Id == paymentId).FirstOrDefaultAsync();
      if (pEntity == null)
      {
        return new ErrorResult(ResponseError.PaymentNotFound, HttpStatusCode.NotFound);
      }

      if (pEntity.Status == PaymentStatus.Awaiting && payment.Status == PaymentStatus.Paid)
      {
        pEntity.Destination.Balance += pEntity.Amount;
        pEntity.Payer = $"SPApp/Admin/{user.Id}";
      }

      pEntity.Status = payment.Status ?? pEntity.Status;
      pEntity.ExpirationDate = payment.ExpirationDate ?? pEntity.ExpirationDate;

      db.Update(pEntity);
      await db.SaveChangesAsync();

      return Results.Ok();
    }

    [ProducesResponseType(typeof(MainStatDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IResult> GetMainStat(SQLiteDbContext db, SPApi sp, MCServer mcs)
    {
      int? SPBalance = null;
      int? SPMBalance = null;

      try
      {
        SPBalance = (int)(await sp.GetBalance(MCServer.SP)).Balance;
      }
      catch { }

      try
      {
        SPMBalance = (int)(await sp.GetBalance(MCServer.SPM)).Balance;
      }
      catch { }

      return Results.Ok(new MainStatDto
      {
        AllUserCount = await db.Users.CountAsync(),
        NewUserTodayCount = await db.Users.Where(u => u.CreatedAt > DateTime.UtcNow.AddDays(-1)).CountAsync(),
        NewUserLastWeekCount = await db.Users.Where(u => u.CreatedAt > DateTime.UtcNow.AddDays(-7)).CountAsync(),

        DayActiveUserCount = await db.Users.Where(u => u.LatestActivity > DateTime.UtcNow.AddDays(-1)).CountAsync(),
        WeekActiveUserCount = await db.Users.Where(u => u.LatestActivity > DateTime.UtcNow.AddDays(-7)).CountAsync(),
        MonthActiveUserCount = await db.Users.Where(u => u.LatestActivity > DateTime.UtcNow.AddMonths(-1)).CountAsync(),

        PaymentCount = await db.Payments.Where(p => p.Mcs == mcs).CountAsync(),
        PaymentTodayCount = await db.Payments.Where(p => p.CreatedAt > DateTime.UtcNow.AddDays(-1) && p.Mcs == mcs).CountAsync(),
        PaymentLastWeekCount = await db.Payments.Where(p => p.CreatedAt > DateTime.UtcNow.AddDays(-7) && p.Mcs == mcs).CountAsync(),
        PaymentTodayFailedCount = await db.Payments.Where(p => p.CreatedAt > DateTime.UtcNow.AddDays(-1) && p.Status == PaymentStatus.Canceled && p.Mcs == mcs).CountAsync(),

        SPCardBalance = SPBalance,
        SPMCardBalance = SPMBalance
      });
    }

    void IWebController.Inject(WebApplication server)
    {
      server.MapGet("/admin/users", GetUsers);
      server.MapGet("/admin/payments", GetPayments);
      server.MapGet("/admin/payments/{paymentId}", GetPayment);
      server.MapPut("/admin/payments/{paymentId}", EditPayment);
      server.MapGet("/admin/statistic", GetMainStat);
    }

    Task IWebController.RunTimingTask(SQLiteDbContext db, PreviewGen pGen)
    {
      return Task.CompletedTask;
    }
  }
}
