namespace Automation.Shared.Base
{
    public class ListPageWrapper<T>
    {
        public List<T> Data { get; set;} = new List<T>();

        public long Total { get; set; } = -1;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}