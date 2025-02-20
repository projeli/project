namespace ProjectService.Domain.Results;

public class PagedResult<T>(List<T>? data = null, string? message = "", bool success = true, Dictionary<string, string[]>? errors = null, int page = 0, int pageSize = 0, int totalCount = 0, int totalPages = 0) : Result<List<T>>(data, message, success, errors)
{
    public int Page { get; init; } = page;
    public int PageSize { get; init; } = pageSize;
    public int TotalCount { get; init; } = totalCount;
    public int TotalPages { get; init; } = totalPages;
}