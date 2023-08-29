using spapp_backend.Db;

namespace spapp_backend.Core.Models
{
  public class LogEntry : BaseModel
  {
    public uint Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Initiator { get; set; } = "System";
    public bool IsDanger { get; set; } = false;
  }
}
