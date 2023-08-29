using spapp_backend.Core.Models;
using spapp_backend.Db;

namespace spapp_backend.Modules.Company.Models
{
  public class CompanyVacancy : BaseModel
  {
    public uint Id { get; set; }
    public UserCompany Company { get; set; } = null!;
    public VacancyType Type { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Links { get; set; } = "[]";
    public FileMeta[] Images { get; set; } = Array.Empty<FileMeta>();
    public string Salary { get; set; } = string.Empty;
  }
}
