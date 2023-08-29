namespace spapp_backend.Db
{
  public class BaseModel
  {
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
  }
}
