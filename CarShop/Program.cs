using CarShop.Data;
using CarShop.Infrastructure.Security;
using CarShop.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
//註冊service
builder.Services.AddScoped<AuthService>();
var app = builder.Build();

// ---------- 中介軟體管線（順序有意義） ----------

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "CarShop API v1"));
}

app.MapControllers();

app.Run();
