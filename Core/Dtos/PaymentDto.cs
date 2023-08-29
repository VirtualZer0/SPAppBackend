using spapp_backend.Core.Enums;
using spapp_backend.Core.Models;

namespace spapp_backend.Core.Dtos
{
  public class PaymentDto
  {
    public Guid Id { get; set; }
    public uint Amount { get; set; }
    public string PayUrl { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; } = DateTime.UtcNow.AddDays(1);
    public DateTime CreatedAt { get; set; }
    public uint DestinationId { get; set; }
    public UserInfoDto User { get; set; } = null!;
    public string Payer { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Awaiting;
  }
}
