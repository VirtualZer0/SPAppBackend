using Discord;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using spapp_backend.Core.Dtos;
using spapp_backend.Core.Enums;
using spapp_backend.Core.Models;
using spapp_backend.Db;
using spapp_backend.Utils;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace spapp_backend.Core.Controllers
{
  public class FileController : IWebController
  {
    static readonly uint MAX_FILESIZE = 1024 * 1024 * 5;
    static readonly uint MAX_FILES_PER_USER = 5;
    static readonly int MAX_FILES_PER_5_MINUTES = 25;
    static readonly string[] WHITELIST_EXT = new string[]
    {
      "image/avif", "image/bmp", "text/csv", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      "image/gif", "image/jpeg", "image/png", "image/svg+xml", "image/webp", "text/plain"
    };

    [Authorize(Roles = AuthRoles.User)]
    [ProducesResponseType(typeof(FileInfoDto[]), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    public async Task<IResult> UploadFile(SQLiteDbContext db, ClaimsPrincipal claims, UserManager<User> userMgr, [FromForm] IFormFileCollection files, MCServer mcs)
    {
      if (files.Count == 0 || files.Count > MAX_FILES_PER_USER)
      {
        return new ErrorResult(ResponseError.WrongFilesCount, HttpStatusCode.UnprocessableEntity);
      }

      var user = await userMgr.GetUserAsync(claims);
      if (user == null)
      {
        return new ErrorResult(ResponseError.UserNotFound, HttpStatusCode.NotFound);
      }

      var forbidden = await App.GetModule<BaseController>("base").CheckForbiddenAction(db, user, ForbiddenUserAction.UPLOAD_FILES, mcs);
      if (forbidden != null)
      {
        return forbidden;
      }

      if (await CheckFilesLimit(user, db))
      {
        return new ErrorResult(ResponseError.TooManyFilesInShortTime, HttpStatusCode.TooManyRequests);
      }

      var fileMetas = new List<FileMeta>();

      foreach (var file in files)
      {
        if (file.Length > MAX_FILESIZE)
        {
          return new ErrorResult(ResponseError.FileTooLarge, HttpStatusCode.UnprocessableEntity);
        }

        if (!WHITELIST_EXT.Contains(file.ContentType))
        {
          return new ErrorResult(ResponseError.WrongFileFormat, HttpStatusCode.UnprocessableEntity);
        }

        var fMeta = new FileMeta
        {
          Author = user,
          Ext = Path.GetExtension(file.FileName),
          Folder = user.Id
        };
        fileMetas.Add(fMeta);
      }

      foreach(var fMeta in fileMetas)
      {
        db.Add(fMeta);
      }

      await db.SaveChangesAsync();

      var userFilesDir = Path.Combine(App.GetStaticPath(), "uploads", user.Id.ToString());
      if (!Directory.Exists(userFilesDir))
      {
        Directory.CreateDirectory(userFilesDir);
      }
      var filePath = Path.GetTempFileName();

      for(int i = 0; i < files.Count; i++)
      {
        using (var stream = File.Create(filePath))
        {
          await files[i].CopyToAsync(stream);
        }
        
        var newFileName = $"{fileMetas[i].Id}{fileMetas[i].Ext}".ToLower();
        File.Move(filePath, Path.Combine(userFilesDir, newFileName), true);
        if (files[i].ContentType.Contains("image"))
        {
          try
          {
            fileMetas[i] = await TryOptimizeImage(fileMetas[i], db);
          }
          catch (Exception ex)
          {
            App.Logger.WriteExceptionLog(ex, "imagemagick.txt");
          }
        }
      }

      return Results.Ok(fileMetas.Select(f => new FileInfoDto { Id = f.Id, Name = f.FullPath, UserId = f.Folder}).ToArray());
    }

    public async Task<bool> CheckFilesLimit(User user, SQLiteDbContext db)
    {
      var checkTime = DateTime.UtcNow.AddMinutes(-5);
      var lastUploads = db.Files
        .Include(f => f.Author)
        .Where(f => f.CreatedAt >= checkTime && f.Author.Id == user.Id)
        .Take(MAX_FILES_PER_5_MINUTES + 1);
      return await lastUploads.CountAsync() > MAX_FILES_PER_5_MINUTES;
    }

    public async Task<FileMeta> TryOptimizeImage(FileMeta file, SQLiteDbContext db)
    {
      var filePath = Path.Combine(App.GetStaticPath(), "uploads", file.FullPath);
      if (!File.Exists(filePath))
      {
        return file;
      }

      using var image = new MagickImage(filePath);
      var isChanged = false;

      if (image.Width > 1920)
      {
        image.Resize(1920, 0);
        isChanged = true;
      }

      if (image.Height > 1920)
      {
        image.Resize(0, 1920);
        isChanged = true;
      }

      var optimizer = new ImageOptimizer()
      {
        IgnoreUnsupportedFormats = true,
        OptimalCompression = true,
      };

      var newPath = filePath;

      if (file.Ext != ".webp" && file.Ext != ".jpg")
      {
        image.Format = MagickFormat.WebP;
        image.Quality = 95;
        file.Ext = ".webp";
        File.Delete(filePath);
        newPath = Path.Combine(App.GetStaticPath(), "uploads", file.FullPath);
        await image.WriteAsync(newPath);
        optimizer.LosslessCompress(newPath);
        db.Update(file);
        await db.SaveChangesAsync();
      }
      else
      {
        if (isChanged)
        {
          await image.WriteAsync(filePath);
        }
        optimizer.LosslessCompress(filePath);
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        File.SetUnixFileMode(newPath, UnixFileMode.OtherWrite | UnixFileMode.OtherRead | UnixFileMode.UserRead | UnixFileMode.UserWrite);
      }

      return file;
    }
    
    public async Task<FileMeta> GetFileMetaById(SQLiteDbContext db, Guid id)
    {
      return await db.Files.Where(f => f.Id == id).FirstAsync();
    }

    public async Task RemoveFile(SQLiteDbContext db, User user, Guid id)
    {
      var file = await db.Files.Where(f => f.Id == id).FirstOrDefaultAsync();
      if (file == null || (file.Folder != user.Id))
      {
        return;
      }

      var filesDir = Path.Combine(App.GetStaticPath(), "uploads");

      try
      {
        File.Delete(Path.Combine(filesDir, file.FullPath));
        db.Remove(file);
        await db.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        App.Logger.WriteExceptionLog(ex, "files.txt");
      }
    }

    public async Task RemoveFiles(SQLiteDbContext db, User user, Guid[] ids)
    {
      foreach (var id in ids)
      {
        await RemoveFile(db, user, id);
      }
    }

    void IWebController.Inject(WebApplication server)
    {
      server.MapPost("files/upload", UploadFile);
    }

    Task IWebController.RunTimingTask(SQLiteDbContext db, PreviewGen pGen)
    {
      return Task.CompletedTask;
    }
  }
}
