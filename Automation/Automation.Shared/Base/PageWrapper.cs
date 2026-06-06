namespace Automation.Shared.Base;

public struct PaginationOptions
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    // XXX : may want to add SortBy and SortDirection
    
    public PaginationOptions()
    {
    }
}

public class Paginated<T>
{
    public List<T> Items { get; set;} = [];

    public long Total { get; set; } = -1;
    public PaginationOptions Options { get; set; }
}