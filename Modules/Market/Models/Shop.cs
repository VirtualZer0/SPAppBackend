using spapp_backend.Core.Models;
using spapp_backend.Db;

namespace spapp_backend.Modules.Market.Models
{
  public class Shop : CrossServerModel
  {
    public uint Id { get; set; }
    public bool IsActive { get; set; } = false;
    public User Owner { get; set; } = null!;
    public ShopSlot[] Slots { get; set; } = Array.Empty<ShopSlot>();
    public string Name { get; set; } = null!;
    public FileMeta Logo { get; set; } = null!;
  }
}
