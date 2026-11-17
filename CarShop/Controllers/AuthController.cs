using CarShop.Dtos.Auth;
using CarShop.Exceptions;
using CarShop.Infrastructure.Security;
using CarShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;


namespace CarShop.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            try
            {
                TokenResult res = await _authService.RegisterAsync(registerRequest);
                return Ok(res);
            }
            catch(ConflictException ex)
            {
                return Problem(statusCode: 409, title: ex.Message);
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var tmp = await _authService.LoginAsync(loginRequest);  
            if ( tmp == null)
            {
                return Problem(statusCode: 401, title: "Email 或密碼錯誤");
            }
            return Ok(tmp);
        }
    }
}
