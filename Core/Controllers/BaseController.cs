using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spapp_backend.Core.Dtos;
using spapp_backend.Core.Enums;
using spapp_backend.Core.Models;
using spapp_backend.Db;
using spapp_backend.Utils;
using System.Net;
using System.Security.Claims;

namespace spapp_backend.Core.Controllers
{
  public class BaseController : IWebController
  {
    /// <summary>
    /// Get initial data for frontend app
    /// </summary>
    [ProducesResponseType(typeof(InitialDto), (int)HttpStatusCode.OK)]
    public IResult GetInitialData(HttpContext ctx, SQLiteDbContext db)
    {
      var thirdParty = App.GetConfig().GetSection("ThirdParty");
      return Results.Ok(new InitialDto
      {
        DiscordClientId = thirdParty["DiscordClientId"] ?? "",
        DiscordRedirectUri = thirdParty["DiscordRedirectUri"] ?? "",
      });
    }

    public async Task<ErrorResult?> CheckForbiddenAction (SQLiteDbContext db, User? user, ForbiddenUserAction action, MCServer mcs) {
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      var forbidden = await db.Forbiddens
        .Include(f => f.User)
        .Where(f => f.User.Id == user.Id && DateTime.UtcNow < f.ForbiddenUntil && f.Mcs == mcs && (f.Action == action || f.Action == ForbiddenUserAction.ALL))
        .Select(f => new ForbiddenActionDto
        {
          Action = f.Action,
          ForbiddenUntil = f.ForbiddenUntil,
          Reason = f.Reason,
        })
        .FirstOrDefaultAsync();

      if (forbidden == null)
      {
        return null;
      }
      else
      {
        return new ErrorResult(ResponseError.ForbiddenAction, HttpStatusCode.Forbidden, forbidden);
      }
    }

    void IWebController.Inject(WebApplication server)
    {
      server.MapGet("/app/init", this.GetInitialData);
    }

    Task IWebController.RunTimingTask(SQLiteDbContext db, PreviewGen pGen)
    {
      return Task.CompletedTask;
    }
  }
}
