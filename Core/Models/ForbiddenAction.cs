using spapp_backend.Core.Enums;
using spapp_backend.Db;

namespace spapp_backend.Core.Models
{
  public class ForbiddenAction : CrossServerModel
  {
    public uint Id { get; set; }
    public User User { get; set; } = null!;
    public ForbiddenUserAction Action { get; set; }
    public DateTime ForbiddenUntil { get; set; }
    public string Reason { get; set; } = string.Empty;
  }
}
