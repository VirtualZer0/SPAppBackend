using Discord;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using spapp_backend.Core;
using spapp_backend.Core.Models;
using spapp_backend.Db;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using spapp_backend.Core.Enums;
using spapp_backend.Core.Controllers;
using spapp_backend.Utils;
using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using spapp_backend.Modules.Crowdfunding.Controllers;
using spapp_backend.Modules.Admin.Controllers;
using System.Net;

namespace spapp_backend
{
  public class App
  {
    #pragma warning disable CS8618
    public static WebApplication WebServer { get; private set; }
    #pragma warning restore CS8618

    public static Dictionary<string, IWebController> Modules { get; private set; } = new();
    public static Logger Logger { get; private set; } = new();
    public static PreviewGen PreviewGen { get; private set; } = new();

    private static readonly Timer TimingTasksTimer = new(RunTimingTasks, null, Timeout.Infinite, Timeout.Infinite);

    public static IConfiguration GetConfig(string? section = null)
    {
      if (section != null)
      {
        return WebServer.Configuration.GetSection(section);
      }
      else
      {
        return WebServer.Configuration;
      }
    }

    public static T GetModule<T>(string key)
    {
      return (T)Modules[key];
    }

    public static string GetStaticPath()
    {
      return Path.Combine(
        WebServer.Environment.ContentRootPath,
        WebServer.Configuration["Settings:StaticDir"] ?? "Data/Static"
      );
    }

    public static void Init(string[] args)
    {
      AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
      {
        Logger.WriteExceptionLog((Exception)e.ExceptionObject);
      };

      var builder = WebApplication.CreateBuilder(args);

#if DEBUG
      IdentityModelEventSource.ShowPII = true;
#else
      IdentityModelEventSource.ShowPII = false;
#endif

      var resetJwtKey = false;

      using (var db = new SQLiteDbContext())
      {
        if (!File.Exists("Data/Main.db"))
        {
          resetJwtKey = true;
          db.Database.Migrate();
        }

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
      }

      if (!Directory.Exists("Data/Logs"))
      {
        Directory.CreateDirectory("Data/Logs");
      }

      // Add OpenAPI
      builder.Services.AddSwaggerGen(o =>
      {
        // using System.Reflection;
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

        o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
          Name = "Authorization",
          Type = SecuritySchemeType.ApiKey,
          Scheme = "Bearer",
          BearerFormat = "JWT",
          In = ParameterLocation.Header,
          Description = "JSON Web Token based security",
        });

        o.AddSecurityRequirement(
          new OpenApiSecurityRequirement
          {
            {
              new OpenApiSecurityScheme
              {
                Reference = new OpenApiReference
                {
                  Type = ReferenceType.SecurityScheme,
                  Id = "Bearer"
                }
              },
              Array.Empty<string>()
            }
          }
        );

        o.TagActionsBy(api =>
        {
          var str = api.RelativePath?.Split("/")[0] ?? "unknown";
          return new[] { char.ToUpper(str[0]) + str[1..] };
        });
        o.DocInclusionPredicate((name, api) => true);
      });

      builder.Services.AddEndpointsApiExplorer();

      builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
      {
        o.SerializerOptions.PropertyNameCaseInsensitive = false;
        o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
      });

      // Add db context
      builder.Services.AddDbContext<SQLiteDbContext>();

      // Add auth
      builder.Services.AddIdentity<User, IdentityRole<uint>>(o =>
      {
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.SignIn.RequireConfirmedEmail = false;
        o.SignIn.RequireConfirmedPhoneNumber = false;
      })
      .AddRoleManager<RoleManager<IdentityRole<uint>>>()
      .AddEntityFrameworkStores<SQLiteDbContext>();

      // Add endpoints
      var profileModule = new ProfileController();

      if (resetJwtKey)
      {
        profileModule.ResetJWTSecret();
      }

      Modules.Add("base", new BaseController());
      Modules.Add("file", new FileController());
      Modules.Add("profile", profileModule);
      Modules.Add("payment", new PaymentController());
      Modules.Add("crowdfund", new CrowdfundController());
      Modules.Add("mainAdmin", new MainAdminController());

      builder.Services
        .AddAuthorization()
        .AddAuthentication(x =>
        {
          x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
          x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
          o.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Settings:AppUrl"],
            ValidAudience = builder.Configuration["Settings:AppUrl"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(profileModule.JwtSecret ?? ""))
          };

          o.Events = new JwtBearerEvents
          {
            OnMessageReceived = context =>
            {
              var accessToken = context.Request.Query["access_token"];

              var path = context.HttpContext.Request.Path;
              if (!string.IsNullOrEmpty(accessToken) &&
                  (path.StartsWithSegments("/signalr")))
              {
                // Read the token out of the query string
                context.Token = accessToken;
              }
              return Task.CompletedTask;
            }
          };
        });

      // Create default roles
      var serviceProvider = builder.Services.BuildServiceProvider();
      var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<uint>>>();
      foreach (var role in (int[])Enum.GetValues(typeof(Role)))
      {
        bool roleExists = roleManager.RoleExistsAsync(role.ToString()).Result;
        if (!roleExists)
        {
          roleManager.CreateAsync(new IdentityRole<uint> { Name = role.ToString() });
        }
      }

      // Add services to the container
      //builder.Services.AddSignalR();
      var spApi = new SPApi();

      spApi.SetCreds(
        MCServer.SP,
        builder.Configuration.GetSection("ThirdParty")["SPId"] ?? "",
        builder.Configuration.GetSection("ThirdParty")["SPToken"] ?? ""
      );

      spApi.SetCreds(
        MCServer.SPM,
        builder.Configuration.GetSection("ThirdParty")["SPMId"] ?? "",
        builder.Configuration.GetSection("ThirdParty")["SPMToken"] ?? ""
      );

      builder.Services.AddSingleton(spApi);
      builder.Services.AddSingleton(PreviewGen);
      builder.Services.AddSingleton(new MojangApi());

      builder.Services.AddCors(options =>
      {
        options.AddPolicy("CorsPolicy", b =>
        {
          b.AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithOrigins(builder.Configuration.GetSection("Settings")["FrontendUrl"] ?? "http://localhost");
        });
      });

      WebServer = builder.Build();

      if (!Directory.Exists(GetStaticPath()))
      {
        Directory.CreateDirectory(GetStaticPath());
        Directory.CreateDirectory(Path.Combine(GetStaticPath(), "uploads"));
      }
    }

    public static void Run()
    {
      var cultureInfo = new CultureInfo("en-US");

      CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
      CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

      Logger.LogToConsole("Starting...", "App");

      ConfigureWebServer();
      //Signal = WebServer.Services.GetRequiredService<IHubContext<MainHub>>();

      //RunTimingTasks(null);
      PreviewGen.Init().Wait();
      TimingTasksTimer.Change(0, 60 * 60 * 1000);
      WebServer.Run();
    }

    private static void RunTimingTasks(object? state)
    {
      var db = new SQLiteDbContext();
      foreach (var module in Modules)
      {
        module.Value.RunTimingTask(db, PreviewGen).Wait();
      }
    }

    private static void ConfigureWebServer()
    {

      if (!Directory.Exists(WebServer.Configuration["Settings:StaticDir"]))
      {
        Directory.CreateDirectory(WebServer.Configuration["Settings:StaticDir"] ?? "Data/Static");
      }

      
      WebServer.UseDeveloperExceptionPage();

      WebServer.UseCors("CorsPolicy");

      WebServer.UseStaticFiles(new StaticFileOptions
      {
        FileProvider = new PhysicalFileProvider(
          GetStaticPath()
        ),
        RequestPath = "/static"
      });

      // Add endpoints
      foreach (var module in Modules)
      {
        module.Value.Inject(WebServer);
      }

      //WebServer.MapHub<MainHub>("/signalr");

      WebServer.UseAuthentication();
      WebServer.UseAuthorization();

      if (bool.Parse(WebServer.Configuration.GetSection("Settings")["EnableSwagger"] ?? "false"))
      {
        WebServer.UseSwagger();
        WebServer.UseSwaggerUI();
      }

      WebServer.UseExceptionHandler(e =>
      {
        e.Run(async ec =>
        {
          var exception =
               ec.Features.Get<IExceptionHandlerPathFeature>();

          if (exception != null)
          {
            Logger.WriteExceptionLog(exception.Error, "webserver.txt");
          }

          await ec.Response.WriteAsJsonAsync(new ErrorResult(ResponseError.Unknown, HttpStatusCode.InternalServerError, exception?.Error.ToString()));
        });
      });
    }
  }

  
}
