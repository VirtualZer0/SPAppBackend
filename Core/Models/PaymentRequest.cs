using spapp_backend.Core.Enums;
using spapp_backend.Db;

namespace spapp_backend.Core.Models
{
  public class PaymentRequest : CrossServerModel
  {
    public Guid Id { get; set; }
    public User Initiator { get; set; } = null!;
    public uint Amount { get; set; }
    public Guid TransCode { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string PayUrl { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; } = DateTime.UtcNow.AddDays(1);
    public Account Destination { get; set; } = null!;
    public string Payer { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Awaiting;
  }
}
