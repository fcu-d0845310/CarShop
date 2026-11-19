

namespace CarShop.Dtos.Common
{
    public class PageResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page {  get; set; }
        public int PageSize {  get; set; }
        public int TotalCount { get; set; }
    }
}
