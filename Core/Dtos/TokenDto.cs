namespace spapp_backend.Core.Dtos
{
  public class TokenDto
  {
    public string Token { get; set; } = string.Empty;
    public uint Expires { get; set; } = 60;
  }
}
