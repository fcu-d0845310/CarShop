using CarShop.Dtos.Post;
using CarShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CarShop.Controllers
{
    [Route("api/posts")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly PostService _postService;
        public PostController(PostService postService)
        {
            _postService = postService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreatePostRequest createPostRequest)
        {
            Claim claim= User.FindFirst("sub")!;
            int userId = int.Parse(claim.Value);

            int? postId = await _postService.CreatePostAsync(userId, createPostRequest);
            if(postId == null)
            {
                return Problem(statusCode: 400, title: "內容不可以為空");
            }
            return Ok(postId);
        }

        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] PostQuery postQuery)
        {
            var result = await _postService.GetPostsAsync(postQuery);
            return Ok(result);
        }
    }
}
