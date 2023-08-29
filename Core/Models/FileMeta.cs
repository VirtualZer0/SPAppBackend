using spapp_backend.Db;
using System.ComponentModel.DataAnnotations.Schema;

namespace spapp_backend.Core.Models
{
  public class FileMeta : BaseModel
  {
    public Guid Id { get; set; }
    public User Author { get; set; } = null!;
    public uint Folder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ext { get; set; } = string.Empty;
    public string FullPath { get => $"{Path.Combine(Folder.ToString(), Id.ToString())}{Ext}".Replace('\\', '/'); }
  }
}
