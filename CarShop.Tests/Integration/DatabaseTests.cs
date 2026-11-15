using CarShop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarShop.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class DatabaseTests(CarShopApiFactory factory)
{
    [Fact]
    public async Task AppDbContext_ConnectsToTestDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Database.CanConnectAsync(TestContext.Current.CancellationToken));

        // 確認 API 實際使用的是測試資料庫，而不是開發資料庫
        Assert.EndsWith("_Test", db.Database.GetDbConnection().Database, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SchemaScript_CreatedTables()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 資料表不存在時查詢會丟出例外
        var exception = await Record.ExceptionAsync(() =>
            db.Cars.AnyAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }
}
