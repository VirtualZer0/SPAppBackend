using spapp_backend.Core.Models;
using spapp_backend.Db;

namespace spapp_backend.Modules.Market.Models
{
  public class ShopSlot : BaseModel
  {
    public uint Id { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ExpirationDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public ShopItem Item { get; set; } = null!;
    public FileMeta[] Images { get; set; } = Array.Empty<FileMeta>();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Price { get; set; }
  }
}
