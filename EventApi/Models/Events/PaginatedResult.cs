namespace EventApi.Models.Events
{
    public record PaginatedResult<T>(IEnumerable<T> Items, int CurrentPage, int CurrentPageSize, int TotalItems);
}
