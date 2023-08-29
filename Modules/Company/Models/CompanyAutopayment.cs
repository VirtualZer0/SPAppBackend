using spapp_backend.Modules.Company.Enums;

namespace spapp_backend.Modules.Company.Models
{
    public class CompanyAutopayment
  {
    public uint Id { get; set; }
    public bool IsInternal { get; set; } = true;
    public UserCompany Company { get; set; } = null!;
    public AutopaymentPeriod Period { get; set; } = AutopaymentPeriod.EVERY_WEEK;
    public DateTime FristPayment { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
  }
}
