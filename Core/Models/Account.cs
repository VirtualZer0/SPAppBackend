using spapp_backend.Core.Enums;
using spapp_backend.Db;

namespace spapp_backend.Core.Models
{
  public class Account : CrossServerModel
  {
    public uint Id { get; set; }
    public User User { get; set; } = null!;
    public double Balance { get; set; } = 0;
    public string Name { get; set; } = "main";
    public bool IsDefault { get; set; } = false;
  }
}
