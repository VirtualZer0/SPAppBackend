using spapp_backend.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace spapp_backend.Modules.Crowdfunding.Dtos
{
  public class EditCrowdfundCompanyDto
  {
    public Guid[]? Images { get; set; }
    public Guid? Preview { get; set; }
    public double? Goal { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ShortDescription { get; set; }
    public DateTime? EndDate { get; set; }
  }
}
