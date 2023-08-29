using spapp_backend.Core.Models;

namespace spapp_backend.Modules.Market.Models
{
  public class ShopReview
  {
    public uint Id { get; set; }
    public User Author { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
    public ushort Rating { get; set; } = 5;
    public string Review { get; set; } = string.Empty;
  }
}
