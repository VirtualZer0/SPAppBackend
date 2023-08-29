using spapp_backend.Core.Dtos;
using spapp_backend.Core.Models;
using spapp_backend.Modules.Crowdfunding.Models;

namespace spapp_backend.Modules.Crowdfunding.Dtos
{
  public class CrowdfundCompanyItemDto
  {
    public uint Id { get; set; }
    public UserInfoDto User { get; set; } = null!;
    public string[] Images { get; set; } = Array.Empty<string>();
    public string? Preview { get; set; } = null;
    public double Goal { get; set; } = 0;
    public double CurrentAmount { get; set; } = 0;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public bool IsOver { get; set; } = false;
    public int CommentCount { get; set; } = 0;
    public DateTime UpdatedAt { get; set;}
  }
}
