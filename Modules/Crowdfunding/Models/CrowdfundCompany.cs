using spapp_backend.Core.Models;
using spapp_backend.Db;

namespace spapp_backend.Modules.Crowdfunding.Models
{
  public class CrowdfundCompany : CrossServerModel
  {
    public uint Id { get; set; }
    public User User { get; set; } = null!;
    public List<FileMeta> Images { get; set; } = new();
    public FileMeta? Preview { get; set; } = null;
    public double Goal { get; set; } = 0;
    public double CurrentAmount { get; set; } = 0;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public bool IsOver { get; set; } = false;
    public List<AccountTransaction> Transactions { get; set; } = new();
    public List<CrowdfundComment> Comments { get; set; } = new();
  }
}
