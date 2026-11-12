/* =====================================================================
   CarShop 資料庫結構
   依據：docs/REQUIREMENTS.md 第 5、6 節、docs/ARCHITECTURE.md 第 6 節
   用途：本機建庫、整合測試的測試資料庫建表
   說明：
     - 腳本不含 CREATE DATABASE / USE，請先在 SSMS 選好目標資料庫再執行
       （整合測試會對測試資料庫執行同一份腳本）
     - 可重複執行：已存在的資料表會先刪除再建立（資料會一併清除）
     - 外鍵一律 ON DELETE NO ACTION
     - 時間欄位皆以 UTC 儲存
   ===================================================================== */

SET ANSI_NULLS ON;          -- 篩選索引需要
SET QUOTED_IDENTIFIER ON;   -- 篩選索引需要
GO

/* ---------------------------------------------------------------------
   1. 刪除既有資料表（先刪參照別人的子資料表，再刪被參照的父資料表）
   --------------------------------------------------------------------- */
DROP TABLE IF EXISTS dbo.CarPostComments;   -- → CarPosts, Users, 自我參照
DROP TABLE IF EXISTS dbo.CarPosts;          -- → Cars, Users
DROP TABLE IF EXISTS dbo.TradeInImages;     -- → TradeInRequests
DROP TABLE IF EXISTS dbo.TradeInRequests;   -- → Users
DROP TABLE IF EXISTS dbo.SalesRecords;      -- → Cars, Users
DROP TABLE IF EXISTS dbo.Orders;            -- → Cars, Users
DROP TABLE IF EXISTS dbo.Offers;            -- → Cars, Users
DROP TABLE IF EXISTS dbo.Reservations;      -- → Cars, Users
DROP TABLE IF EXISTS dbo.UserFavorites;     -- → Cars, Users
DROP TABLE IF EXISTS dbo.CarImages;         -- → Cars
DROP TABLE IF EXISTS dbo.Cars;              -- → Branches
DROP TABLE IF EXISTS dbo.Branches;
DROP TABLE IF EXISTS dbo.Users;             -- → Roles
DROP TABLE IF EXISTS dbo.Roles;
GO

/* ---------------------------------------------------------------------
   2. Roles：固定 ID，不使用 IDENTITY（1 = Boss、2 = Customer）
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Roles
(
    Id      INT            NOT NULL,
    Name    NVARCHAR(50)   NOT NULL,

    CONSTRAINT PK_Roles PRIMARY KEY (Id)
);

CREATE UNIQUE INDEX UX_Roles_Name ON dbo.Roles (Name);
GO

/* ---------------------------------------------------------------------
   3. Users
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Users
(
    Id                  INT IDENTITY(1,1) NOT NULL,
    Email               NVARCHAR(255)  NOT NULL,
    Password            NVARCHAR(60)   NULL,        -- BCrypt 雜湊；Google 會員為 NULL
    Name                NVARCHAR(50)   NOT NULL,
    Phone               NVARCHAR(10)   NULL,
    IdentityNumber      NVARCHAR(500)  NULL,        -- Data Protection 加密後的密文
    IdentityNumberHash  NVARCHAR(64)   NULL,        -- HMAC-SHA256，用來檢查重複
    Address             NVARCHAR(100)  NULL,
    RoleId              INT            NOT NULL,
    AuthProvider        NVARCHAR(50)   NOT NULL,
    ProviderId          NVARCHAR(255)  NULL,        -- Google 的 sub
    IsProfileCompleted  BIT            NOT NULL CONSTRAINT DF_Users_IsProfileCompleted DEFAULT (0),
    PrivacyAgreedAt     DATETIME2      NULL,
    CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    LastLoginAt         DATETIME2      NULL,

    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Users_AuthProvider CHECK (AuthProvider IN (N'Local', N'Google'))
);

CREATE UNIQUE INDEX UX_Users_Email
    ON dbo.Users (Email);

CREATE UNIQUE INDEX UX_Users_IdentityNumberHash
    ON dbo.Users (IdentityNumberHash)
    WHERE IdentityNumberHash IS NOT NULL;

CREATE UNIQUE INDEX UX_Users_AuthProvider_ProviderId
    ON dbo.Users (AuthProvider, ProviderId)
    WHERE ProviderId IS NOT NULL;

CREATE INDEX IX_Users_RoleId ON dbo.Users (RoleId);
GO

/* ---------------------------------------------------------------------
   4. Branches
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Branches
(
    Id          INT IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(50)   NOT NULL,
    Address     NVARCHAR(100)  NOT NULL,
    Phone       NVARCHAR(20)   NOT NULL,
    CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_Branches_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Branches PRIMARY KEY (Id)
);
GO

/* ---------------------------------------------------------------------
   5. Cars
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Cars
(
    Id              INT IDENTITY(1,1) NOT NULL,
    BranchId        INT            NOT NULL,
    Brand           NVARCHAR(50)   NOT NULL,
    Color           NVARCHAR(50)   NOT NULL,
    Model           NVARCHAR(50)   NOT NULL,
    ProductionYear  INT            NOT NULL,
    Mileage         INT            NOT NULL,       -- 公里
    Displacement    INT            NOT NULL,       -- cc
    FuelType        NVARCHAR(20)   NOT NULL,
    Transmission    NVARCHAR(20)   NOT NULL,
    Seats           INT            NOT NULL,
    SellingPrice    INT            NOT NULL,       -- 元
    CostPrice       INT            NOT NULL,       -- 元，前台 API 絕不回傳
    Status          NVARCHAR(20)   NOT NULL,
    Description     NVARCHAR(MAX)  NOT NULL,
    CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_Cars_CreatedAt DEFAULT (SYSUTCDATETIME()),
    RowVersion      ROWVERSION     NOT NULL,       -- 樂觀並行控制

    CONSTRAINT PK_Cars PRIMARY KEY (Id),
    CONSTRAINT FK_Cars_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches (Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Cars_Status       CHECK (Status       IN (N'Available', N'PendingPayment', N'Reserved', N'Sold')),
    CONSTRAINT CK_Cars_FuelType     CHECK (FuelType     IN (N'Gasoline', N'Diesel', N'Hybrid', N'Electric')),
    CONSTRAINT CK_Cars_Transmission CHECK (Transmission IN (N'Manual', N'Automatic'))
);

CREATE INDEX IX_Cars_BranchId ON dbo.Cars (BranchId);
GO

/* ---------------------------------------------------------------------
   6. CarImages
   --------------------------------------------------------------------- */
CREATE TABLE dbo.CarImages
(
    Id          INT IDENTITY(1,1) NOT NULL,
    CarId       INT            NOT NULL,
    ImageUrl    NVARCHAR(255)  NOT NULL,
    IsPrimary   BIT            NOT NULL,
    CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_CarImages_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_CarImages PRIMARY KEY (Id),
    CONSTRAINT FK_CarImages_Cars FOREIGN KEY (CarId) REFERENCES dbo.Cars (Id) ON DELETE NO ACTION
);

CREATE INDEX IX_CarImages_CarId ON dbo.CarImages (CarId);

-- 每台車最多一張主圖
CREATE UNIQUE INDEX UX_CarImages_CarId_Primary
    ON dbo.CarImages (CarId)
    WHERE IsPrimary = 1;
GO

/* ---------------------------------------------------------------------
   7. UserFavorites：複合主鍵 (UserId, CarId)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.UserFavorites
(
    UserId      INT        NOT NULL,
    CarId       INT        NOT NULL,
    CreatedAt   DATETIME2  NOT NULL CONSTRAINT DF_UserFavorites_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_UserFavorites PRIMARY KEY (UserId, CarId),
    CONSTRAINT FK_UserFavorites_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_UserFavorites_Cars  FOREIGN KEY (CarId)  REFERENCES dbo.Cars  (Id) ON DELETE NO ACTION
);

-- UserId 已由主鍵的第一個欄位涵蓋
CREATE INDEX IX_UserFavorites_CarId ON dbo.UserFavorites (CarId);
GO

/* ---------------------------------------------------------------------
   8. Reservations
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Reservations
(
    Id               INT IDENTITY(1,1) NOT NULL,
    CarId            INT           NOT NULL,
    UserId           INT           NOT NULL,
    ReservationDate  DATETIME2     NOT NULL,
    Status           NVARCHAR(20)  NOT NULL,
    CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_Reservations_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Reservations PRIMARY KEY (Id),
    CONSTRAINT FK_Reservations_Cars  FOREIGN KEY (CarId)  REFERENCES dbo.Cars  (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Reservations_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Reservations_Status CHECK (Status IN (N'Pending'))
);

CREATE INDEX IX_Reservations_CarId  ON dbo.Reservations (CarId);
CREATE INDEX IX_Reservations_UserId ON dbo.Reservations (UserId);

-- 同一會員對同一台車只能有一筆審核中的預約
CREATE UNIQUE INDEX UX_Reservations_UserId_CarId_Pending
    ON dbo.Reservations (UserId, CarId)
    WHERE Status = N'Pending';
GO

/* ---------------------------------------------------------------------
   9. Offers
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Offers
(
    Id          INT IDENTITY(1,1) NOT NULL,
    CarId       INT           NOT NULL,
    UserId      INT           NOT NULL,
    OfferPrice  INT           NOT NULL,     -- 元
    Status      NVARCHAR(20)  NOT NULL,
    CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_Offers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt   DATETIME2     NULL,

    CONSTRAINT PK_Offers PRIMARY KEY (Id),
    CONSTRAINT FK_Offers_Cars  FOREIGN KEY (CarId)  REFERENCES dbo.Cars  (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Offers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Offers_Status CHECK (Status IN (N'Pending'))
);

CREATE INDEX IX_Offers_CarId  ON dbo.Offers (CarId);
CREATE INDEX IX_Offers_UserId ON dbo.Offers (UserId);

-- 同一會員對同一台車只能有一筆審核中的出價
CREATE UNIQUE INDEX UX_Offers_UserId_CarId_Pending
    ON dbo.Offers (UserId, CarId)
    WHERE Status = N'Pending';
GO

/* ---------------------------------------------------------------------
   10. Orders
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Orders
(
    Id              INT IDENTITY(1,1) NOT NULL,
    OrderNo         NVARCHAR(20)   NOT NULL,    -- 綠界 MerchantTradeNo，最長 20 碼英數字
    CarId           INT            NOT NULL,
    UserId          INT            NOT NULL,
    DepositAmount   INT            NOT NULL,    -- 元
    PaymentStatus   NVARCHAR(20)   NOT NULL,
    PaymentMethod   NVARCHAR(50)   NULL,
    TradeNo         NVARCHAR(100)  NULL,        -- 綠界交易序號
    CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ExpiresAt       DATETIME2      NOT NULL,    -- 暫鎖到期時間
    PaidAt          DATETIME2      NULL,

    CONSTRAINT PK_Orders PRIMARY KEY (Id),
    CONSTRAINT FK_Orders_Cars  FOREIGN KEY (CarId)  REFERENCES dbo.Cars  (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Orders_PaymentStatus CHECK (PaymentStatus IN (N'Pending', N'Paid', N'Failed', N'Expired'))
);

CREATE UNIQUE INDEX UX_Orders_OrderNo ON dbo.Orders (OrderNo);

CREATE INDEX IX_Orders_CarId  ON dbo.Orders (CarId);
CREATE INDEX IX_Orders_UserId ON dbo.Orders (UserId);

-- 同一台車同時只能有一筆付款中的訂單（RowVersion 之外的第二道防線）
CREATE UNIQUE INDEX UX_Orders_CarId_Pending
    ON dbo.Orders (CarId)
    WHERE PaymentStatus = N'Pending';
GO

/* ---------------------------------------------------------------------
   11. TradeInRequests
   --------------------------------------------------------------------- */
CREATE TABLE dbo.TradeInRequests
(
    Id              INT IDENTITY(1,1) NOT NULL,
    UserId          INT           NOT NULL,
    Brand           NVARCHAR(50)  NOT NULL,
    Model           NVARCHAR(50)  NOT NULL,
    ProductionYear  INT           NOT NULL,
    Mileage         INT           NOT NULL,
    ExpectedPrice   INT           NOT NULL,     -- 元
    Status          NVARCHAR(20)  NOT NULL,
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_TradeInRequests_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_TradeInRequests PRIMARY KEY (Id),
    CONSTRAINT FK_TradeInRequests_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
    CONSTRAINT CK_TradeInRequests_Status CHECK (Status IN (N'Pending'))
);

CREATE INDEX IX_TradeInRequests_UserId ON dbo.TradeInRequests (UserId);
GO

/* ---------------------------------------------------------------------
   12. TradeInImages
   --------------------------------------------------------------------- */
CREATE TABLE dbo.TradeInImages
(
    Id                INT IDENTITY(1,1) NOT NULL,
    TradeInRequestId  INT            NOT NULL,
    ImageUrl          NVARCHAR(255)  NOT NULL,
    CreatedAt         DATETIME2      NOT NULL CONSTRAINT DF_TradeInImages_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_TradeInImages PRIMARY KEY (Id),
    CONSTRAINT FK_TradeInImages_TradeInRequests FOREIGN KEY (TradeInRequestId)
        REFERENCES dbo.TradeInRequests (Id) ON DELETE NO ACTION
);

CREATE INDEX IX_TradeInImages_TradeInRequestId ON dbo.TradeInImages (TradeInRequestId);
GO

/* ---------------------------------------------------------------------
   13. CarPosts
   --------------------------------------------------------------------- */
CREATE TABLE dbo.CarPosts
(
    Id          INT IDENTITY(1,1) NOT NULL,
    CarId       INT            NULL,            -- 可不關聯車輛
    UserId      INT            NOT NULL,
    Title       NVARCHAR(50)   NOT NULL,
    Content     NVARCHAR(MAX)  NOT NULL,
    Category    NVARCHAR(20)   NOT NULL,
    CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_CarPosts_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_CarPosts PRIMARY KEY (Id),
    CONSTRAINT FK_CarPosts_Cars  FOREIGN KEY (CarId)  REFERENCES dbo.Cars  (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_CarPosts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
    CONSTRAINT CK_CarPosts_Category CHECK (Category IN (N'Question', N'Review', N'Discussion', N'Maintenance'))
);

CREATE INDEX IX_CarPosts_CarId  ON dbo.CarPosts (CarId);
CREATE INDEX IX_CarPosts_UserId ON dbo.CarPosts (UserId);
GO

/* ---------------------------------------------------------------------
   14. CarPostComments：ParentId 自我參照
   --------------------------------------------------------------------- */
CREATE TABLE dbo.CarPostComments
(
    Id          INT IDENTITY(1,1) NOT NULL,
    PostId      INT            NOT NULL,
    UserId      INT            NOT NULL,
    Content     NVARCHAR(MAX)  NOT NULL,
    ParentId    INT            NULL,            -- 回覆的留言
    CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_CarPostComments_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_CarPostComments PRIMARY KEY (Id),
    CONSTRAINT FK_CarPostComments_CarPosts FOREIGN KEY (PostId)   REFERENCES dbo.CarPosts (Id)        ON DELETE NO ACTION,
    CONSTRAINT FK_CarPostComments_Users    FOREIGN KEY (UserId)   REFERENCES dbo.Users (Id)           ON DELETE NO ACTION,
    CONSTRAINT FK_CarPostComments_Parent   FOREIGN KEY (ParentId) REFERENCES dbo.CarPostComments (Id) ON DELETE NO ACTION
);

CREATE INDEX IX_CarPostComments_PostId   ON dbo.CarPostComments (PostId);
CREATE INDEX IX_CarPostComments_UserId   ON dbo.CarPostComments (UserId);
CREATE INDEX IX_CarPostComments_ParentId ON dbo.CarPostComments (ParentId);
GO

/* ---------------------------------------------------------------------
   15. SalesRecords（第一版不使用，保留給後台）
   --------------------------------------------------------------------- */
CREATE TABLE dbo.SalesRecords
(
    Id          INT IDENTITY(1,1) NOT NULL,
    CarId       INT        NOT NULL,
    UserId      INT        NOT NULL,
    FinalPrice  INT        NOT NULL,            -- 元
    SaleDate    DATETIME2  NOT NULL,
    CreatedAt   DATETIME2  NOT NULL CONSTRAINT DF_SalesRecords_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_SalesRecords PRIMARY KEY (Id),
    CONSTRAINT FK_SalesRecords_Cars  FOREIGN KEY (CarId)  REFERENCES dbo.Cars  (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SalesRecords_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION
);

CREATE INDEX IX_SalesRecords_CarId  ON dbo.SalesRecords (CarId);
CREATE INDEX IX_SalesRecords_UserId ON dbo.SalesRecords (UserId);
GO
