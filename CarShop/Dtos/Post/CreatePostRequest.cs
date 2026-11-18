using CarShop.Data.Constants;
using System.ComponentModel.DataAnnotations;

namespace CarShop.Dtos.Post
{
    public class CreatePostRequest
    {
        public int? CarId { get; set; }
        [Required]
        [MaxLength(50)]
        public string Title {  get; set; } = string.Empty;
        [Required]
        [MaxLength(20000)]
        public string Content {  get; set; } = string.Empty;
        [Required]
        [AllowedValues(PostCategory.Question, PostCategory.Review, PostCategory.Discussion, PostCategory.Maintenance,ErrorMessage = "分類不正確")]
        public string Category {  get; set; } = string.Empty;
    }
}
