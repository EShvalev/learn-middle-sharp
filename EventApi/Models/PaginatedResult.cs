namespace EventApi.Models
{
    public record PaginatedResult<T>(IEnumerable<T> Items, int CurrentPage, int OnPageItems, int TotalItems);
}
