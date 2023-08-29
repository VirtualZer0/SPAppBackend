namespace spapp_backend.Modules.Market.Models
{
  public class ShopItem
  {
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ushort StackAmount { get; set; } = 64;
    public string ItemImg { get; set; } = string.Empty;
    public double MinPrice { get; set; } = 0;
  }
}
