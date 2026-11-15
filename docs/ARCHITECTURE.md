# 二手車電商系統（前台）架構文件

> 狀態：**已確認（v1.2）**
> 依據：`docs/REQUIREMENTS.md` v1.0、架構訪談 A1–A4
> 本文件中標示「預設」的項目為業界慣例做法，已於審閱時一併確認。

---

## 1. 架構決策紀錄

| # | 議題 | 決定 | 理由 |
|---|---|---|---|
| A1 | .NET 版本 | **.NET 10 LTS** | 生態系成熟，相依套件確定支援；支援期與 .NET 11 相同（至 2028/11） |
| A2 | 後端分層 | **三層式**：Controller → Service → EF Core，不設 Repository | 程式碼精簡，可直接使用 EF Core 的投影、追蹤、交易功能；`DbContext` 本身已是 Repository＋Unit of Work。曾評估加入依實體設計的 Repository，最後決定不採用 |
| A3 | 認證機制 | **單一 JWT**：有效期 2 小時，以 JSON 回傳、前端存 localStorage；不使用 Cookie 與 Refresh Token | 實作單純、前後端不必同網域；以短效期、CSP 與 XSS 防護降低 token 外洩的影響。曾評估 Refresh Token，因複雜度較高列入之後再做 |
| A4 | 前端 UI | **待後端 API 完成後決定** | 開發順序改為後端先行 |
| A5 | ASP.NET Core Identity | **不使用** | 目標是親手練習第三方登入與帳號綁定，完整 Identity 會把這段流程包起來；也能維持原本的 Users、Roles 資料表設計 |
| A6 | 密碼雜湊 | **BCrypt**（work factor 12） | 使用現成且廣泛驗證的實作，不自行撰寫雜湊演算法；結果 60 字元，符合原資料表設計 |
| A7 | Google 登入 | **授權碼流程＋PKCE**，由後端完成 | 能完整練習 OAuth 2.0／OpenID Connect；日後可用同一套流程加上 LINE 等服務 |
| A8 | 真人驗證 | **reCAPTCHA v2**，用於註冊與登入 | 「我不是機器人」勾選框辨識度高，展示作品時一看就懂 |
| A9 | 帳號安全 | 第一版做**登入相關 API 速率限制**；登入失敗鎖定、忘記密碼、Email 驗證之後再做 | CAPTCHA 加上速率限制已能阻擋大部分自動化攻擊 |
| A10 | 資料庫開發方式 | **DB First**：先在 SQL Server 建立資料表，再以 `dotnet ef dbcontext scaffold` 產生程式碼；結構定案後不再變動，不使用 Migration | 資料表已事先設計完成（完整定義見 REQUIREMENTS.md 第 6 節），開發者熟悉 SSMS；台灣企業常見做法 |
| A11 | 狀態與分類的表示 | **常數類別**（`static class` ＋ `const string`），資料庫以 CHECK 約束限制可用的值 | DB First 產生的欄位為 `string`，常數類別不需改動自動產生的程式碼；打錯值會被資料庫拒絕 |

---

## 2. 技術版本

| 項目 | 版本 | 備註 |
|---|---|---|
| .NET SDK／ASP.NET Core | 10.0.x | LTS |
| EF Core（SQL Server provider） | 10.0.x | 版本需與 .NET 一致 |
| MSSQL | 本機 SQL Server，以 SSMS 管理 | |
| Vue | 3.5.x | 3.6 尚為 RC，不採用 |
| Vite | 8.x | |
| TypeScript | 前端開發時確認 | |

主要後端套件（預設）：

| 用途 | 套件 |
|---|---|
| 資料存取 | `Microsoft.EntityFrameworkCore.SqlServer`、`Microsoft.EntityFrameworkCore.Design` |
| 程式碼產生 | `dotnet-ef` 命令列工具（執行 scaffold 指令需要，版本與 EF Core 一致） |
| JWT 驗證 | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| 密碼雜湊 | `BCrypt.Net-Next` |
| Google ID Token 驗證 | `Microsoft.IdentityModel.Protocols.OpenIdConnect`（取得 Google 的 JWKS 公開金鑰並驗證簽章） |
| 速率限制 | ASP.NET Core 內建 `Microsoft.AspNetCore.RateLimiting` |
| API 文件 | `Microsoft.AspNetCore.OpenApi`（內建 OpenAPI 產生）＋ Scalar 或 Swagger UI 瀏覽 |
| 測試 | xUnit、`Microsoft.AspNetCore.Mvc.Testing` |

> 實際安裝時以當下的最新穩定版為準，並確認支援 .NET 10。

---

## 3. 專案結構

沿用 Visual Studio 建立的結構，方案資料夾即為 repo 根目錄。

```
CarShop/                          repo 根目錄：git init 與執行 claude 的位置
├── CarShop.slnx                  方案檔（.NET 10 的新版格式）
├── .gitignore
├── README.md                     專案介紹
├── CLAUDE.md                     給 agent 的專案說明（第 10 節）
├── docs/
│   ├── REQUIREMENTS.md
│   └── ARCHITECTURE.md
├── database/
│   ├── schema.sql                資料表結構（SSMS 匯出）
│   └── data.sql                  初始資料
├── CarShop/                      Web API 專案
│   ├── Controllers/              接收請求、回傳結果，不放商業邏輯
│   ├── Services/                 商業邏輯，直接使用 AppDbContext
│   ├── Dtos/                     API 輸入輸出格式（依功能分子資料夾）
│   ├── Data/
│   │   ├── AppDbContext.cs       [自動產生] scaffold 產生，不手動修改
│   │   ├── Entities/             [自動產生] scaffold 產生，不手動修改
│   │   ├── Partials/             partial class 擴充（計算屬性等）
│   │   ├── Constants/            狀態、分類、角色 ID 的常數類別
│   │   └── QueryExtensions/      重複使用的查詢條件
│   ├── Infrastructure/
│   │   ├── Ecpay/                綠界串接（CheckMacValue、表單參數）
│   │   ├── Storage/              檔案儲存介面與本機實作
│   │   └── Security/             JWT 簽發、身分證加解密
│   ├── BackgroundJobs/           背景工作（釋放逾時訂單）
│   └── Program.cs
├── CarShop.Tests/                測試專案（階段 0 建立）
│   ├── Unit/                     工具類別的單元測試
│   └── Integration/              透過 HTTP 呼叫 API 的整合測試
└── frontend/                     Vue 前端（後端完成後建立）
```

原則：只屬於某個程式專案的內容放在該專案資料夾內；文件、SQL 腳本、CLAUDE.md 等整個方案共用的內容放在根目錄。

---

## 4. API 設計慣例（預設）

### 路由與命名

- 所有端點以 `/api` 開頭，資源名稱用複數小寫，多字以 `-` 連接（例：`/api/trade-ins`）
- 「我的」資料統一放在 `/api/me/...`，後端從 JWT 取得使用者 ID，**不接受從參數傳入使用者 ID**

### DTO 原則

- Controller 不直接回傳 Entity，一律轉成 DTO
- 前台的任何 DTO 都**不包含 `CostPrice`**；以整合測試檢查回應中不出現此欄位
- 狀態欄位以字串輸出（例：`"Pending"`），不輸出數字

### 分頁回應格式

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 135
}
```

`pageSize` 上限 50，超過以 50 計算。

### 錯誤回應格式

採用 RFC 9457 **Problem Details** 標準格式（ASP.NET Core 內建支援）：

```json
{
  "type": "https://example.com/errors/car-not-available",
  "title": "此車輛目前無法下訂",
  "status": 409,
  "detail": "車輛正在付款中，請稍後再試",
  "errors": { "OfferPrice": ["金額必須低於售價"] }
}
```

| 狀態碼 | 使用情境 |
|---|---|
| 400 | 輸入驗證失敗 |
| 401 | 未登入或 token 失效 |
| 403 | 已登入但無權限（例：存取別人的訂單） |
| 404 | 資源不存在 |
| 409 | 狀態衝突（例：車輛已被鎖定、重複收藏、已有審核中的議價） |

**唯一索引違規的處理**：寫入前以程式檢查（例如 `AnyAsync`），重複時回傳 409 與友善訊息。`SaveChangesAsync` **不攔截**唯一索引違規（SQL Server 錯誤碼 2601／2627）：兩個請求同時通過檢查時，由資料庫的唯一索引擋下其中一個，該請求回傳 500，資料不會重複。RowVersion 衝突（`DbUpdateConcurrencyException`）不在此限，處理方式見 7.1 節的下訂流程。

### 時間

- 資料庫一律存 **UTC**，API 輸出 ISO 8601 格式（含時區），由前端轉成台灣時間顯示

---

## 5. 認證設計（A3）

### Token 規格

| 項目 | 內容 |
|---|---|
| 格式 | JWT（HMAC-SHA256 簽章） |
| 內容（claims） | 使用者 ID、角色、`IsProfileCompleted`、到期時間 |
| 有效期 | **2 小時**，過期後需重新登入 |
| 傳遞方式 | 登入回應的 JSON；呼叫 API 時放在 `Authorization: Bearer` 標頭 |
| 前端存放 | localStorage |
| 後端存放 | 不存（無狀態） |

不使用 Cookie，也不使用 Refresh Token（第 12 節第 10 項）。

登入成功的回應格式：

```json
{
  "accessToken": "eyJhbGciOi...",
  "expiresAt": "2026-11-12T10:00:00Z"
}
```

### 流程

```
登入／註冊成功
  → 回傳 JWT，前端存入 localStorage

呼叫 API
  → Header: Authorization: Bearer <JWT>

JWT 過期（API 回傳 401）
  → 前端清除 token，導向登入頁

登出
  → 前端刪除 localStorage 中的 token（後端沒有對應的 API）
```

### 已知限制與對策

| 限制 | 說明 | 對策 |
|---|---|---|
| **無法從後端撤銷** | 登出只是前端刪除 token；若 token 已被竊取，在到期前仍然有效 | 有效期縮短為 2 小時，限制被竊取後可利用的時間 |
| **XSS 可讀取 token** | token 存在 localStorage，JavaScript 讀得到 | 前端不使用 `v-html` 顯示使用者輸入；設定 CSP 標頭（只允許自己網域的腳本） |
| **使用者每 2 小時需重新登入** | 沒有自動換發機制 | 第一版接受此限制；Refresh Token 列入「之後再做」 |

面試時可以說明：選擇單一 JWT 是為了降低複雜度，並清楚知道它無法撤銷的代價，以及日後加入 Refresh Token 的升級方向。

### Google 登入：授權碼流程＋PKCE（A7）

後端自行完成 OAuth 2.0／OpenID Connect 流程，不使用 Identity 或現成的外部登入中介軟體，以便練習串接第三方登入。同一套流程日後可套用到 LINE 等其他服務。

```
1. 前端：GET /api/auth/google/authorize-url
   後端：產生 state、nonce、PKCE code_verifier（各為隨機值）
         以 state 為鍵，將 nonce 與 code_verifier 存入伺服器端快取（10 分鐘、只能用一次）
         回傳 Google 授權網址，包含：
           client_id、redirect_uri、response_type=code、scope=openid email profile、
           state、nonce、code_challenge = BASE64URL(SHA256(code_verifier))、code_challenge_method=S256

2. 瀏覽器導向 Google，使用者登入並同意授權

3. Google 導回前端頁面：{redirect_uri}?code=...&state=...

4. 前端：POST /api/auth/google/callback { code, state }
   後端：
     a. 以 state 取出快取的 nonce 與 code_verifier，取出後立即刪除；找不到就拒絕（防 CSRF 與重放）
     b. 向 Google token 端點交換 token：code、client_id、client_secret、redirect_uri、code_verifier
     c. 驗證 ID Token：以 Google 公開金鑰（JWKS）驗證簽章；檢查 iss、aud（我們的 Client ID）、exp、
        nonce 是否與快取中相同、email_verified 為 true
     d. 以 ProviderId（ID Token 的 sub）查找會員
          找到 → 登入
          找不到，且 Email 已被密碼帳號使用 → 拒絕（見第 12 節第 7 項）
          找不到 → 建立會員（AuthProvider = Google、IsProfileCompleted = false）
     e. 發行我們自己的 JWT
```

名詞說明：

| 名詞 | 作用 |
|---|---|
| `state` | 隨機值，導回時必須相同，防止攻擊者把自己的授權碼塞給受害者（CSRF） |
| `code_verifier`／`code_challenge`（PKCE） | 發起時只送出雜湊值，換 token 時才出示原值，證明換 token 的人就是發起登入的人 |
| `nonce` | 寫進 ID Token 中，防止舊的 ID Token 被重複使用 |

實作備註：
- state 快取使用 `IMemoryCache`，適用單一伺服器。若日後部署多台，需改用分散式快取（如 Redis）
- `redirect_uri` 指向**前端**頁面，前端再把 code 交給後端。這樣整個流程都不需要 Cookie，與 A3 一致
- 後端尚未有前端頁面時，以手動方式測試：瀏覽器開啟授權網址，登入後從網址列複製 code 與 state，再用 API 測試工具呼叫 callback
- 整合測試以假的 HTTP 處理器模擬 Google 的 token 端點與 JWKS，不實際連線 Google

`IsProfileCompleted = false` 的會員，除了 `/api/auth/*`、`/api/me`、`/api/auth/complete-profile` 以外的會員功能一律回傳 403。

### 密碼雜湊（A6）

- 使用 `BCrypt.Net-Next`，**work factor 12**（每次雜湊約需數百毫秒，大幅提高暴力破解成本）
- 雜湊結果固定 60 字元，維持 `Users.Password NVARCHAR(60)`
- 密碼規則：至少 8 個字元，需包含英文字母與數字
- 注意：BCrypt 只會使用密碼的前 72 個位元組，因此密碼長度上限設為 64 個字元

### 真人驗證：reCAPTCHA v2（A8）

```
前端顯示「我不是機器人」勾選框 → 使用者通過後取得 token
  → 隨註冊或登入資料一起送出（欄位 captchaToken）
  → 後端呼叫 https://www.google.com/recaptcha/api/siteverify（secret、response）
  → success 為 true 且 hostname 符合才繼續處理，否則回傳 400
```

- 套用在 `POST /api/auth/register` 與 `POST /api/auth/login`
- **一定要在後端驗證**，只在前端顯示勾選框無法阻擋直接呼叫 API 的機器人
- 開發與整合測試時，以設定切換為假的驗證實作（`ICaptchaVerifier` 介面），避免測試依賴外部服務

### 速率限制（A9）

使用 ASP.NET Core 內建的 Rate Limiter，依來源 IP 計算：

| 端點 | 限制 | 超過時 |
|---|---|---|
| `POST /api/auth/login` | 每分鐘 10 次 | 回傳 429，附 `Retry-After` 標頭 |
| `POST /api/auth/register` | 每分鐘 5 次 | 同上 |
| `POST /api/auth/google/callback` | 每分鐘 10 次 | 同上 |

若日後部署在反向代理之後，需設定信任的代理，才能從 `X-Forwarded-For` 取得真實 IP。

---

## 6. 資料存取原則（A2）

Service 直接使用 `AppDbContext`，不另設 Repository 層。

### 原則

- **讀取查詢用 `Select` 投影**：只取出 DTO 需要的欄位，`CostPrice` 從查詢階段就不會被取出，而不是取出後才在 Controller 過濾
- **唯讀查詢加上 `AsNoTracking()`**：不需要更新的資料不讓 EF Core 追蹤，節省記憶體
- **一個業務動作只存一次檔**：例如「建立訂單＋鎖定車輛」在同一個 `SaveChangesAsync` 或同一個交易內完成，避免出現訂單建好、車輛卻沒鎖到的情況
- **重複使用的查詢條件寫成擴充方法**：例如 `.WhereAvailable()`，避免同一段條件散落在多個 Service

### DB First 開發方式（A10）

資料表先在 SQL Server 建立，再以 EF Core 反向產生 Entity 與 `AppDbContext`。結構定案後不再變動，不使用 EF Core Migration，也不做結構版本管理。

**資料庫腳本**（放在 repo 根目錄的 `database/`）：

| 檔案 | 內容 | 用途 |
|---|---|---|
| `schema.sql` | 所有資料表、索引、外鍵、約束 | 由 SSMS「產生指令碼」匯出一次；本機建庫與整合測試建立測試資料庫都使用它 |
| `data.sql` | 初始資料：Customer／Boss 角色、2～3 間分店、10～20 台車與圖片、1～2 個測試會員 | 本機開發與作品展示；沒有後台時，前台需要它才有車輛可瀏覽 |

`schema.sql` 依 REQUIREMENTS.md 第 6 節的完整定義建立，特別注意以下設計：`Cars.RowVersion`（`ROWVERSION`）、`Users.Email` 唯一索引、`Users.IdentityNumberHash` 篩選唯一索引（`WHERE IdentityNumberHash IS NOT NULL`）、`Orders.OrderNo` 唯一索引、`Users.(AuthProvider, ProviderId)` 篩選唯一索引、狀態與分類欄位的 CHECK 約束、`CreatedAt` 預設值 `SYSUTCDATETIME()`、`Roles` 固定 ID（不使用 IDENTITY）、`TradeInImages` 資料表、`CarPostComments.ParentId` 自我參照外鍵。

**產生程式碼的指令**（在 API 專案資料夾執行）：

```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:Default" Microsoft.EntityFrameworkCore.SqlServer --output-dir Data/Entities --context-dir Data --context AppDbContext --no-onconfiguring --force
```

| 參數 | 為什麼 |
|---|---|
| `Name=ConnectionStrings:Default` | 從 User Secrets 讀取連線字串，**不把連線字串寫在指令或程式碼中** |
| `--no-onconfiguring` | 不產生 `OnConfiguring`。否則產生器會把連線字串（含密碼）**寫死在 `AppDbContext` 裡** |
| `--force` | 重新產生時覆蓋舊檔 |

**自動產生的檔案不手動修改**：`Data/Entities/` 與 `Data/AppDbContext.cs` 每次重新產生都會被覆蓋。需要擴充時，寫在 `Data/Partials/` 的 partial class 中。

**狀態、分類與角色以常數類別表示**（A11），放在 `Data/Constants/`：

```csharp
public static class CarStatus
{
    public const string Available = "Available";
    public const string PendingPayment = "PendingPayment";
    public const string Reserved = "Reserved";
    public const string Sold = "Sold";
}

public static class RoleIds
{
    public const int Boss = 1;
    public const int Customer = 2;
}

// 使用時
if (car.Status != CarStatus.Available) { ... }
```

程式中**不直接寫字串**（例如 `"Available"`），一律使用常數，避免拼錯。常數的值必須與 `schema.sql` 的 CHECK 約束一致。

### 範例

```csharp
public static class CarQueryExtensions
{
    public static IQueryable<Car> WhereAvailable(this IQueryable<Car> query) =>
        query.Where(c => c.Status == CarStatus.Available);
}

// Service 中
var cars = await _db.Cars
    .AsNoTracking()
    .WhereAvailable()
    .Select(c => new CarListItemDto(c.Id, c.Brand, c.Model, c.SellingPrice))  // 不含 CostPrice
    .ToListAsync(ct);
```

### 對測試的影響

Service 依賴 `AppDbContext`，不容易用假物件替換，因此：
- **純邏輯**（訂金計算、身分證驗證、CheckMacValue 等）抽成獨立類別，以單元測試驗證
- **Service 的行為**以連到真實 SQL Server 的整合測試驗證

---

## 7. 關鍵流程設計

### 7.1 下訂與暫鎖（Q8）

```
POST /api/orders { carId }
  1. 查詢車輛；Status 必須是 Available，否則 409
  2. Status 改為 PendingPayment
  3. 建立訂單：DepositAmount = SellingPrice × 5%（四捨五入至整數元）
              ExpiresAt = 現在 + 15 分鐘
  4. SaveChanges
       → EF Core 以 RowVersion 檢查：若期間有人先改了這台車，
         拋出 DbUpdateConcurrencyException → 回傳 409「車輛已被其他人下訂」
  5. 回傳綠界付款表單所需的參數（含 CheckMacValue）
```

步驟 2～4 在**同一個交易**內完成。

### 7.2 綠界付款回呼

```
POST /api/payments/ecpay/notify（綠界伺服器呼叫，不經過使用者瀏覽器）
  1. 驗證 CheckMacValue，失敗則拒絕並記錄
  2. 以 MerchantTradeNo 查訂單
  3. 若訂單已是 Paid → 直接回應 "1|OK"（冪等：同一筆通知重複送達不重複處理）
  4. RtnCode = 1（成功）→ 訂單 Paid、車輛 Reserved
     其他 → 訂單 Failed、車輛 Available
  5. 回應 "1|OK"（綠界要求的格式，否則會重送）
```

- 綠界金鑰（HashKey、HashIV、MerchantID）放在 User Secrets
- CheckMacValue 演算法依綠界官方文件自行實作，並以官方文件中的範例資料寫單元測試驗證
- **逾時後才收到付款成功通知**：訂單已 Expired 但綠界回報付款成功時，若車輛仍為 Available 則恢復保留，否則標記為需人工退款並記錄（第一版只記錄，處理留給後台）

**本機開發**：綠界無法呼叫 `localhost`，需以 ngrok 建立對外網址，並設定為 ReturnURL。

### 7.3 逾時釋放（背景工作）

- 以 ASP.NET Core 的 `BackgroundService` 每分鐘執行一次
- 找出 `PaymentStatus = Pending` 且 `ExpiresAt < 現在` 的訂單 → 訂單改 Expired、車輛改 Available
- 同樣受 RowVersion 保護，避免與付款回呼同時修改時互相覆蓋

### 7.4 身分證加密（Q4）

- 使用 ASP.NET Core **Data Protection** 加密後存入 `IdentityNumber`
- **加密金鑰必須持久化保存**（存到指定資料夾或資料庫）。預設的金鑰可能在環境變更後遺失，**金鑰遺失＝所有身分證資料無法解密**
- **唯一性檢查**：Data Protection 每次加密結果都不同，無法用密文比對是否重複。因此另存 `IdentityNumberHash` = HMAC-SHA256（身分證, 秘密金鑰），並建立唯一索引
  - 使用 HMAC 而非一般 SHA-256：身分證組合有限，一般雜湊可被窮舉反查；沒有秘密金鑰就無法計算 HMAC
  - 欄位允許 NULL（Google 會員補填前），使用篩選索引 `WHERE IdentityNumberHash IS NOT NULL`
  - 重複時的錯誤訊息不透露是哪個帳號使用
- API 只輸出遮罩格式，例如 `A12****789`
- 驗證：英文字母＋9 位數字，並檢查台灣身分證的檢查碼

### 7.5 檔案上傳（Q7）

```csharp
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct);  // 回傳可存取的 URL
    Task DeleteAsync(string url, CancellationToken ct);
}
```

- 第一版實作 `LocalFileStorage`，存到 `wwwroot/uploads/{yyyy}/{MM}/{隨機GUID}.{副檔名}`
- 種子資料的車輛圖片放在 `wwwroot/images/seed/`，與使用者上傳的 `uploads/` 分開；`data.sql` 存相對路徑
- `wwwroot/uploads/` 加入 `.gitignore`（使用者上傳的檔案不進 git）；`wwwroot/images/seed/` 要進 git，clone 的人才有圖片
- 限制：副檔名 jpg／png／webp；單檔 5 MB；每筆賣車申請最多 10 張
- 讀取檔案開頭的位元組（Magic Number）確認真的是圖片，而不是只看副檔名
- 日後換成雲端儲存時，只要新增一個實作類別

---

## 8. 設定與機密管理（預設）

| 設定 | 存放位置 |
|---|---|
| 資料庫連線字串 | User Secrets |
| JWT 簽章金鑰 | User Secrets |
| 身分證 HMAC 金鑰 | User Secrets |
| Google Client Secret | User Secrets |
| reCAPTCHA Secret Key | User Secrets |
| 綠界 HashKey、HashIV、MerchantID | User Secrets |
| Google Client ID、reCAPTCHA Site Key | `appsettings.json`（非機密） |
| 訂金比例、暫鎖分鐘數、上傳限制 | `appsettings.json` |

`appsettings.json` 與 git 中**不得出現任何機密**。

---

## 9. 測試策略（預設）

| 類型 | 對象 | 做法 |
|---|---|---|
| 單元測試（工具類別） | 身分證驗證、訂金計算、CheckMacValue、遮罩、Token 產生、PKCE 產生、身分證 HMAC | 純邏輯，不碰資料庫 |
| 整合測試 | 每支 API 的主要情境與錯誤情境（Service 的商業規則主要在此驗證） | `WebApplicationFactory` 啟動 API，連到本機 SQL Server 上**專用的測試資料庫**；測試開始前以 `database/schema.sql` 重建資料表，各測試自行放入需要的資料 |
| 併發測試 | 兩個請求同時下訂同一台車 | 驗證只有一筆成功、另一筆收到 409 |
| 安全測試 | 前台回應不含 `CostPrice`；存取他人資料回傳 403 | 整合測試 |

> 整合測試不使用 EF Core InMemory：它不支援 RowVersion 的行為，測不出併發問題。

---

## 10. Agent 開發環境設定（套用課程第 6～10 課）

| 機制 | 設定內容 |
|---|---|
| `CLAUDE.md` | 指令（build、test、scaffold）、分層規則、禁止事項（不回傳 CostPrice、不拼接 SQL、不提交機密、**不手動修改 scaffold 產生的檔案**）、指向 `docs/` 的兩份文件 |
| 權限 deny 規則 | 禁止讀取 User Secrets 檔案與 `.env` 類檔案；禁止 `git push` |
| Hook | **不使用**。改在 `CLAUDE.md` 規定：修改程式碼後，結束前必須執行 `dotnet test`，並在回報中寫出測試結果。由開發者確認回報內容 |
| Skill | `add-endpoint`：新增 API 的標準流程（DTO → Service → Controller → 整合測試 → 更新文件） |
| Subagent | `security-reviewer`：每個里程碑完成後進行安全審查 |

---

## 11. 開發順序

後端先行（A4）。每個步驟完成時，測試全部通過才進入下一步。

| 階段 | 內容 |
|---|---|
| 0. 骨架 | 撰寫 `database/schema.sql` 與 `data.sql` 並在本機建庫；建立方案與專案；scaffold 產生 Entity 與 `AppDbContext`；Problem Details、OpenAPI、CLAUDE.md 與權限設定 |
| 1. 里程碑 1 API | 認證（註冊、登入、Google、補填資料）、個人資料、車輛、品牌、分店、收藏 |
| 2. 里程碑 2 API | 預約、議價、賣車申請（含上傳） |
| 3. 里程碑 3 API | 訂單、綠界串接、背景工作、併發測試 |
| 4. 里程碑 4 API | 論壇 |
| 5. 安全審查 | 以 security-reviewer 子代理全面審查 |
| 6. 前端 | 決定 UI 做法（A4）後開始 |

---

## 12. 細節確認紀錄

| # | 問題 | 決定 |
|---|---|---|
| 1 | 開發順序：全部後端完成後才做前端，還是里程碑 1 後端完成就決定 UI？ | 全部後端完成後才做前端 |
| 2 | ~~Access Token 15 分鐘、Refresh Token 7 天~~ | 已由第 10 項取代 |
| 3 | 訂金四捨五入至整數元（綠界金額必須為整數） | 確認採用 |
| 4 | 圖片上傳限制：jpg／png／webp、5 MB、每筆最多 10 張 | 確認採用 |
| 5 | 整合測試使用本機 SQL Server 上的專用測試資料庫 | 確認採用 |
| 6 | 身分證是否需要唯一 | 需要唯一（以 HMAC 雜湊建立唯一索引，見 7.4 節） |
| 7 | Google 登入的 Email 已被密碼帳號註冊時怎麼辦？自動合併有帳號被盜用的風險 | 拒絕登入，提示改用密碼登入；帳號綁定功能之後再做 |
| 8 | 速率限制次數：登入 10 次／分、註冊 5 次／分（每個 IP） | 確認採用 |
| 9 | 密碼規則：8～64 字元，需含英文字母與數字 | 確認採用 |
| 10 | 是否使用 Refresh Token | 不使用，改為單一 JWT，有效期 2 小時（A3） |
| 11 | 狀態與分類的表示 | 常數類別（A11） |
| 12 | 是否使用 Claude Code hook | 不使用；測試由 CLAUDE.md 規則要求 agent 執行並回報，開發者自行確認 |
| 13 | 同時送出造成的唯一索引違規如何回應 | 只以 `AnyAsync` 事先檢查，不攔截 2601／2627，同時送出的那一筆回傳 500；不建立 `IsUniqueViolation()`（見第 4 節） |
