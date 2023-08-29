namespace spapp_backend.Core.Dtos
{
  public class ItemList<T>
  {
    public List<T> Items { get; set; } = null!;
    public int Count { get; set; } = 0;
  }
}
