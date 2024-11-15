﻿using spapp_backend.Db;

namespace spapp_backend.Core
{
  public interface IWebController
  {
    public void Inject(WebApplication server);
    public Task RunTimingTask(SQLiteDbContext db);
  }
}
