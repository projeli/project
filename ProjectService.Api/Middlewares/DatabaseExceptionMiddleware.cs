using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Projeli.Shared.Domain.Extensions;
using Projeli.Shared.Domain.Results;

namespace ProjectService.Api.Middlewares;

public class DatabaseExceptionMiddleware(RequestDelegate next, ILogger<DatabaseExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateException exception)
        {
            if (exception.InnerException is PostgresException postgresException)
            {
                context.Response.ContentType = "application/json";
                switch (postgresException.SqlState)
                {
                    // Unique violation (e.g., duplicate key)
                    case "23505":
                        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                        var column = postgresException.ConstraintName!.Split('_').Last();
                        var table = postgresException.TableName;
                        var entityName = table is null ? "record" : table.EndsWith('s') ? table[..^1] : table;
                        var error = $"A {entityName.ToSnakeCase().Replace('_', ' ').ToLower()} with the same {column.ToSnakeCase().Replace('_', ' ').ToLower()} already exists";
                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(
                                new Result<object>(null, error, false, new Dictionary<string, string[]>(
                                    [new KeyValuePair<string, string[]>(column, [error])]
                                )), JsonSerializerOptions));
                        break;

                    // Foreign key violation
                    case "23503":
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                            { message = "A foreign key violation occurred" }));
                        break;

                    // Not null violation
                    case "23502":
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                            { message = "A required field cannot be null" }));
                        break;

                    // Check constraint violation
                    case "23514":
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                            { message = "A check constraint was violated" }));
                        break;

                    // Exclusion constraint violation
                    case "23P01":
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                            { message = "An exclusion constraint was violated" }));
                        break;

                    // Invalid input syntax (e.g., wrong data type)
                    case "22P02":
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                            { message = "Invalid input syntax for type" }));
                        break;

                    // Division by zero
                    case "22012":
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(new { message = "Division by zero" }));
                        break;

                    // String data right truncation
                    case "22001":
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                            { message = "String data is too long for the defined column" }));
                        break;

                    default:
                        await DefaultResponse(context);
                        break;
                }
            }
            else
            {
                await DefaultResponse(context);
            }
        }
    }

    private async Task DefaultResponse(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
            { message = "An error occurred while processing the request" }));
    }
}