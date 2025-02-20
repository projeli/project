namespace ProjectService.Domain.Results;

public class Result<T>(T? data, string? message = null, bool success = true, Dictionary<string, string[]>? errors = null) : IResult<T>
{
    public T? Data { get; init;  } = data;
    public bool Success { get; init; } = success;
    public string? Message { get; set; } = message;
    public Dictionary<string, string[]> Errors { get; set; } = errors ?? new Dictionary<string, string[]>();
    
    public static IResult<T> Fail(string message)
    {
        return new Result<T>(default, message, false);
    }
    
    public static IResult<T?> NotFound()
    {
        return new Result<T?>(default, "Not found", false);
    }

    public static IResult<T> ValidationFail(Dictionary<string, string[]> errors)
    {
        return new Result<T>(default, "Validation failed", false) { Errors = errors };
    }
}