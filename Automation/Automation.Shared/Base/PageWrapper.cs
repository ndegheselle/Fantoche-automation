namespace Automation.Shared.Base
{
    public class PageWrapper<T>
    {
        public IEnumerable<T>? Data { get; set; }
        public long Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; }
    }
}