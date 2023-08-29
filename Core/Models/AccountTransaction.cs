using spapp_backend.Core.Enums;
using spapp_backend.Db;

namespace spapp_backend.Core.Models
{
  public class AccountTransaction : CrossServerModel
  {
    public uint Id { get; set; }
    public User Initiator { get; set; } = null!;
    public TransactionType Type { get; set; } = TransactionType.Internal;
    public double Amount { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public bool IsSuccess { get; set; } = false;
    public TransactionFailReason? Reason { get; set; } = null;
  }
}
