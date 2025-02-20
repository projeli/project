using System.Net;

namespace ProjectService.Infrastructure.Exceptions.Http;

public class UnauthorizedException(string message = nameof(HttpStatusCode.Unauthorized)) 
    : HttpException(HttpStatusCode.Unauthorized, message)
{
    
}