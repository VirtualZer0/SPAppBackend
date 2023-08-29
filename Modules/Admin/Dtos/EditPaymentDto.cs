using spapp_backend.Core.Enums;

namespace spapp_backend.Modules.Admin.Dtos
{
  public class EditPaymentDto
  {
    public PaymentStatus? Status { get; set; }
    public DateTime? ExpirationDate { get; set; }
  }
}
