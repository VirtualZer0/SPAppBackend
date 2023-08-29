namespace spapp_backend.Core.Dtos
{
  public class WithdrawMoneyDto
  {
    public uint AccountId { get; set; }
    public uint Amount { get; set; }
    public string Card { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
  }
}
