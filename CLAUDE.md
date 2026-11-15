# CarShop 專案說明（給 agent）

二手車電商前台：ASP.NET Core Web API（.NET 10）＋ EF Core（DB First）＋ SQL Server。前端（Vue）在後端完成後才建立。

**開始任何工作前先讀：**
- `docs/REQUIREMENTS.md`：需求、狀態定義、資料表定義、API 清單
- `docs/ARCHITECTURE.md`：架構決策、分層、認證、資料存取原則、關鍵流程

文件沒有寫清楚、需要做決定的地方，先列出來問開發者，不要自行決定。

## 常用指令（在 repo 根目錄執行）

```bash
dotnet tool restore          # 還原本機工具（dotnet-ef）
dotnet build                 # 建置，必須 0 錯誤 0 警告
dotnet test                  # 執行全部測試（使用 Microsoft Testing Platform，見 global.json）
dotnet run --project CarShop # 啟動 API；開發環境文件在 /swagger
```

重新產生 Entity 與 `AppDbContext`（在 `CarShop/` 資料夾執行）：

```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:Default" Microsoft.EntityFrameworkCore.SqlServer --output-dir Data/Entities --context-dir Data --context AppDbContext --no-onconfiguring --force
```

## 專案結構與分層

- `Controllers/`：接收請求、回傳結果，**不放商業邏輯**
- `Services/`：商業邏輯，直接使用 `AppDbContext`（不設 Repository）
- `Dtos/`：API 輸入輸出，依功能分子資料夾；Controller 不直接回傳 Entity
- `Data/AppDbContext.cs`、`Data/Entities/`：**scaffold 自動產生，禁止手動修改**；擴充寫在 `Data/Partials/` 的 partial class
- `Data/Constants/`：狀態、分類、角色 ID 的常數類別（見下方）
- `Data/QueryExtensions/`：重複使用的查詢條件（例：`.WhereAvailable()`）
- `Infrastructure/`：`Ecpay/`、`Storage/`（`IFileStorage`）、`Security/`（JWT、身分證加解密）
- `BackgroundJobs/`：背景工作（釋放逾時訂單）
- `CarShop.Tests/Unit/`：純邏輯單元測試；`CarShop.Tests/Integration/`：透過 HTTP 呼叫 API 的整合測試
- `database/schema.sql`、`database/data.sql`：資料庫腳本。資料庫結構定案，不使用 Migration

## 資料存取規則

- 讀取查詢用 `Select` 投影成 DTO，唯讀查詢加 `AsNoTracking()`
- 一個業務動作只呼叫一次 `SaveChangesAsync`（或包在同一個交易內）
- 一律使用 EF Core LINQ 或參數化查詢，**禁止拼接 SQL 字串**

### 常數類別（`Data/Constants/`，由開發者撰寫）

- 狀態、分類、角色 ID 一律使用常數，例如 `CarStatus.Available`、`RoleIds.Customer`，**程式中不直接寫字串或數字**
- 常數的值必須與 `database/schema.sql` 的 CHECK 約束及 `Roles` 固定 ID 一致
- 這些類別由開發者親自撰寫；若需要的常數尚未存在，**先詢問開發者**，不要自行建立

### 唯一索引違規

- 寫入前以 `AnyAsync` 等方式檢查，重複時回傳友善的 409 訊息
- **不攔截**唯一索引違規（SQL Server 錯誤碼 2601／2627）：不寫 `catch (DbUpdateException)`，也不建立 `IsUniqueViolation()` 之類的判斷方法
- 兩個請求同時通過檢查時，由資料庫的唯一索引擋下，該請求回傳 500；這是開發者接受的取捨，資料不會重複
- 此規則不涵蓋 RowVersion 衝突（`DbUpdateConcurrencyException`），其處理方式依 `docs/ARCHITECTURE.md`

## 禁止事項

- **任何前台 API 回應都不得包含 `CostPrice`**（從查詢投影階段就不取出）
- 不拼接 SQL
- 不提交、不寫出任何機密：連線字串、金鑰一律放 User Secrets，`appsettings.json` 不得出現機密；不要讀取或印出 User Secrets
- 不手動修改 scaffold 產生的 `Data/AppDbContext.cs` 與 `Data/Entities/`
- 不修改 `database/` 腳本與資料庫結構，除非開發者明確要求
- 不執行 `git commit`、`git push`，由開發者審查後自行 commit

## 完成前的檢查

- 修改程式碼後，**結束前必須執行 `dotnet test`**
- 回報中必須寫出測試結果：**通過幾個、失敗幾個**；有失敗時附上失敗的測試名稱與原因，不可隱瞞
- `dotnet build` 必須 0 錯誤 0 警告

## 整合測試

- `CarShopApiFactory` 啟動 API，測試開始前在測試資料庫執行 `database/schema.sql`（資料表全部重建，資料庫不存在時自動建立）
- 測試資料庫連線字串：CarShop 專案 User Secrets 的 `ConnectionStrings:Test`，名稱必須以 `_Test` 結尾，否則測試拒絕執行
- 各測試自行放入需要的資料；不使用 EF Core InMemory（不支援 RowVersion）
