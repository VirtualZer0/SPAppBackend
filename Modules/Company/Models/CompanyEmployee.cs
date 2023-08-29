using spapp_backend.Core.Models;

namespace spapp_backend.Modules.Company.Models
{
  public class CompanyEmployee
  {
    public int Id { get; set; }
    public User Employee { get; set; } = null!;
    public UserCompany Company { get; set; } = null!;
    public string JobTitle { get; set; } = string.Empty;
    public bool HaveOwnerAccess { get; set; } = false;
    public string? Telegram { get; set; } = null;
    public string? Vk { get; set; } = null;
  }
}
