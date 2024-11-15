using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using spapp_backend.Core.Dtos;
using spapp_backend.Core.Dtos.SP;
using spapp_backend.Core.Enums;
using spapp_backend.Core.Models;
using spapp_backend.Db;
using spapp_backend.Utils;
using System.Net;
using System.Security.Claims;

namespace spapp_backend.Core.Controllers
{
  public class PaymentController : IWebController
  {
    public static readonly int MAX_PAYMENTS_PER_20_MINUTES = 5;

    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IResult> AcceptPayment(SQLiteDbContext db, SPPaymentResponse payment, string transCode, [FromHeader(Name = "X-Body-Hash")] string hash, MCServer mcs)
    {
      var paymentReq = await db.Payments
        .Include(p => p.Destination)
        .Where(p => p.TransCode.ToString() == transCode.ToUpper() && p.Mcs == mcs)
        .FirstOrDefaultAsync();

      if (paymentReq == null)
      {
        return new ErrorResult(ResponseError.PaymentNotFound, HttpStatusCode.NotFound);
      }

      if (paymentReq.Hash != hash)
      {
        return new ErrorResult(ResponseError.WrongPaymentHash, HttpStatusCode.Forbidden);
      }

      paymentReq.Destination.Balance += payment.Amount;
      paymentReq.Status = PaymentStatus.Paid;

      return Results.Ok();
    }

    [ProducesResponseType((int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> WithdrawMoney(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, SPApi spApi, WithdrawMoneyDto dto, MCServer mcs)
    {
      if (dto.Card == "")
      {
        return new ErrorResult(ResponseError.ValidationFailed, HttpStatusCode.BadRequest);
      }

      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.Forbidden);
      }

      var account = await db.Accounts
        .Include(a => a.User)
        .Where(a => a.Id == dto.AccountId && a.Mcs == mcs && a.User.Id == user.Id)
        .FirstOrDefaultAsync();

      if (account == null)
      {
        return new ErrorResult(ResponseError.AccountNotFound, HttpStatusCode.NotFound);
      }

      var transaction = new AccountTransaction
      {
        Amount = dto.Amount,
        Destination = dto.Card,
        From = account.Id.ToString(),
        Initiator = user,
        Mcs = mcs,
        Type = TransactionType.Output,
        IsSuccess = false,
        Reason = TransactionFailReason.Unknown
      };

      await db.AddAsync(transaction);
      await db.SaveChangesAsync();

      if (account.Balance < dto.Amount)
      {
        transaction.Reason = TransactionFailReason.InsufficientFunds;
        db.Update(transaction);
        await db.SaveChangesAsync();
        return new ErrorResult(ResponseError.InsufficientFunds, HttpStatusCode.BadRequest);
      }

      try
      {
        await spApi.SendTransaction(new SPCreateTransactionDto
        {
          Amount = dto.Amount,
          Receiver = dto.Card,
          Comment = $"AID: {account.Id} | TID: {transaction.Id}"
        }, mcs);
      }
      catch (Exception ex)
      {
        transaction.Reason = TransactionFailReason.SPApiError;
        db.Update(transaction);
        await db.SaveChangesAsync();
        return new ErrorResult(ResponseError.SPApiError, HttpStatusCode.InternalServerError, ex);
      }

      account.Balance -= dto.Amount;
      transaction.IsSuccess = true;
      transaction.Reason = null;

      db.Update(account);
      db.Update(transaction);
      await db.SaveChangesAsync();
      return Results.Ok();
    }

    [ProducesResponseType(typeof(PaymentDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> GetPayment(SQLiteDbContext db, string uuid, MCServer mcs)
    {
      var res = await db.Payments
        .Where(p => p.Id.ToString() == uuid.ToUpper() && p.Mcs == mcs)
        .Include(p => p.Destination.User)
        .Select(p => new PaymentDto
        {
          Id = p.Id,
          Amount = p.Amount,
          User = new UserInfoDto
          {
            Id = p.Destination.User.Id,
            MinecraftUuid = p.Destination.User.MinecraftUUID,
            UserName = p.Destination.User.UserName ?? ""
          },
          DestinationId = p.Destination.Id,
          ExpirationDate = p.ExpirationDate,
          CreatedAt = p.CreatedAt,
          Payer = p.Payer,
          PayUrl = p.PayUrl,
          Status = p.Status
        }).FirstOrDefaultAsync();

      if (res == null)
      {
        return new ErrorResult(ResponseError.PaymentNotFound, HttpStatusCode.NotFound);
      }
      else
      {
        return Results.Ok(res);
      }
    }

    [ProducesResponseType(typeof(PaymentDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> CreatePayment(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, SPApi sp, CreatePaymentDto dto, MCServer mcs)
    {
      if (dto.Amount < 1)
      {
        return new ErrorResult(ResponseError.ValidationFailed, HttpStatusCode.BadRequest);
      }

      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      var forbidden = await App.GetModule<BaseController>("base").CheckForbiddenAction(db, user, ForbiddenUserAction.CREATE_PAYMENTS, mcs);
      if (forbidden != null)
      {
        return forbidden;
      }

      var checkTime = DateTime.UtcNow.AddMinutes(-20);
      var unprocessedPayments = db.Payments
        .Include(p => p.Initiator)
        .Where(p => p.CreatedAt >= checkTime && p.Initiator.Id == user.Id && p.Status != PaymentStatus.Paid);


      if (await unprocessedPayments.CountAsync() > MAX_PAYMENTS_PER_20_MINUTES)
      {
        return new ErrorResult(ResponseError.TooManyUnprocessedPayments, HttpStatusCode.TooManyRequests);
      }

      var account = await db.Accounts.Where(a => a.Mcs == mcs && a.Id == dto.AccountId).FirstOrDefaultAsync();

      if (account == null)
      {
        return new ErrorResult(ResponseError.AccountNotFound, HttpStatusCode.NotFound);
      }

      var payment = new PaymentRequest()
      {
        Initiator = user,
        Destination = account,
        Mcs = mcs,
        Status = PaymentStatus.Awaiting,
        Amount = dto.Amount,
        ExpirationDate = DateTime.UtcNow.AddMinutes(10),
        TransCode = Guid.NewGuid(),
      };

      db.Payments.Add(payment);
      await db.SaveChangesAsync();

      try
      {
        var spRes = await sp.RequestPayment(new SPPaymentDataDto
        {
          Amount = payment.Amount,
          Data = payment.Data,
          RedirectUrl = dto.RedirectTo.Replace("[ID]", payment.Id.ToString()),
          WebhookUrl = new Uri(new Uri(App.GetConfig("Settings")["AppUrl"] ?? ""), $"/payments/accept/{payment.TransCode}").ToString(),
        }, mcs);

        if (spRes != null)
        {
          payment.PayUrl = spRes.Url;
          payment.Hash = spRes.Hash;
          db.Update(payment);
          await db.SaveChangesAsync();
          return Results.Ok(new PaymentDto
          {
            Id = payment.Id,
            Amount = payment.Amount,
            DestinationId = payment.Destination.Id,
            ExpirationDate = payment.ExpirationDate,
            CreatedAt = payment.CreatedAt,
            PayUrl = payment.PayUrl,
            Status = payment.Status,
            User = new UserInfoDto
            {
              Id = payment.Initiator.Id,
              MinecraftUuid = payment.Initiator.MinecraftUUID,
              UserName = payment.Initiator.UserName ?? ""
            }
          });
        }
        else
        {
          payment.Status = PaymentStatus.Canceled;
          db.Update(payment);
          await db.SaveChangesAsync();
          return new ErrorResult(ResponseError.SPApiError, HttpStatusCode.ServiceUnavailable);
        }
      }
      catch (Exception ex)
      {
        payment.Status = PaymentStatus.Canceled;
        db.Update(payment);
        await db.SaveChangesAsync();
        App.Logger.WriteExceptionLog(ex, "payments.txt");
        return new ErrorResult(ResponseError.SPApiError, HttpStatusCode.ServiceUnavailable);
      }
    }

    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> CreateInternalTransaction(SQLiteDbContext db, User user, AccountTransaction trans, MCServer mcs)
    {
      var forbidden = await App.GetModule<BaseController>("base").CheckForbiddenAction(db, user, ForbiddenUserAction.CREATE_TRANSACTIONS, mcs);
      if (forbidden != null)
      {
        return forbidden;
      }

      var account = await db.Accounts.Where(a => a.Id == user.Id && a.Mcs == mcs && a.IsDefault).FirstOrDefaultAsync();

      trans.Type = TransactionType.Internal;
      trans.Mcs = mcs;

      if (account == null)
      {
        return new ErrorResult(ResponseError.AccountNotFound, HttpStatusCode.NotFound);
      }

      trans.From = account.Id.ToString();
      Account? destAcc = null;

      if (!trans.Destination.Contains("SPApp"))
      {
        var destAccId = int.Parse(trans.Destination);
        destAcc = await db.Accounts.Where(acc => acc.Id == destAccId && acc.Mcs == mcs).FirstOrDefaultAsync();
      }

      if (account.Balance < trans.Amount)
      {
        trans.IsSuccess = false;
        trans.Reason = TransactionFailReason.InsufficientFunds;
        db.Add(trans);
        await db.SaveChangesAsync();
        return new ErrorResult(ResponseError.InsufficientFunds, HttpStatusCode.BadRequest);
      }

      account.Balance -= trans.Amount;

      if (destAcc != null)
      {
        destAcc.Balance += trans.Amount;
        db.Update(destAcc);
      }

      trans.IsSuccess = true;
      db.Add(trans);
      db.Update(account);
      await db.SaveChangesAsync();
      return Results.Ok();
    }

    void IWebController.Inject(WebApplication server)
    {
      server.MapPost("payments/create", CreatePayment);
      server.MapPost("payments/accept/{transCode}", AcceptPayment);
      server.MapPost("payments/withdraw", WithdrawMoney);
      server.MapGet("payments/topup/{uuid}", GetPayment);
    }

    Task IWebController.RunTimingTask(SQLiteDbContext db, PreviewGen pGen)
    {
      return Task.CompletedTask;
    }
  }
}
