namespace spapp_backend.Core.Dtos
{
  public class OAuthTokenDto
  {
#pragma warning disable IDE1006 // Стили именования
    public string access_token { get; set; } = string.Empty;
    public string token_type { get; set; } = string.Empty;
    public ulong expires_in { get; set; } = 0;
    public string refresh_token { get; set ; } = string.Empty;
    public string scope { get; set; } = string.Empty;
#pragma warning restore IDE1006 // Стили именования
  }
}
