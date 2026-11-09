/*
    JodVyapar - 04_ProductsAndRequirements.sql
    Adds Units (with seed rows), Products, ProductImages and Requirements.

    - Run after 01, 02 and 03 (see Database/README.md).
    - Idempotent: objects that already exist are skipped and Units rows are matched on fixed GUIDs
      (MD5("jodvyapar:unit:<key>"), same scheme as 02_SeedData.sql), so it is safe to re-run.
    - Must match the EF Core mapping (UnitConfiguration, ProductConfiguration,
      ProductImageConfiguration, RequirementConfiguration).
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/* ===================== Units ===================== */

IF OBJECT_ID(N'dbo.Units', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Units (
        Id        uniqueidentifier NOT NULL,
        Name      nvarchar(50)     NOT NULL,
        Symbol    nvarchar(20)     NOT NULL,
        SortOrder int              NOT NULL CONSTRAINT DF_Units_SortOrder DEFAULT 0,
        IsActive  bit              NOT NULL CONSTRAINT DF_Units_IsActive DEFAULT 1,
        CreatedAt datetime2        NOT NULL CONSTRAINT DF_Units_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2        NOT NULL CONSTRAINT DF_Units_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Units PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Units_Name' AND object_id = OBJECT_ID(N'dbo.Units'))
    CREATE UNIQUE INDEX UX_Units_Name ON dbo.Units (Name);
GO

-- IsActive is left alone on existing rows, so a unit switched off in the database stays off.
MERGE dbo.Units AS target
USING (VALUES
    ('c3bf02ab-4cd4-ead0-2b19-027196960dea', N'Kilogram', N'kg', 1),
    ('6a5a24f0-e7d5-dacf-654d-c08f4c883c07', N'Quintal', N'qtl', 2),
    ('7ba72bee-eba4-621f-b00a-67a0f886e0e9', N'Tonne', N't', 3),
    ('3ecb06ae-ecfc-45a8-e7dc-6da98a64955c', N'Gram', N'g', 4),
    ('4e4c9141-35a8-f9bf-7fb9-83631468319f', N'Metre', N'm', 5),
    ('315b32df-156e-9b8a-76d9-517d4b0617e7', N'Piece', N'pc', 6),
    ('9ee91d3f-37a5-542e-4a9b-b08433fa3874', N'Dozen', N'doz', 7),
    ('8ee8603a-a238-1bed-6b63-be99d9df637c', N'Box', N'box', 8),
    ('a5149902-eb47-843d-e686-fb77a2956431', N'Carton', N'ctn', 9),
    ('f1c7126e-1cd2-1191-bd81-308c8ba80938', N'Litre', N'L', 10),
    ('b5a765ac-011c-c34a-8994-378e71b61102', N'Set', N'set', 11),
    ('00bd4869-2bb7-93d6-9dc9-5590babf5299', N'Pair', N'pair', 12),
    ('ff9a61c7-c9ad-c598-1ade-b250841b37f5', N'Square foot', N'sq ft', 13),
    ('4e7a84e9-fe3f-0c48-107a-872f86ce6ed9', N'Square metre', N'sq m', 14)
) AS source (Id, Name, Symbol, SortOrder)
ON target.Id = source.Id
WHEN MATCHED AND (target.Name <> source.Name OR target.Symbol <> source.Symbol OR target.SortOrder <> source.SortOrder) THEN
    UPDATE SET Name = source.Name, Symbol = source.Symbol, SortOrder = source.SortOrder, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, Symbol, SortOrder) VALUES (source.Id, source.Name, source.Symbol, source.SortOrder);
GO

/* ===================== Products ===================== */

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id           uniqueidentifier NOT NULL,
        SellerUserId uniqueidentifier NOT NULL,
        Title        nvarchar(120)    NOT NULL,
        Description  nvarchar(2000)   NULL,
        CategoryId   uniqueidentifier NOT NULL,
        PriceMin     decimal(12,2)    NOT NULL,
        PriceMax     decimal(12,2)    NULL,
        UnitId       uniqueidentifier NOT NULL,
        MinOrderQty  decimal(12,2)    NOT NULL,
        StateId      uniqueidentifier NOT NULL,
        CityId       uniqueidentifier NOT NULL,
        Keywords     nvarchar(500)    NULL,
        Status       varchar(20)      NOT NULL,
        IsDeleted    bit              NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT 0,
        DeletedAt    datetime2        NULL,
        CreatedAt    datetime2        NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt    datetime2        NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Products PRIMARY KEY (Id),
        CONSTRAINT FK_Products_Users_SellerUserId FOREIGN KEY (SellerUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Products_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id),
        CONSTRAINT FK_Products_Units_UnitId FOREIGN KEY (UnitId) REFERENCES dbo.Units (Id),
        CONSTRAINT FK_Products_States_StateId FOREIGN KEY (StateId) REFERENCES dbo.States (Id),
        CONSTRAINT FK_Products_Cities_CityId FOREIGN KEY (CityId) REFERENCES dbo.Cities (Id),
        CONSTRAINT CK_Products_PriceRange CHECK (PriceMin > 0 AND (PriceMax IS NULL OR PriceMax >= PriceMin)),
        CONSTRAINT CK_Products_MinOrderQty CHECK (MinOrderQty > 0),
        CONSTRAINT CK_Products_Status CHECK (Status IN ('Active', 'Paused', 'Removed'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId_Status' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_CategoryId_Status ON dbo.Products (CategoryId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_StateId_CityId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_StateId_CityId ON dbo.Products (StateId, CityId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_SellerUserId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_SellerUserId ON dbo.Products (SellerUserId);
GO

/* ===================== ProductImages ===================== */

-- No soft delete: removing an image deletes the row and its file. Max 3 per product (enforced by the API).
IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductImages (
        Id          uniqueidentifier NOT NULL,
        ProductId   uniqueidentifier NOT NULL,
        StoragePath nvarchar(260)    NOT NULL,
        ContentType varchar(50)      NOT NULL,
        SortOrder   int              NOT NULL CONSTRAINT DF_ProductImages_SortOrder DEFAULT 0,
        CreatedAt   datetime2        NOT NULL CONSTRAINT DF_ProductImages_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt   datetime2        NOT NULL CONSTRAINT DF_ProductImages_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ProductImages PRIMARY KEY (Id),
        CONSTRAINT FK_ProductImages_Products_ProductId FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductImages_ProductId_SortOrder' AND object_id = OBJECT_ID(N'dbo.ProductImages'))
    CREATE INDEX IX_ProductImages_ProductId_SortOrder ON dbo.ProductImages (ProductId, SortOrder);
GO

/* ===================== Requirements ===================== */

IF OBJECT_ID(N'dbo.Requirements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Requirements (
        Id          uniqueidentifier NOT NULL,
        BuyerUserId uniqueidentifier NOT NULL,
        Title       nvarchar(120)    NOT NULL,
        Description nvarchar(2000)   NULL,
        CategoryId  uniqueidentifier NOT NULL,
        BudgetMin   decimal(12,2)    NULL,
        BudgetMax   decimal(12,2)    NULL,
        Quantity    decimal(12,2)    NOT NULL,
        UnitId      uniqueidentifier NOT NULL,
        StateId     uniqueidentifier NOT NULL,
        CityId      uniqueidentifier NOT NULL,
        Keywords    nvarchar(500)    NULL,
        NeededBy    date             NULL,
        Status      varchar(20)      NOT NULL,
        ExpiresAt   datetime2        NOT NULL,
        IsDeleted   bit              NOT NULL CONSTRAINT DF_Requirements_IsDeleted DEFAULT 0,
        DeletedAt   datetime2        NULL,
        CreatedAt   datetime2        NOT NULL CONSTRAINT DF_Requirements_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt   datetime2        NOT NULL CONSTRAINT DF_Requirements_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Requirements PRIMARY KEY (Id),
        CONSTRAINT FK_Requirements_Users_BuyerUserId FOREIGN KEY (BuyerUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Requirements_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id),
        CONSTRAINT FK_Requirements_Units_UnitId FOREIGN KEY (UnitId) REFERENCES dbo.Units (Id),
        CONSTRAINT FK_Requirements_States_StateId FOREIGN KEY (StateId) REFERENCES dbo.States (Id),
        CONSTRAINT FK_Requirements_Cities_CityId FOREIGN KEY (CityId) REFERENCES dbo.Cities (Id),
        CONSTRAINT CK_Requirements_BudgetRange CHECK (BudgetMin IS NULL OR BudgetMax IS NULL OR BudgetMax >= BudgetMin),
        CONSTRAINT CK_Requirements_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_Requirements_Status CHECK (Status IN ('Active', 'Paused', 'Fulfilled', 'Expired', 'Removed'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requirements_CategoryId_Status' AND object_id = OBJECT_ID(N'dbo.Requirements'))
    CREATE INDEX IX_Requirements_CategoryId_Status ON dbo.Requirements (CategoryId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requirements_StateId_CityId' AND object_id = OBJECT_ID(N'dbo.Requirements'))
    CREATE INDEX IX_Requirements_StateId_CityId ON dbo.Requirements (StateId, CityId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requirements_BuyerUserId' AND object_id = OBJECT_ID(N'dbo.Requirements'))
    CREATE INDEX IX_Requirements_BuyerUserId ON dbo.Requirements (BuyerUserId);
GO

-- Serves the hourly expiry job.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requirements_Status_ExpiresAt' AND object_id = OBJECT_ID(N'dbo.Requirements'))
    CREATE INDEX IX_Requirements_Status_ExpiresAt ON dbo.Requirements (Status, ExpiresAt);
GO

PRINT N'04_ProductsAndRequirements.sql completed.';
GO
