using Microsoft.AspNetCore.Routing.Constraints;

namespace spapp_backend.Core.Dtos
{
  public class CommentDto
  {
    public uint Id { get; set; }
    public UserInfoDto User { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
  }
}
