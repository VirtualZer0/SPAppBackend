using spapp_backend.Core.Models;
using spapp_backend.Db;

namespace spapp_backend.Modules.Company.Models
{
  public class UserCompany : CrossServerModel
  {
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Links { get; set; } = "[]";
    public FileMeta[] Images { get; set; } = Array.Empty<FileMeta>();
    public User Owner { get; set; } = null!;
    public CompanyEmployee[] Employees { get; set; } = Array.Empty<CompanyEmployee>();
    public CompanyVacancy[] Jobs { get; set; } = Array.Empty<CompanyVacancy>();
    public CompanyAutopayment[] Autopayments { get; set; } = Array.Empty<CompanyAutopayment>();
    public Account Account { get; set; } = null!;
  }
}
