namespace Automation.Shared
{
    public class PageWrapper<T>
    {
        public T Data { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
