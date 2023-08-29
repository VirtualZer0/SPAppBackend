using Microsoft.AspNetCore.Identity;
using spapp_backend.Core.Models;
using spapp_backend.Db;
using System.Security.Claims;

namespace spapp_backend.Utils
{
  public class Logger
  {
    public void LogToConsole(string message, string? service = null)
    {
      if (service == null)
      {
        Console.WriteLine($"{DateTime.UtcNow:dd.MM.yy HH:mm:ss}\t{message}");
      }
      else
      {
        Console.WriteLine($"{DateTime.UtcNow:dd.MM.yy HH:mm:ss}\t[{service}] {message}");
      }
    }

    public async void LogActionWithInititator(string action, ClaimsPrincipal claims, UserManager<User> userMgr, bool isDanger = false)
    {
      try
      {
        var acc = await userMgr.GetUserAsync(claims);
        LogActionWithInititator(action, acc, isDanger);
      }
      catch { }
    }

    public async void LogActionWithInititator(string action, User? acc, bool isDanger = false)
    {
      try
      {
        using var db = new SQLiteDbContext();
        var logEntry = new LogEntry
        {
          Text = action,
          Initiator = acc != null ? $"{acc.Id} | {acc.UserName}" : "Unknown",
          IsDanger = isDanger
        };

        db.Logs.Add(logEntry);
        await db.SaveChangesAsync();
      }
      catch { }
    }

    public async void LogActionWithSystem(string action, bool isDanger = false)
    {
      try
      {
        using var db = new SQLiteDbContext();
        var logEntry = new LogEntry
        {
          Text = action,
          Initiator = "System",
          IsDanger = isDanger
        };

        db.Logs.Add(logEntry);
        await db.SaveChangesAsync();
      }
      catch { }
    }

    public async void WriteExceptionLog(Exception ex, string file = "errors.txt")
    {
      await File.AppendAllTextAsync(Path.Combine("Data", "Logs", file), $"\n\n{DateTime.UtcNow:dd.MM.yy HH:mm:ss}\t{ex}");
    }
  }
}
