namespace CarShop.Dtos.Post
{
    public class PostQuery
    {
        public int? CarId { get; set; }
        public string? Category { get; set; }
        public int Page { get; set; } = 1;        // 沒填就是第 1 頁
        public int PageSize { get; set; } = 20;   // 沒填就是 20 筆
    }
}
