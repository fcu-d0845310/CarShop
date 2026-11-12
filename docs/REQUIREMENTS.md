# 二手車電商系統（前台）需求文件

> 狀態：**已確認（v1.0）**
> 來源：需求訪談 Q1–Q9、原始資料庫設計（手繪表格版）、功能清單、待確認事項回覆
> 第 6 節為完整的資料表定義，建立資料庫時以本文件為準
> 標記說明：[新增]　[修改]　[移除]

---

## 1. 專案概述

| 項目 | 內容 |
|---|---|
| 目的 | 履歷作品集，展示全端開發能力 |
| 範圍 | **只做前台（客戶端）**。後台（老闆／管理者）不在本專案範圍 |
| 後端 | ASP.NET Core Web API + EF Core（版本於架構規劃時確定） |
| 前端 | Vue 3 + Vite + TypeScript |
| 資料庫 | MSSQL（本機 SSMS） |
| 金流 | 綠界 ECPay **測試環境** |
| 第三方登入 | Google |

---

## 2. 決策紀錄

| # | 議題 | 決定 | 影響 |
|---|---|---|---|
| Q1 | 沒有後台，申請類功能的狀態誰來改？ | 前台只負責**送出申請＋顯示狀態**，第一版狀態停在「審核中」 | 預約、議價、賣車沒有「同意／拒絕」流程 |
| Q2 | 線上付訂 | 串接**綠界 ECPay 測試環境** | 本機開發需要 ngrok 之類的工具接收付款回呼 |
| Q3 | 登入方式 | **Email＋密碼**、**Google** | Google 會員沒有密碼 → `Password` 可為 NULL |
| Q4 | 身分證、地址 | **註冊時必填** | 需加密儲存、遮罩顯示、驗證格式、同意條款；Google 會員首次登入需補填資料 |
| Q5 | 第一版範圍 | **全部功能**都做 | 分里程碑開發，每個里程碑都是可展示的版本 |
| Q6 | 資料表版本 | **手繪版為準**；訊息類欄位之後再做 | 預約、議價、賣車在第一版**不含留言** |
| Q7 | 圖片儲存 | **後端本機資料夾** | 透過介面隔離，日後可換雲端儲存 |
| Q8 | 同時付訂同一台車 | **下單時暫鎖 15 分鐘** | `Cars` 新增 `RowVersion`；車輛新增「付款中」狀態 |
| Q9 | 訂金金額 | **售價的百分比** | 金額一律由後端計算 |

---

## 3. 角色與權限

| 功能 | 訪客 | 會員（Customer） |
|---|:-:|:-:|
| 瀏覽車輛、分店、論壇 | 可 | 可 |
| 註冊、登入 | 可 | — |
| 管理個人資料 | — | 可 |
| 收藏、預約賞車、議價、付訂、賣車申請 | — | 可 |
| 論壇發文、留言 | — | 可 |

- `Roles` 資料表預設兩筆資料：`Customer`、`Boss`。前台只會用到 `Customer`，`Boss` 保留給日後的後台
- **訪客可以瀏覽車輛與論壇**，需要登入的只有寫入類操作

---

## 4. 功能清單

### 第一版

分成四個里程碑，**每完成一個，都是一個可以展示的版本**。

#### 里程碑 1：會員＋車輛瀏覽＋收藏

| 功能 | 說明 | 驗收重點 |
|---|---|---|
| Email 註冊 | 填寫 Email、密碼、姓名、手機、身分證、地址，勾選個資同意條款，並通過 reCAPTCHA | Email 不可重複；**身分證不可重複**；身分證檢查碼驗證；密碼以 BCrypt 雜湊；速率限制 |
| Email 登入／登出 | 登入需通過 reCAPTCHA；登出由前端刪除 token | 登入失敗訊息不透露「帳號存在與否」；速率限制 |
| Google 登入 | 以授權碼流程＋PKCE 串接；首次登入建立會員並導向**補填資料頁** | 未補填完成前不能使用會員功能；補填的身分證同樣不可重複 |
| 個人資料 | 查看與修改姓名、手機、地址 | 身分證只顯示遮罩後的格式（例：`A12****789`） |
| 車輛列表 | 依**分店、品牌、價格區間、燃料、變速箱、座位數**篩選；分頁；排序 | 只顯示可販售的車輛狀態 |
| 車輛詳情 | 規格、車況介紹、多張圖片（主圖優先） | **絕對不回傳 `CostPrice`** |
| 分店資訊 | 列出所有分店 | |
| 收藏與追蹤 | 加入／移除收藏；會員中心查看收藏清單與車輛目前狀態 | 同一台車不能重複收藏 |

#### 里程碑 2：申請類功能

第一版只負責**送出申請**與**查看狀態**，狀態停在「審核中」。

| 功能 | 說明 | 驗收重點 |
|---|---|---|
| 預約賞車 | 選擇車輛與期望時間送出，可選任意未來時間（不採固定時段） | 時間必須是未來時間；同一會員對同一台車只能有一筆審核中的預約 |
| 線上議價 | 對車輛提出期望價格 | 金額必須大於 0 且低於售價；同一會員對同一台車只能有一筆審核中的出價 |
| 線上估價／我要賣車 | 填寫自有車輛的品牌、型號、年份、里程、期望價格，並上傳圖片 | 圖片上傳安全規則見第 7 節 |
| 會員中心 | 查看自己的預約、議價、賣車申請與狀態 | 只能看到自己的資料 |

#### 里程碑 3：線上付訂

| 功能 | 說明 | 驗收重點 |
|---|---|---|
| 接受原價付訂 | 會員按下付訂 → 建立訂單並暫鎖車輛 15 分鐘 → 跳轉綠界付款 | 同一台車同時只能有一筆付款中的訂單（樂觀並行控制） |
| 付款結果處理 | 綠界回呼後更新訂單；付款成功則車輛改為「已保留」 | **驗證綠界的 CheckMacValue**；同一筆回呼重複送達時不能重複處理 |
| 逾時釋放 | 超過 15 分鐘未付款，訂單逾期、車輛恢復上架 | |
| 我的訂單 | 會員中心查看訂單與付款狀態 | |

- 訂金 = 售價 × 訂金比例，比例為 **5%**，寫在設定檔中
- 金額**一律由後端計算**，不接受前端傳入的金額

#### 里程碑 4：車主論壇

| 功能 | 說明 | 驗收重點 |
|---|---|---|
| 文章列表 | 依車輛、分類篩選，分頁 | |
| 文章詳情 | 文章內容＋巢狀留言 | |
| 發文 | 選擇分類並發文，**可選擇**關聯某台車輛 | 內容防 XSS；分類必須是第 5 節定義的值 |
| 留言、回覆 | 回覆文章或回覆其他留言（巢狀） | 內容防 XSS；巢狀最多 **2 層**，回覆的回覆不再縮排 |

### 之後再做

| 功能 | 原因 |
|---|---|
| 後台（老闆／管理者）全部功能 | 不在本專案範圍 |
| 預約、議價、賣車的**留言與老闆回覆** | Q6 決定，第一版只做送出申請 |
| 議價成功後付訂 | 需要後台審核議價 |
| 退款、尾款、簽約流程 | 需要後台 |
| LINE 等其他第三方登入 | Q3 決定；Google 的串接流程可直接沿用 |
| 登入失敗鎖定、忘記密碼、Email 驗證 | 第一版以 reCAPTCHA 與速率限制保護登入 |
| Google 帳號與既有密碼帳號綁定 | 第一版遇到相同 Email 時拒絕 Google 登入 |
| 論壇的編輯／刪除（含刪除自己的文章）、官方回覆、檢舉 | 第一版只做發文、留言與回覆 |
| 圖片改存雲端（Azure Blob／S3） | Q7 決定，已預留介面 |
| 會員大頭貼 | 第 9 節第 16 項 |
| Refresh Token（可撤銷的登入、真正的登出） | 第 9 節第 15 項，第一版使用單一 JWT |

---

## 5. 狀態定義

狀態與分類欄位以 `NVARCHAR(20)` 儲存，資料庫以 CHECK 約束限制可用的值，程式中以常數類別對應（見 ARCHITECTURE.md 第 6 節）。

| 對象 | 狀態流轉 |
|---|---|
| **車輛** `Cars.Status` | `Available`（上架中）→ `PendingPayment`（付款中）→ `Reserved`（已保留）→ `Sold`（已售出）<br>`PendingPayment` 逾時 → `Available` |
| **訂單** `Orders.PaymentStatus` | `Pending`（待付款）→ `Paid`（已付款）／`Failed`（失敗）／`Expired`（逾期） |
| **預約** `Reservations.Status` | `Pending`（審核中）；之後由後台處理 |
| **議價** `Offers.Status` | `Pending`（審核中）；之後由後台處理 |
| **賣車** `TradeInRequests.Status` | `Pending`（審核中）；之後由後台處理 |

固定值的欄位：

| 欄位 | 可用的值 |
|---|---|
| `CarPosts.Category` | `Question`（詢問）、`Review`（心得）、`Discussion`（討論）、`Maintenance`（保養） |
| `Cars.FuelType` | `Gasoline`（汽油）、`Diesel`（柴油）、`Hybrid`（油電）、`Electric`（電動） |
| `Cars.Transmission` | `Manual`（手排）、`Automatic`（自排） |
| `Users.AuthProvider` | `Local`（Email 註冊）、`Google` |

---

## 6. 資料表定義

以原始資料庫設計（手繪表格版）為基礎，套用訪談決定後的**完整定義**。「變更」欄標示相對原始設計的差異，空白表示與原始設計相同。

通用規則：
- 所有 `Id` 為 `INT IDENTITY(1,1)` 主鍵，**`Roles` 例外**（見下方）
- 所有時間欄位為 `DATETIME2`，以 **UTC** 儲存；`CreatedAt` 設定預設值 `DEFAULT SYSUTCDATETIME()`，新增資料時不需要由程式提供
- **所有金額欄位為 `INT`（整數元）**，符合綠界金額必須為整數的要求
- 狀態欄位為 `NVARCHAR(20)`，值為第 5 節定義的英文值，並加上 CHECK 約束
- 外鍵預設 `ON DELETE NO ACTION`，避免 SQL Server 的多重串聯刪除路徑錯誤
- 所有約束與索引都明確命名，前綴為 `PK_`、`FK_`、`UX_`（唯一索引）、`IX_`（一般索引）、`CK_`、`DF_`
- 除了各資料表列出的唯一索引，另外建立：
  - 所有外鍵欄位的一般索引（已被主鍵開頭欄位涵蓋的不重複建立）
  - `Reservations`、`Offers`：`(UserId, CarId) WHERE Status = 'Pending'` 篩選唯一索引，防止同時送出造成重複申請
  - `Orders`：`(CarId) WHERE PaymentStatus = 'Pending'` 篩選唯一索引，`RowVersion` 之外的第二道防線
  - `CarImages`：`(CarId) WHERE IsPrimary = 1` 篩選唯一索引，每台車只能有一張主圖
  - 違反唯一索引時 SQL Server 回傳錯誤碼 2601／2627，API 須轉成 409（見 ARCHITECTURE.md 第 4 節）
- 未特別標示「可 NULL」的欄位皆為 `NOT NULL`

### Roles

| 欄位 | 型別 | 說明 | 變更 |
|---|---|---|---|
| `Id` | INT | 主鍵，**不使用 IDENTITY**，固定值 | [修改] 固定 ID，程式中以常數對應 |
| `Name` | NVARCHAR(50) | 角色名稱，唯一 | [修改] 加上唯一索引 |

固定資料：`1 = Boss`、`2 = Customer`。

### Users

| 欄位 | 型別 | 說明 | 變更 |
|---|---|---|---|
| `Id` | INT | 主鍵 | |
| `Email` | NVARCHAR(255) | 信箱，唯一 | [修改] 加上唯一索引 |
| `Password` | NVARCHAR(60)，可 NULL | BCrypt 雜湊值 | [修改] 允許 NULL：Google 會員沒有密碼（Q3） |
| `Name` | NVARCHAR(50) | 姓名 | |
| `Phone` | NVARCHAR(10)，可 NULL | 手機 | [修改] 允許 NULL：Google 會員補填前為空 |
| `IdentityNumber` | NVARCHAR(500)，可 NULL | 身分證（加密後的密文） | [修改] 長度 10 → 500：密文比原文長；允許 NULL（Q4） |
| `IdentityNumberHash` | NVARCHAR(64)，可 NULL | 身分證的 HMAC-SHA256，用來檢查重複 | [新增] 篩選唯一索引 `WHERE IdentityNumberHash IS NOT NULL` |
| `Address` | NVARCHAR(100)，可 NULL | 地址 | [修改] 允許 NULL：Google 會員補填前為空 |
| `RoleId` | INT | 外鍵 → Roles | |
| `AuthProvider` | NVARCHAR(50) | 註冊方式：`Local` 或 `Google` | |
| `ProviderId` | NVARCHAR(255)，可 NULL | 第三方帳號 ID（Google 的 sub） | [修改] `NVARCHAR(MAX)` → `NVARCHAR(255)`：MAX 無法建立索引；加上 `(AuthProvider, ProviderId)` 篩選唯一索引 |
| `IsProfileCompleted` | BIT，預設 0 | 是否已填完必填資料 | [新增] Google 會員補填流程使用 |
| `PrivacyAgreedAt` | DATETIME2，可 NULL | 同意個資條款的時間 | [新增] |
| `CreatedAt` | DATETIME2 | 建立時間 | |
| `LastLoginAt` | DATETIME2，可 NULL | 上次登入時間 | |

### Branches

| 欄位 | 型別 | 說明 | 變更 |
|---|---|---|---|
| `Id` | INT | 主鍵 | |
| `Name` | NVARCHAR(50) | 分店名 | |
| `Address` | NVARCHAR(100) | 地址 | |
| `Phone` | NVARCHAR(20) | 電話 | [修改] `NVARCHAR(9)` → `NVARCHAR(20)`：市話含區碼為 10 碼 |
| `CreatedAt` | DATETIME2 | 建立時間 | |

### Cars

| 欄位 | 型別 | 說明 | 變更 |
|---|---|---|---|
| `Id` | INT | 主鍵 | |
| `BranchId` | INT | 外鍵 → Branches | |
| `Brand` | NVARCHAR(50) | 品牌 | |
| `Color` | NVARCHAR(50) | 顏色 | |
| `Model` | NVARCHAR(50) | 型號 | |
| `ProductionYear` | INT | 出廠年份 | |
| `Mileage` | INT | 里程（公里） | |
| `Displacement` | INT | 排氣量（cc） | |
| `FuelType` | NVARCHAR(20) | 燃料，值見第 5 節 | [新增] |
| `Transmission` | NVARCHAR(20) | 變速箱，值見第 5 節 | [新增] |
| `Seats` | INT | 座位數 | [新增] |
| `SellingPrice` | INT | 售價（元） | [修改] `DECIMAL(10,2)` → `INT` |
| `CostPrice` | INT | 成本價（元）。**前台 API 絕不回傳** | [修改] `DECIMAL(10,2)` → `INT` |
| `Status` | NVARCHAR(20) | `Available`／`PendingPayment`／`Reserved`／`Sold` | [修改] 新增 `PendingPayment`（Q8） |
| `Description` | NVARCHAR(MAX) | 車況介紹 | |
| `CreatedAt` | DATETIME2 | 建立時間 | |
| `RowVersion` | ROWVERSION | 樂觀並行控制 | [新增]（Q8） |

### CarImages

| 欄位 | 型別 | 說明 | 變更 |
|---|---|---|---|
| `Id` | INT | 主鍵 | |
| `CarId` | INT | 外鍵 → Cars | |
| `ImageUrl` | NVARCHAR(255) | 圖片網址 | |
| `IsPrimary` | BIT | 是否為主圖 | |
| `CreatedAt` | DATETIME2 | 建立時間 | |

[移除] 原設計的 `ImageType` 欄位。

### UserFavorites

| 欄位 | 型別 | 說明 |
|---|---|---|
| `UserId` | INT | 複合主鍵、外鍵 → Users |
| `CarId` | INT | 複合主鍵、外鍵 → Cars |
| `CreatedAt` | DATETIME2 | 建立時間 |

### Reservations

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Id` | INT | 主鍵 |
| `CarId` | INT | 外鍵 → Cars |
| `UserId` | INT | 外鍵 → Users |
| `ReservationDate` | DATETIME2 | 預約看車時間 |
| `Status` | NVARCHAR(20) | `Pending` |
| `CreatedAt` | DATETIME2 | 建立時間 |

### Offers

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Id` | INT | 主鍵 |
| `CarId` | INT | 外鍵 → Cars |
| `UserId` | INT | 外鍵 → Users |
| `OfferPrice` | INT | 出價金額（元） |
| `Status` | NVARCHAR(20) | `Pending` |
| `CreatedAt` | DATETIME2 | 建立時間 |
| `UpdatedAt` | DATETIME2，可 NULL | 最後修改時間 |

### Orders

| 欄位 | 型別 | 說明 | 變更 |
|---|---|---|---|
| `Id` | INT | 主鍵 | |
| `OrderNo` | NVARCHAR(20) | 訂單編號，英數字，唯一 | [修改] `NVARCHAR(50)` → `NVARCHAR(20)` 並加唯一索引：綠界 `MerchantTradeNo` 規定最長 20 碼英數字 |
| `CarId` | INT | 外鍵 → Cars | |
| `UserId` | INT | 外鍵 → Users | |
| `DepositAmount` | INT | 訂金（元） | [修改] `DECIMAL(10,2)` → `INT` |
| `PaymentStatus` | NVARCHAR(20) | `Pending`／`Paid`／`Failed`／`Expired` | |
| `PaymentMethod` | NVARCHAR(50)，可 NULL | 付款方式（綠界回傳） | |
| `TradeNo` | NVARCHAR(100)，可 NULL | 綠界交易序號 | |
| `CreatedAt` | DATETIME2 | 建立時間 | |
| `ExpiresAt` | DATETIME2 | 暫鎖到期時間（建立後 15 分鐘） | [新增]（Q8） |
| `PaidAt` | DATETIME2，可 NULL | 付款時間 | |

### TradeInRequests

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Id` | INT | 主鍵 |
| `UserId` | INT | 外鍵 → Users |
| `Brand` | NVARCHAR(50) | 品牌 |
| `Model` | NVARCHAR(50) | 型號 |
| `ProductionYear` | INT | 出廠年份 |
| `Mileage` | INT | 里程 |
| `ExpectedPrice` | INT | 期望價格（元） |
| `Status` | NVARCHAR(20) | `Pending` |
| `CreatedAt` | DATETIME2 | 建立時間 |

### TradeInImages

原設計名稱為 `TradeImaage`，修正拼字並與其他資料表命名一致。

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Id` | INT | 主鍵 |
| `TradeInRequestId` | INT | 外鍵 → TradeInRequests |
| `ImageUrl` | NVARCHAR(255) | 圖片網址 |
| `CreatedAt` | DATETIME2 | 建立時間 |

### CarPosts

| 欄位 | 型別 | 說明 | 變更 |
|---|---|---|---|
| `Id` | INT | 主鍵 | |
| `CarId` | INT，可 NULL | 外鍵 → Cars，關聯的車輛 | [修改] 改為非必填：可發表不針對特定車輛的文章 |
| `UserId` | INT | 外鍵 → Users（發文者） | |
| `Title` | NVARCHAR(50) | 標題 | |
| `Content` | NVARCHAR(MAX) | 內容 | |
| `Category` | NVARCHAR(20) | 貼文分類，值見第 5 節 | [修改] 加上 CHECK 約束 |
| `CreatedAt` | DATETIME2 | 建立時間 | |

### CarPostComments

| 欄位 | 型別 | 說明 | 變更 |
|---|---|---|---|
| `Id` | INT | 主鍵 | |
| `PostId` | INT | 外鍵 → CarPosts | |
| `UserId` | INT | 外鍵 → Users（留言者） | |
| `Content` | NVARCHAR(MAX) | 內容 | |
| `ParentId` | INT，可 NULL | 回覆的留言 | [修改] 加上外鍵 → CarPostComments（自我參照） |
| `CreatedAt` | DATETIME2 | 建立時間 | |

### SalesRecords（第一版不使用）

後台銷售統計使用，保留結構，前台不存取。

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Id` | INT | 主鍵 |
| `CarId` | INT | 外鍵 → Cars |
| `UserId` | INT | 外鍵 → Users |
| `FinalPrice` | INT | 成交價（元） |
| `SaleDate` | DATETIME2 | 成交日期 |
| `CreatedAt` | DATETIME2 | 建立時間 |

### 列入「之後再做」的欄位（第一版不加）

`Offers.Message`、`Offers.BossReply`、`Reservations.Message`、`TradeInRequests.Description`、`TradeInRequests.BossNotes`

---

## 7. API 端點清單

所有端點以 `/api` 開頭。[登入] 表示需要登入。

### 認證與會員

| 方法 | 路徑 | 說明 |
|---|---|---|
| POST | `/api/auth/register` | Email 註冊 |
| POST | `/api/auth/login` | Email 登入 |
| GET | `/api/auth/google/authorize-url` | 取得 Google 授權網址（後端產生 state 與 PKCE） |
| POST | `/api/auth/google/callback` | 以授權碼完成 Google 登入 |
| POST | `/api/auth/complete-profile` | [登入] Google 會員補填資料 |
| GET | `/api/me` | [登入] 取得個人資料（身分證為遮罩格式） |
| PUT | `/api/me` | [登入] 修改個人資料 |

### 車輛與分店

| 方法 | 路徑 | 說明 |
|---|---|---|
| GET | `/api/cars` | 車輛列表。參數：`branchId`、`brand`、`minPrice`、`maxPrice`、`fuelType`、`transmission`、`seats`、`page`、`pageSize`、`sort` |
| GET | `/api/cars/{id}` | 車輛詳情（含圖片） |
| GET | `/api/brands` | 品牌清單（篩選用） |
| GET | `/api/branches` | 分店列表 |

### 收藏

| 方法 | 路徑 | 說明 |
|---|---|---|
| GET | `/api/me/favorites` | [登入] 我的收藏 |
| POST | `/api/cars/{id}/favorite` | [登入] 加入收藏 |
| DELETE | `/api/cars/{id}/favorite` | [登入] 移除收藏 |

### 申請類

| 方法 | 路徑 | 說明 |
|---|---|---|
| POST | `/api/reservations` | [登入] 預約賞車 |
| GET | `/api/me/reservations` | [登入] 我的預約 |
| POST | `/api/offers` | [登入] 提出議價 |
| GET | `/api/me/offers` | [登入] 我的議價 |
| POST | `/api/trade-ins` | [登入] 賣車申請（`multipart/form-data`，含圖片） |
| GET | `/api/me/trade-ins` | [登入] 我的賣車申請 |

### 訂單與付款

| 方法 | 路徑 | 說明 |
|---|---|---|
| POST | `/api/orders` | [登入] 建立訂單並暫鎖車輛，回傳跳轉綠界所需的參數 |
| GET | `/api/me/orders` | [登入] 我的訂單 |
| GET | `/api/orders/{orderNo}` | [登入] 訂單詳情（只能看自己的） |
| POST | `/api/payments/ecpay/notify` | 綠界伺服器端回呼（驗證 CheckMacValue，不需登入） |
| POST | `/api/payments/ecpay/return` | 付款完成後瀏覽器導回的頁面 |

### 論壇

| 方法 | 路徑 | 說明 |
|---|---|---|
| GET | `/api/posts` | 文章列表。參數：`carId`（選填）、`category`（選填）、`page` |
| GET | `/api/posts/{id}` | 文章詳情＋巢狀留言 |
| POST | `/api/posts` | [登入] 發文 |
| POST | `/api/posts/{id}/comments` | [登入] 留言或回覆（`parentId` 可選） |

---

## 8. 非功能需求

### 安全

- **機密資料**：連線字串、綠界金鑰、Google Client Secret 使用 User Secrets 或環境變數，不進 git
- **密碼**：BCrypt 雜湊（work factor 12）
- **真人驗證**：註冊、登入需通過 reCAPTCHA v2，且由後端驗證
- **速率限制**：登入、註冊、Google callback 依 IP 限制呼叫次數
- **身分證**：加密儲存、API 只回傳遮罩格式
- **成本價**：前台所有 API 都不回傳 `CostPrice`
- **SQL Injection**：一律使用 EF Core LINQ 或參數化查詢
- **XSS**：論壇內容輸出時編碼；前端不使用 `v-html` 顯示使用者輸入；設定 CSP 標頭。因為登入 token 存在 localStorage，XSS 防護是保護 token 的主要防線
- **授權**：會員只能存取自己的資料（例如 `/api/orders/{orderNo}` 要檢查訂單擁有者）
- **金流**：驗證綠界 CheckMacValue；回呼處理要能承受重複送達（冪等）
- **檔案上傳**：限制副檔名（jpg／png／webp）與大小；檢查檔案實際內容；以隨機檔名儲存

### 品質

- 後端：xUnit 單元測試；關鍵流程（下單、付款回呼、併發）要有測試
- 驗證：前端驗證提供即時回饋，**後端驗證才是真正的防線**

---

## 9. 細節確認紀錄

| # | 問題 | 決定 |
|---|---|---|
| 1 | 訂金比例 | 5%（設定檔可調） |
| 2 | 訪客可以瀏覽車輛與論壇嗎？ | 可以 |
| 3 | 同一會員對同一台車，能否同時有多筆審核中的預約或議價？ | 不行，只能各一筆 |
| 4 | 議價金額限制 | 必須大於 0 且低於售價 |
| 5 | 論壇巢狀回覆層數 | 2 層（回覆的回覆不再縮排） |
| 6 | 論壇第一版能刪除自己的文章嗎？ | 不做 |
| 7 | `CarImages.ImageType` | 移除此欄位 |
| 8 | `Branches.Phone` 長度 | 改為 `NVARCHAR(20)` |
| 9 | `TradeImaage` 名稱 | 改名為 `TradeInImages` |
| 10 | 預約賞車時間 | 任意未來時間，不採固定時段 |
| 11 | 品牌是否獨立成表 | 不獨立，維持自由輸入；等後台時再處理 |
| 12 | 狀態與分類在程式中的表示 | 常數類別，資料庫以 CHECK 約束限制 |
| 13 | `Roles` 的 ID | 固定值：1 = Boss、2 = Customer |
| 14 | 身分證雜湊欄位 | 保留 `IdentityNumberHash`，由唯一索引防止重複 |
| 15 | 是否使用 Refresh Token | 不使用，改為單一 JWT，有效期 2 小時 |
| 16 | 會員大頭貼 | 第一版不做 |
| 17 | 論壇分類 | Question、Review、Discussion、Maintenance |
| 18 | 論壇文章是否必須關聯車輛 | 否，`CarPosts.CarId` 改為非必填 |
| 19 | 車輛新增欄位 | 燃料、變速箱、座位數，並加入列表篩選 |
| 20 | 金額型別 | 全部改為 `INT` |
| 21 | `CreatedAt` 預設值 | 資料庫設定 `DEFAULT SYSUTCDATETIME()` |
| 22 | 第 6 節以外的索引 | 外鍵一般索引；預約、議價、付款中訂單、主圖的篩選唯一索引（見第 6 節通用規則） |
| 23 | 數值範圍的 CHECK 約束 | 不加，由後端 API 驗證 |
| 24 | 狀態欄位預設值 | 不設，由程式以常數明確指定 |
| 25 | `data.sql` 重複執行 | 清空所有資料表並重設 IDENTITY 後重新寫入，整份包在一個交易中；僅供本機開發使用 |
| 26 | 腳本是否指定資料庫 | 不含 `CREATE DATABASE`／`USE`，執行前在 SSMS 選擇目標資料庫；整合測試對測試資料庫執行同一份 `schema.sql` |
| 27 | 品牌寫法 | 英文原廠名稱（例：`Toyota`、`Mercedes-Benz`） |
| 28 | 電動車排氣量 | 存 `0`；API 或前端顯示為「不適用」，不可顯示「0 cc」 |
| 29 | 電話格式 | 分店電話與會員手機**一律只存數字**，顯示時再加上連字號；API 收到含連字號或空白的輸入時，先移除再驗證與儲存。會員手機固定 10 碼且以 `09` 開頭（例：`0912345678`）；市話含區碼為 9～10 碼（例：台北 `0227971688`、高雄 `075563660`） |
| 30 | 種子資料圖片 | 使用**本機圖片檔**，放在 API 專案的 `wwwroot/images/seed/`，`data.sql` 存相對路徑（例：`/images/seed/car-1-1.png`）；檔名格式 `car-{車輛Id}-{序號}.png`，800x600 佔位圖；不依賴外部服務，CSP 不需例外 |
| 31 | 預約、議價、賣車的 CHECK 約束 | 目前只允許 `Pending`；做後台加入其他狀態時須一併修改 |
