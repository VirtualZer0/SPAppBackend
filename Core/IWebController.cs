using spapp_backend.Db;
using spapp_backend.Utils;

namespace spapp_backend.Core
{
  public interface IWebController
  {
    public void Inject(WebApplication server);
    public Task RunTimingTask(SQLiteDbContext db, PreviewGen pGen);
  }
}
