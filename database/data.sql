/* =====================================================================
   CarShop 初始資料
   前置條件：已執行 database/schema.sql
   說明：
     - 不含 USE，請先在 SSMS 選好目標資料庫再執行
     - 可重複執行：會先清空「所有」資料表（包含會員與其收藏、訂單、論壇等），
       再重設 IDENTITY 並寫入固定資料。測試會員需重新透過註冊 API 建立
     - 不建立會員資料：身分證必須由程式加密，SQL 無法產生
     - 整段在同一個交易內執行，任何一步失敗都會整批回復
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;          -- 有篩選索引的資料表，寫入時需要
SET QUOTED_IDENTIFIER ON;   -- 有篩選索引的資料表，寫入時需要

BEGIN TRANSACTION;

/* ---------------------------------------------------------------------
   1. 清空資料（先刪子資料表，再刪父資料表）
   --------------------------------------------------------------------- */
DELETE FROM dbo.CarPostComments;   -- 自我參照：同一個 DELETE 刪除全部列不會違反外鍵
DELETE FROM dbo.CarPosts;
DELETE FROM dbo.TradeInImages;
DELETE FROM dbo.TradeInRequests;
DELETE FROM dbo.SalesRecords;
DELETE FROM dbo.Orders;
DELETE FROM dbo.Offers;
DELETE FROM dbo.Reservations;
DELETE FROM dbo.UserFavorites;
DELETE FROM dbo.CarImages;
DELETE FROM dbo.Cars;
DELETE FROM dbo.Branches;
DELETE FROM dbo.Users;
DELETE FROM dbo.Roles;

/* ---------------------------------------------------------------------
   2. 重設 IDENTITY，讓下一筆從 1 開始
      只對「曾經寫入過資料」的資料表重設：
      剛建立、從未寫入的資料表若執行 RESEED 0，下一筆會變成 0
   --------------------------------------------------------------------- */
DECLARE @tables TABLE (Name SYSNAME NOT NULL);
INSERT INTO @tables (Name) VALUES
    (N'Users'), (N'Branches'), (N'Cars'), (N'CarImages'),
    (N'Reservations'), (N'Offers'), (N'Orders'),
    (N'TradeInRequests'), (N'TradeInImages'),
    (N'CarPosts'), (N'CarPostComments'), (N'SalesRecords');

DECLARE @name SYSNAME, @sql NVARCHAR(400);
DECLARE reseed_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.Name
    FROM @tables AS t
    JOIN sys.identity_columns AS ic ON ic.object_id = OBJECT_ID(N'dbo.' + t.Name)
    WHERE ic.last_value IS NOT NULL;

OPEN reseed_cursor;
FETCH NEXT FROM reseed_cursor INTO @name;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'DBCC CHECKIDENT (N''dbo.' + QUOTENAME(@name) + N''', RESEED, 0) WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @sql;
    FETCH NEXT FROM reseed_cursor INTO @name;
END
CLOSE reseed_cursor;
DEALLOCATE reseed_cursor;

/* ---------------------------------------------------------------------
   3. Roles（固定 ID，需與程式的 RoleIds 常數一致）
   --------------------------------------------------------------------- */
INSERT INTO dbo.Roles (Id, Name) VALUES
    (1, N'Boss'),
    (2, N'Customer');

/* ---------------------------------------------------------------------
   4. Branches（指定 Id，讓 Cars 可以穩定參照；電話只存數字）
   --------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Branches ON;

INSERT INTO dbo.Branches (Id, Name, Address, Phone) VALUES
    (1, N'台北內湖店', N'台北市內湖區瑞光路 168 號',   N'0227971688'),
    (2, N'台中西屯店', N'台中市西屯區台灣大道三段 520 號', N'0423585200'),
    (3, N'高雄左營店', N'高雄市左營區博愛二路 366 號', N'075563660');

SET IDENTITY_INSERT dbo.Branches OFF;

/* ---------------------------------------------------------------------
   5. Cars（15 台，狀態皆為 Available）
      品牌一律使用英文原廠名稱；電動車排氣量為 0
   --------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Cars ON;

INSERT INTO dbo.Cars
    (Id, BranchId, Brand, Color, Model, ProductionYear, Mileage, Displacement,
     FuelType, Transmission, Seats, SellingPrice, CostPrice, Status, Description)
VALUES
    ( 1, 1, N'Toyota',        N'白色',   N'Corolla Altis 1.8',       2019,  62000, 1798, N'Gasoline', N'Automatic', 5,  465000,  398000, N'Available',
      N'一手車，原廠定期保養紀錄完整。內裝乾淨無異味，輪胎於去年更換。適合通勤與家庭使用。'),
    ( 2, 2, N'Toyota',        N'銀色',   N'RAV4 2.5 Hybrid',         2021,  38000, 2487, N'Hybrid',   N'Automatic', 5,  958000,  860000, N'Available',
      N'油電版本，市區油耗表現優異。配備 TSS 主動安全系統、電動尾門，原廠保固尚有剩餘。'),
    ( 3, 3, N'Toyota',        N'珍珠白', N'Sienta 1.5 Hybrid',       2023,  18000, 1496, N'Hybrid',   N'Automatic', 7,  728000,  650000, N'Available',
      N'七人座油電小休旅，雙側電動滑門方便接送小孩。里程低、車況近新。'),
    ( 4, 1, N'Honda',         N'黑色',   N'Civic Type R 2.0',        2018,  45000, 1996, N'Gasoline', N'Manual',    4, 1380000, 1250000, N'Available',
      N'FK8 世代，六速手排。全車原漆，無賽道使用紀錄，原廠保養。'),
    ( 5, 2, N'Honda',         N'灰色',   N'CR-V 1.5 VTi-S',          2020,  55000, 1498, N'Gasoline', N'Automatic', 5,  798000,  715000, N'Available',
      N'渦輪增壓引擎，後座與行李廂空間寬敞。配備 Honda SENSING，已完成第三方認證檢測。'),
    ( 6, 3, N'Mazda',         N'紅色',   N'MX-5 2.0',                2017,  41000, 1998, N'Gasoline', N'Manual',    2,  788000,  700000, N'Available',
      N'雙門敞篷跑車，經典魂動紅。軟頂無破損，六速手排換檔手感佳。'),
    ( 7, 1, N'Mazda',         N'深灰色', N'CX-5 2.2 SKYACTIV-D',     2019,  72000, 2191, N'Diesel',   N'Automatic', 5,  698000,  620000, N'Available',
      N'柴油版本，扭力充沛、長途油耗表現好。配備 BOSE 音響與 360 度環景影像。'),
    ( 8, 2, N'Lexus',         N'黑色',   N'ES 300h',                 2020,  48000, 2487, N'Hybrid',   N'Automatic', 5, 1450000, 1320000, N'Available',
      N'油電豪華房車，車內安靜舒適。原廠保養，配備電動椅記憶、Mark Levinson 音響。'),
    ( 9, 1, N'BMW',           N'藍色',   N'320i M Sport',            2021,  30000, 1998, N'Gasoline', N'Automatic', 5, 1580000, 1430000, N'Available',
      N'G20 世代 M Sport 套件，操控靈活。原廠保固內，配備數位儀表與抬頭顯示器。'),
    (10, 3, N'Mercedes-Benz', N'黑色',   N'V250d',                   2019,  85000, 2143, N'Diesel',   N'Automatic', 7, 2280000, 2080000, N'Available',
      N'七人座商旅，雙側電動滑門、獨立座椅。適合商務接送與大家庭出遊。'),
    (11, 1, N'Tesla',         N'白色',   N'Model 3 Long Range',      2022,  28000,    0, N'Electric', N'Automatic', 5, 1290000, 1170000, N'Available',
      N'雙馬達四輪驅動長程版，電池健康度良好。附原廠家用充電座。'),
    (12, 2, N'Ford',          N'白色',   N'Focus 1.5 EcoBoost',      2016,  98000, 1497, N'Gasoline', N'Automatic', 5,  328000,  275000, N'Available',
      N'入門代步首選，價格親民。已更換電瓶與煞車來令片，車況穩定。'),
    (13, 3, N'Volkswagen',    N'灰色',   N'Golf 280 TSI',            2018,  67000, 1395, N'Gasoline', N'Automatic', 5,  528000,  460000, N'Available',
      N'經典掀背車，底盤紮實。DSG 變速箱已完成保養，附完整保養紀錄。'),
    (14, 2, N'Hyundai',       N'銀色',   N'IONIQ 5',                 2023,  15000,    0, N'Electric', N'Automatic', 5, 1350000, 1230000, N'Available',
      N'純電跨界休旅，支援 800V 快充。車內空間寬敞，配備 V2L 對外供電功能。'),
    (15, 3, N'Suzuki',        N'綠色',   N'Jimny 1.5',               2020,  33000, 1462, N'Gasoline', N'Manual',    4,  868000,  790000, N'Available',
      N'硬派越野小車，五速手排、分時四驅。無涉水紀錄，原廠配件齊全。');

SET IDENTITY_INSERT dbo.Cars OFF;

/* ---------------------------------------------------------------------
   6. CarImages（每台 2～3 張，第一張為主圖）
      圖片檔位於 API 專案的 wwwroot/images/seed/，檔名為 car-{車輛Id}-{序號}.png
   --------------------------------------------------------------------- */
INSERT INTO dbo.CarImages (CarId, ImageUrl, IsPrimary) VALUES
    ( 1, N'/images/seed/car-1-1.png', 1),
    ( 1, N'/images/seed/car-1-2.png', 0),
    ( 1, N'/images/seed/car-1-3.png', 0),

    ( 2, N'/images/seed/car-2-1.png', 1),
    ( 2, N'/images/seed/car-2-2.png', 0),
    ( 2, N'/images/seed/car-2-3.png', 0),

    ( 3, N'/images/seed/car-3-1.png', 1),
    ( 3, N'/images/seed/car-3-2.png', 0),

    ( 4, N'/images/seed/car-4-1.png', 1),
    ( 4, N'/images/seed/car-4-2.png', 0),
    ( 4, N'/images/seed/car-4-3.png', 0),

    ( 5, N'/images/seed/car-5-1.png', 1),
    ( 5, N'/images/seed/car-5-2.png', 0),

    ( 6, N'/images/seed/car-6-1.png', 1),
    ( 6, N'/images/seed/car-6-2.png', 0),
    ( 6, N'/images/seed/car-6-3.png', 0),

    ( 7, N'/images/seed/car-7-1.png', 1),
    ( 7, N'/images/seed/car-7-2.png', 0),

    ( 8, N'/images/seed/car-8-1.png', 1),
    ( 8, N'/images/seed/car-8-2.png', 0),
    ( 8, N'/images/seed/car-8-3.png', 0),

    ( 9, N'/images/seed/car-9-1.png', 1),
    ( 9, N'/images/seed/car-9-2.png', 0),
    ( 9, N'/images/seed/car-9-3.png', 0),

    (10, N'/images/seed/car-10-1.png', 1),
    (10, N'/images/seed/car-10-2.png', 0),

    (11, N'/images/seed/car-11-1.png', 1),
    (11, N'/images/seed/car-11-2.png', 0),
    (11, N'/images/seed/car-11-3.png', 0),

    (12, N'/images/seed/car-12-1.png', 1),
    (12, N'/images/seed/car-12-2.png', 0),

    (13, N'/images/seed/car-13-1.png', 1),
    (13, N'/images/seed/car-13-2.png', 0),
    (13, N'/images/seed/car-13-3.png', 0),

    (14, N'/images/seed/car-14-1.png', 1),
    (14, N'/images/seed/car-14-2.png', 0),
    (14, N'/images/seed/car-14-3.png', 0),

    (15, N'/images/seed/car-15-1.png', 1),
    (15, N'/images/seed/car-15-2.png', 0);

COMMIT TRANSACTION;

/* ---------------------------------------------------------------------
   7. 檢查結果（預期：Roles 2、Branches 3、Cars 15、CarImages 39）
   --------------------------------------------------------------------- */
SELECT N'Roles'     AS TableName, COUNT(*) AS TotalRows FROM dbo.Roles
UNION ALL SELECT N'Branches',  COUNT(*) FROM dbo.Branches
UNION ALL SELECT N'Cars',      COUNT(*) FROM dbo.Cars
UNION ALL SELECT N'CarImages', COUNT(*) FROM dbo.CarImages;
