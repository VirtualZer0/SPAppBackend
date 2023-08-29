namespace spapp_backend.Core.Dtos
{
  public class CreatePaymentDto
  {
    public uint AccountId { get; set; }
    public uint Amount { get; set; }
    public string RedirectTo { get; set; } = string.Empty;
  }
}
