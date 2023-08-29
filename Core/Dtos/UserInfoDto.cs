namespace spapp_backend.Core.Dtos
{
  public class UserInfoDto
  {
    public ulong Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MinecraftUuid { get; set; } = string.Empty;
  }
}
