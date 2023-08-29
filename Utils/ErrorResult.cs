using spapp_backend.Core.Dtos;
using spapp_backend.Core.Enums;
using System.Net;

namespace spapp_backend.Utils
{
  public class ErrorResult : IResult
  {
    public HttpStatusCode statusCode { get; set; } = HttpStatusCode.BadRequest;
    public ResponseError error { get; set; } = ResponseError.Unknown;
    public object? detail { get; set; }

    public ErrorResult(ResponseError error)
    {
      this.error = error;
    }

    public ErrorResult(ResponseError error, object? detail)
    {
      this.error = error;
      this.detail = detail;
    }

    public ErrorResult(ResponseError error, HttpStatusCode statusCode = HttpStatusCode.BadRequest, object? detail = null)
    {
      this.error = error;
      this.detail = detail;
      this.statusCode = statusCode;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
      httpContext.Response.StatusCode = (int)statusCode;
      await httpContext.Response.WriteAsJsonAsync(new ResponseErrorDto
      {
        Code = (int)statusCode,
        Error = (int)error,
        Detail = detail
      });
    }
  }
}
