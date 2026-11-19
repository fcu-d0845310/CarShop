namespace CarShop.Dtos.Post
{
    public class PostListItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int? CarId { get; set; }
        public string? CarName { get; set; }
        public int CommentCount { get; set; }

    }
}
