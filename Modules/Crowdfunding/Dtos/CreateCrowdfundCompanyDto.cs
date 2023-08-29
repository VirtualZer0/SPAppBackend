using spapp_backend.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace spapp_backend.Modules.Crowdfunding.Dtos
{
  public class CreateCrowdfundCompanyDto
  {
    public Guid[] Images { get; set; } = Array.Empty<Guid>();
    public Guid? Preview { get; set; } = null!;

    [Required]
    public double Goal { get; set; } = 0;

    [Required]
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    [Required]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public DateTime EndDate { get; set; }
  }
}
