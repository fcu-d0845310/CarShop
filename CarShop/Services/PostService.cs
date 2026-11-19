using AngleSharp.Html.Parser;
using CarShop.Data;
using CarShop.Data.Entities;
using CarShop.Dtos.Common;
using CarShop.Dtos.Post;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<PageResult<PostListItem>> GetPostsAsync(PostQuery postQuery)
        {
            if(postQuery.Page < 1)
            {
                postQuery.Page = 1;
            }

            if(postQuery.PageSize < 1)
            {
                postQuery.PageSize = 20;
            }
            else if(postQuery.PageSize > 50)
            {
                postQuery.PageSize = 50;
            }

            //組合查詢條件
            var query = _appDbContext.CarPosts.AsNoTracking();
            if(postQuery.CarId != null)
            {
                query = query.Where(c => c.CarId == postQuery.CarId);
            }
            
           
            if(!string.IsNullOrEmpty(postQuery.Category))
            {
                query = query.Where(c => c.Category == postQuery.Category);
            }


            int totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Skip((postQuery.Page - 1) * postQuery.PageSize)
                .Take(postQuery.PageSize)
                .Select(p => new PostListItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    Category = p.Category,
                    CreatedAt = DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc),//時區不對會有問題所以在這裡轉換
                    AuthorName = p.User.Name,    
                    CarId = p.CarId,
                    CarName = p.Car != null ? p.Car.Brand + " " + p.Car.Model : null,
                    CommentCount = p.CarPostComments.Count()
                })
                .ToListAsync();

            return new PageResult<PostListItem>
            {
                Items = items,
                Page = postQuery.Page,
                PageSize = postQuery.PageSize,
                TotalCount = totalCount
            };
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
            string returnContent = san.Sanitize(content);

            // 解析過濾後的 HTML，檢查是否還有內容
            var doc = new HtmlParser().ParseDocument(returnContent);
            bool hasText = !string.IsNullOrWhiteSpace(doc.Body?.TextContent);
            bool hasImage = doc.QuerySelector("img[src]") != null;

            if (!hasText && !hasImage)
            {
                return null;
            }

            return returnContent;
        }
    }
}
