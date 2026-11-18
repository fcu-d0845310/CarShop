using AngleSharp.Html.Parser;
using CarShop.Data;
using CarShop.Data.Entities;
using CarShop.Dtos.Post;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

namespace CarShop.Services
{
    public class PostService
    {
        private readonly AppDbContext _appDbContext;
        public PostService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<int?> CreatePostAsync(int userId, CreatePostRequest createPostRequest)
        {
            if(createPostRequest.CarId != null)
            {
                if(!await _appDbContext.Cars.AnyAsync(c => c.Id == createPostRequest.CarId))
                {
                    createPostRequest.CarId = null;
                }
            }
            string? checkResult = Sanitize(createPostRequest.Content);//處理ckeditor的結果

            if(checkResult == null)
            {
                return null;
            }
            CarPost p = new CarPost
            {
                CarId = createPostRequest.CarId,
                UserId = userId,
                Title = createPostRequest.Title,
                Content = checkResult,
                Category = createPostRequest.Category,
            };

            _appDbContext.CarPosts.Add(p);
            await _appDbContext.SaveChangesAsync();

            return p.Id;
        }

        // 過濾 CKEditor 送來的 HTML；過濾後沒有文字也沒有圖片時回傳 null
        private string? Sanitize(string content)
        {
            var san = new HtmlSanitizer();
            san.FilterUrl += (sender, e) =>
            {
                if (e.Tag.TagName == "IMG" && !e.OriginalUrl.StartsWith("/uploads/"))
                {
                    e.SanitizedUrl = null;
                }

            };
            string tmp = san.Sanitize(content);

            // 解析過濾後的 HTML，檢查是否還有內容
            var doc = new HtmlParser().ParseDocument(tmp);
            bool hasText = !string.IsNullOrWhiteSpace(doc.Body?.TextContent);
            bool hasImage = doc.QuerySelector("img[src]") != null;

            if (!hasText && !hasImage)
            {
                return null;
            }

            return tmp;
        }
    }
}
