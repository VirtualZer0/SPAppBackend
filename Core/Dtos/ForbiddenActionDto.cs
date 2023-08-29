using spapp_backend.Core.Enums;

namespace spapp_backend.Core.Dtos
{
  public class ForbiddenActionDto
  {
    public ForbiddenUserAction Action { get; set; }
    public DateTime ForbiddenUntil { get; set; }
    public string Reason { get; set; } = string.Empty;
  }
}
