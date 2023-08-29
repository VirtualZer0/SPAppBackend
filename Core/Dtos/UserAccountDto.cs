using spapp_backend.Core.Enums;

namespace spapp_backend.Core.Dtos
{
  public class UserAccountDto
  {
    public uint Id { get; set; }
    public double Balance { get; set; }
    public bool IsDefault { get; set; }
    public MCServer Mcs { get; set; }
  }
}
