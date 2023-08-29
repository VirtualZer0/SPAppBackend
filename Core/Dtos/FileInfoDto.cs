using spapp_backend.Core.Models;

namespace spapp_backend.Core.Dtos
{
  public class FileInfoDto
  {
    public Guid Id { get; set; }
    public uint UserId { get; set; }
    public string Name { get; set; } = string.Empty;
  }
}
