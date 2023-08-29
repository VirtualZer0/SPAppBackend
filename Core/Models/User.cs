using Microsoft.AspNetCore.Identity;

namespace spapp_backend.Core.Models
{
  public class User : IdentityUser<uint>
  {
    public string MinecraftUUID { get; set; } = string.Empty;
    public string DiscordId { get; set; } = string.Empty;
    public string DiscordName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime LatestActivity { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }
}
