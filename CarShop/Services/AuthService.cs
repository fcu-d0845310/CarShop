using CarShop.Data;
using CarShop.Data.Constants;
using CarShop.Data.Entities;
using CarShop.Dtos.Auth;
using CarShop.Exceptions;
using CarShop.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CarShop.Services
{
    public class AuthService
    {
        private readonly AppDbContext _appDbContext;
        private readonly JwtHelper _jwtHelper;
        public AuthService(AppDbContext appDbContext, JwtHelper jwtHelper)
        {
            _appDbContext = appDbContext;
            _jwtHelper = jwtHelper;
        }

        public async Task<TokenResult> RegisterAsync(RegisterRequest registerRequest)
        {
            if(await _appDbContext.Users.AnyAsync(u => u.Email == registerRequest.Email))
            {
                throw new ConflictException("Email已被註冊過");
            }
            string passWordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password, 12);

            User user = new User()
            {
                Name = registerRequest.Name,
                RoleId = RoleIds.Customer,
                AuthProvider = AuthProvider.Local,
                Email = registerRequest.Email,
                Password = passWordHash,
                Address = registerRequest.Address,
                Phone = registerRequest.Phone,
                IsProfileCompleted = true,
                PrivacyAgreedAt = DateTime.UtcNow
                //Todo:身分證加密
            };

             _appDbContext.Users.Add(user);
            await _appDbContext.SaveChangesAsync();

            return _jwtHelper.GenerateToken(user.Id, user.RoleId, user.IsProfileCompleted);
        }

        
        public async Task<TokenResult?> LoginAsync(LoginRequest loginRequest)
        {
            User? user = await _appDbContext.Users.Where(u => u.Email == loginRequest.Email && u.AuthProvider == AuthProvider.Local).FirstOrDefaultAsync();

            if (user == null || user.Password is null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
            {
                return null;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _appDbContext.SaveChangesAsync();

            return _jwtHelper.GenerateToken(user.Id, user.RoleId, user.IsProfileCompleted);
        }
    }
}
