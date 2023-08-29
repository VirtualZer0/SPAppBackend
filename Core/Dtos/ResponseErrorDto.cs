namespace spapp_backend.Core.Dtos
{
  public class ResponseErrorDto
  {
    public int Code { get; set; }
    public int Error { get; set; }
    public object? Detail { get; set; }
  }
}
