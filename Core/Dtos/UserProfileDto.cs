using spapp_backend.Core.Enums;
using System.Text.Json.Serialization;

namespace spapp_backend.Core.Dtos
{
  public class UserProfileDto
  {
    public ulong Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DiscordName { get; set; } = string.Empty;
    public string DiscordId { get; set; } = string.Empty;
    public string MinecraftUuid { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public DateTime? LatestActivity { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Dictionary<uint, UserAccountDto> Accounts { get; set; } = new();
  }
}
