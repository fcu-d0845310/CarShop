using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CarShop.Tests.Integration;

/// <summary>
/// 啟動整個 API 供整合測試使用，並在測試開始前以 database/schema.sql 重建測試資料庫的資料表。
/// 連線字串來自 API 專案的 User Secrets：ConnectionStrings:Test。
/// </summary>
public sealed partial class CarShopApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 測試專用金鑰，與開發環境的 Jwt:Key 無關
    private const string TestJwtKey = "integration-tests-only-signing-key-0123456789";

    private readonly string _testConnectionString = LoadTestConnectionString();

    public async ValueTask InitializeAsync()
    {
        await EnsureDatabaseExistsAsync();
        await RunSchemaScriptAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 最後加入的設定來源優先權最高，覆寫 User Secrets 中的開發用設定
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _testConnectionString,
                ["Jwt:Key"] = TestJwtKey,
            }));
    }

    private static string LoadTestConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Test");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "缺少 ConnectionStrings:Test，請以 dotnet user-secrets 在 CarShop 專案設定。");
        }

        // 保護：schema.sql 會刪除所有資料表，只允許對名稱以 _Test 結尾的資料庫執行
        var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (!database.EndsWith("_Test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"測試資料庫名稱必須以 _Test 結尾，目前為「{database}」。");
        }

        return connectionString;
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        var builder = new SqlConnectionStringBuilder(_testConnectionString);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        // 資料庫名稱以參數傳入，由 QUOTENAME 在 SQL Server 端處理，不在 C# 拼接 SQL
        const string sql = """
            IF DB_ID(@name) IS NULL
            BEGIN
                DECLARE @create NVARCHAR(300) = N'CREATE DATABASE ' + QUOTENAME(@name);
                EXEC (@create);
            END
            """;

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@name", System.Data.SqlDbType.NVarChar, 128).Value = database;
        await command.ExecuteNonQueryAsync();
    }

    private async Task RunSchemaScriptAsync()
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Database", "schema.sql");
        var script = await File.ReadAllTextAsync(scriptPath);

        await using var connection = new SqlConnection(_testConnectionString);
        await connection.OpenAsync();

        // GO 是 SSMS 的批次分隔符號，不是 T-SQL，必須自行切開逐批執行
        foreach (var batch in GoSeparator().Split(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}

/// <summary>
/// 同一個 collection 的測試共用一個 CarShopApiFactory，整個測試執行只重建一次資料表。
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<CarShopApiFactory>
{
    public const string Name = "Api";
}
