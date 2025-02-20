using System.Net;

namespace ProjectService.Infrastructure.Exceptions.Http;

public abstract class HttpException(HttpStatusCode statusCode, string message) 
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}