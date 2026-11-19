using CarShop.Data;
using CarShop.Infrastructure.Security;
using CarShop.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---------- 服務註冊（DI） ----------

builder.Services.AddControllers();

// 錯誤回應統一為 RFC 9457 Problem Details
builder.Services.AddProblemDetails();

// 產生 OpenAPI 文件（/openapi/v1.json）
builder.Services.AddOpenApi();

// DbContext：連線字串在建立 DbContext 時才讀取，整合測試的覆寫才會生效
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("缺少連線字串 ConnectionStrings:Default")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// JwtBearer 的驗證參數從設定檔的 "Jwt" 區段讀取（Key 放在 User Secrets）
// 在第一次驗證 token 時才讀取，整合測試覆寫的 Jwt:Key 才會生效
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("缺少設定 Jwt:Key");

        options.MapInboundClaims = false; // 保留 sub、role 等原始 claim 名稱
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        };
    });

builder.Services.AddAuthorization();

// JwtHelper 沒有保存每次請求不同的狀態，整個應用程式共用一個即可
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddSingleton<IDHasher>();
builder.Services.AddSingleton<IDEncryptor>();
//註冊service
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PostService>();
//RateLimit
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    //"login"：每個 IP 每分鐘 10 次
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",   // 依 IP 分開計算
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    // "register"，每分鐘 5 次
    options.AddPolicy("register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", 
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
    // 超過時加上 Retry-After 標頭（告訴前端幾秒後可以再試）
    options.OnRejected = (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }
        return ValueTask.CompletedTask;
    };
});
var app = builder.Build();

// ----------中介軟體----------

// 未處理的例外 → 500 Problem Details（開發環境由框架自動使用開發者錯誤頁）
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

// 沒有內容的錯誤狀態碼（404、401…）→ Problem Details
app.UseStatusCodePages();

app.UseHttpsRedirection();

// 提供 wwwroot 的檔案，例如 /images/seed/car-1-1.png
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "CarShop API v1"));
}

app.MapControllers();

app.Run();
