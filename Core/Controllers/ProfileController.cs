using Discord.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using spapp_backend.Core.Dtos;
using spapp_backend.Core.Dtos.SP;
using spapp_backend.Core.Enums;
using spapp_backend.Core.Models;
using spapp_backend.Db;
using spapp_backend.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace spapp_backend.Core.Controllers
{
  public class ProfileController : IWebController, IDisposable
  {
    public string JwtSecret { get; private set; } = string.Empty;
    readonly HttpClient httpClient = new();
    public ProfileController()
    {
      if (!File.Exists("Data/JWTSecret"))
      {
        this.ResetJWTSecret();
      }
      else
      {
        this.JwtSecret = File.ReadAllText("Data/JWTSecret");
      }
    }

    public void ResetJWTSecret()
    {
      Random random = new();
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
      var key = new string(Enumerable.Repeat(chars, 32)
          .Select(s => s[random.Next(s.Length)]).ToArray());

      File.WriteAllText("Data/JWTSecret", key);
      this.JwtSecret = key;
    }

    /// <summary>
    /// Get bearer token by discord auth code
    /// </summary>
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(TokenDto), (int)HttpStatusCode.OK)]
    [AllowAnonymous]
    public async Task<IResult> AuthByDiscord(UserManager<User> userMgr, SQLiteDbContext db, SPApi spApi, MojangApi mjApi, AuthByDiscordDto dto, MCServer mcs)
    {
      var conf = App.GetConfig();

      OAuthTokenDto? authData;
      try
      {
        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", conf["ThirdParty:DiscordClientId"] ?? ""),
            new KeyValuePair<string, string>("client_secret", conf["ThirdParty:DiscordClientSecret"] ?? ""),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", dto.Code),
            new KeyValuePair<string, string>("redirect_uri", conf["ThirdParty:DiscordRedirectUri"] ?? ""),
        });

        var res = await httpClient.PostAsync("https://discord.com/api/oauth2/token", body);
        if ((int)res.StatusCode >= 400)
        {
          return new ErrorResult(ResponseError.AuthDiscord);
        }

        authData = await res.Content.ReadFromJsonAsync<OAuthTokenDto>();
      }
      catch (Exception ex)
      {
        return new ErrorResult(ResponseError.AuthDiscord, ex);
      }

      // Third-party checks
      using var discordClient = new DiscordRestClient();
      try
      {
        await discordClient.LoginAsync(Discord.TokenType.Bearer, authData?.access_token ?? "");
      }
      catch (Exception ex)
      {
        return new ErrorResult(ResponseError.AuthDiscord, ex);
      }

      var dsUser = await discordClient.GetCurrentUserAsync();
      if (dsUser == null)
      {
        return new ErrorResult(ResponseError.AuthDiscord);
      }

      var user = await db.Users.Where(u => u.DiscordId == dsUser.Id.ToString()).FirstOrDefaultAsync();

      if (user == null)
      {
        // Create user
        SPUserDto? spUser = null;

        try
        {
          spUser = await spApi.GetUser(dsUser.Id, MCServer.SP);
        }
        catch
        {
          return new ErrorResult(ResponseError.SPApiError, HttpStatusCode.ServiceUnavailable);
        }

        if (spUser == null)
        {
          return new ErrorResult(ResponseError.SPUserNotFound, HttpStatusCode.NotFound);
        }

        var mojangUser = await mjApi.GetUserByLogin(spUser.Username);

        if (mojangUser == null)
        {
          return new ErrorResult(ResponseError.MojangUserNotFound, HttpStatusCode.NotFound);
        }
        user = await CreateUser(spUser.Username, dsUser.Username, dsUser.Id.ToString(), mojangUser.Id, userMgr, db);
      }

      var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
      var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
      var roles = await userMgr.GetRolesAsync(user);
      var identity = new List<Claim>();

      foreach (var role in roles)
      {
        identity.Add(new Claim(ClaimTypes.Role, role));
      }

      identity.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

      var token = new JwtSecurityToken(
        conf["Settings:AppUrl"],
        conf["Settings:AppUrl"],
        identity,
        expires: DateTime.UtcNow.AddMinutes(int.Parse(conf["JwtConfig:ExpiresIn"] ?? "1440")),
        signingCredentials: credentials
      );

      var forbidden = await App.GetModule<BaseController>("base").CheckForbiddenAction(db, user, ForbiddenUserAction.ALL, mcs);
      if (forbidden != null)
      {
        return forbidden;
      }

      App.Logger.LogActionWithInititator("User authorized by Discord", user);
      return Results.Ok(new TokenDto
      {
        Token = new JwtSecurityTokenHandler().WriteToken(token),
        Expires = uint.Parse(conf["JwtConfig:ExpiresIn"] ?? "1440")
      });
    }

    public async Task<User> CreateUser(string name, string discordName, string discordId, string uuid, UserManager<User> userMgr, SQLiteDbContext db)
    {
      var user = new User
      {
        UserName = name,
        DiscordName = discordName,
        DiscordId = discordId,
        MinecraftUUID = uuid,
        CreatedAt = DateTime.UtcNow,
        IsActive = true,
      };

      var createResult = await userMgr.CreateAsync(user);
      var rolesResult = await userMgr.AddToRolesAsync(user, new string[] { AuthRoles.User });

      if (App.GetConfig("Settings")["SuperadminDiscordId"] == discordId)
      {
        await userMgr.AddToRolesAsync(user, new string[] { AuthRoles.Superadmin, AuthRoles.Admin });
      }

      if (!rolesResult.Succeeded || !createResult.Succeeded)
      {
        throw new Exception("Ошибка регистрации пользователя");
      }

      Dictionary<uint, double> balance = new();

      foreach (var mcservs in (MCServer[])Enum.GetValues(typeof(MCServer)))
      {
        var account = new Account
        {
          Mcs = mcservs,
          IsDefault = true,
          User = user
        };

        db.Accounts.Add(account);
        balance.Add((uint)mcservs, account.Balance);
      }

      await db.SaveChangesAsync();

      return user;
    }

    [ProducesResponseType(typeof(UserProfileDto), (int)HttpStatusCode.OK)]
    [Authorize(Roles = AuthRoles.User)]
    public async Task<IResult> GetMyProfile(ClaimsPrincipal claims, UserManager<User> userMgr, SQLiteDbContext db, MCServer mcs)
    {
      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      user.LatestActivity = DateTime.UtcNow;
      await userMgr.UpdateAsync(user);

      var accounts = db.Accounts
        .Include(acc => acc.User)
        .Where(acc => acc.User.Id == user.Id && acc.IsDefault)
        .ToDictionary(acc => (uint)acc.Mcs, acc => new UserAccountDto { Id = acc.Id, Balance = acc.Balance, IsDefault = acc.IsDefault, Mcs = acc.Mcs });

      return Results.Ok(new UserProfileDto
      {
        Id = user.Id,
        UserName = user.UserName ?? "",
        DiscordName = user.DiscordName,
        DiscordId = user.DiscordId,
        MinecraftUuid = user.MinecraftUUID,
        LatestActivity = user.LatestActivity,
        CreatedAt = user.CreatedAt,
        Roles = (await userMgr.GetRolesAsync(user)).ToArray(),
        Accounts = accounts
      });
    }

    public void Dispose()
    {
      httpClient.Dispose();
      GC.SuppressFinalize(this);
    }

    void IWebController.Inject(WebApplication server)
    {
      server.MapPost("auth/discord", AuthByDiscord);
      server.MapGet("user/me", GetMyProfile);
    }

    Task IWebController.RunTimingTask(SQLiteDbContext db, PreviewGen pGen)
    {
      return Task.CompletedTask;
    }
  }
}
