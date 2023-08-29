using spapp_backend.Core.Models;
using spapp_backend.Db;

namespace spapp_backend.Modules.Crowdfunding.Models
{
  public class CrowdfundComment : BaseModel
  {
    public uint Id { get; set; }
    public CrowdfundCompany CrowdCompany { get; set; } = null!;
    public User User { get; set; } = null!;
    public string Comment { get; set; } = string.Empty;
  }
}
