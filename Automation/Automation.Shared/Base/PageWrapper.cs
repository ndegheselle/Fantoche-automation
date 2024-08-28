namespace Automation.Shared.Base
{
    public interface IPageWrapper<out T> : IEnumerable<T>
    {
        public long Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class ListPageWrapper<T> : List<T>, IPageWrapper<T>
    {
        public long Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public ListPageWrapper()
        { 
        }

        public ListPageWrapper(int page, int pageSize, long total)
        {
            Total = total;
            Page = page;
            PageSize = pageSize;
        }
    }
}