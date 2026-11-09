/*
    JodVyapar - 01_CreateTables.sql
    Creates every table, key, constraint, default and index used by the API.

    - Run against the JodVyapar database (see Database/README.md).
    - Idempotent: objects that already exist are skipped, so it is safe to re-run.
    - Must match the EF Core mapping in JodVyapar.api (the *EntityConfigurations.cs files under Features).
      Never change a table here; add a new numbered script (03_..., 04_...) instead.

    Conventions: PK_<Table>, FK_<Child>_<Parent>_<Column>, IX_<Table>_<Columns> (lookup),
    UX_<Table>_<Columns> (unique), DF_<Table>_<Column> (default). All datetimes are UTC.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON; -- required for the filtered unique indexes (sqlcmd defaults it to OFF)
SET XACT_ABORT ON;
GO

/* ===================== Identity ===================== */

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        Id               uniqueidentifier NOT NULL,
        Name             nvarchar(256)    NULL,
        NormalizedName   nvarchar(256)    NULL,
        ConcurrencyStamp nvarchar(max)    NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Roles_NormalizedName' AND object_id = OBJECT_ID(N'dbo.Roles'))
    CREATE UNIQUE INDEX UX_Roles_NormalizedName ON dbo.Roles (NormalizedName) WHERE NormalizedName IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id                   uniqueidentifier NOT NULL,
        FullName             nvarchar(100)    NOT NULL,
        ConsentAcceptedAt    datetime2        NOT NULL,
        CreatedAt            datetime2        NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt            datetime2        NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME(),
        IsDeleted            bit              NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0,
        DeletedAt            datetime2        NULL,
        UserName             nvarchar(256)    NULL,
        NormalizedUserName   nvarchar(256)    NULL,
        Email                nvarchar(256)    NULL,
        NormalizedEmail      nvarchar(256)    NULL,
        EmailConfirmed       bit              NOT NULL CONSTRAINT DF_Users_EmailConfirmed DEFAULT 0,
        PasswordHash         nvarchar(max)    NULL,
        SecurityStamp        nvarchar(max)    NULL,
        ConcurrencyStamp     nvarchar(max)    NULL,
        PhoneNumber          nvarchar(max)    NULL,
        PhoneNumberConfirmed bit              NOT NULL CONSTRAINT DF_Users_PhoneNumberConfirmed DEFAULT 0,
        TwoFactorEnabled     bit              NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT 0,
        LockoutEnd           datetimeoffset   NULL,
        LockoutEnabled       bit              NOT NULL CONSTRAINT DF_Users_LockoutEnabled DEFAULT 1,
        AccessFailedCount    int              NOT NULL CONSTRAINT DF_Users_AccessFailedCount DEFAULT 0,
        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_NormalizedUserName' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX UX_Users_NormalizedUserName ON dbo.Users (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_NormalizedEmail' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_NormalizedEmail ON dbo.Users (NormalizedEmail);
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles (
        UserId uniqueidentifier NOT NULL,
        RoleId uniqueidentifier NOT NULL,
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserRoles_RoleId' AND object_id = OBJECT_ID(N'dbo.UserRoles'))
    CREATE INDEX IX_UserRoles_RoleId ON dbo.UserRoles (RoleId);
GO

IF OBJECT_ID(N'dbo.UserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserClaims (
        Id         int IDENTITY(1, 1) NOT NULL,
        UserId     uniqueidentifier   NOT NULL,
        ClaimType  nvarchar(max)      NULL,
        ClaimValue nvarchar(max)      NULL,
        CONSTRAINT PK_UserClaims PRIMARY KEY (Id),
        CONSTRAINT FK_UserClaims_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserClaims_UserId' AND object_id = OBJECT_ID(N'dbo.UserClaims'))
    CREATE INDEX IX_UserClaims_UserId ON dbo.UserClaims (UserId);
GO

IF OBJECT_ID(N'dbo.RoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleClaims (
        Id         int IDENTITY(1, 1) NOT NULL,
        RoleId     uniqueidentifier   NOT NULL,
        ClaimType  nvarchar(max)      NULL,
        ClaimValue nvarchar(max)      NULL,
        CONSTRAINT PK_RoleClaims PRIMARY KEY (Id),
        CONSTRAINT FK_RoleClaims_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RoleClaims_RoleId' AND object_id = OBJECT_ID(N'dbo.RoleClaims'))
    CREATE INDEX IX_RoleClaims_RoleId ON dbo.RoleClaims (RoleId);
GO

IF OBJECT_ID(N'dbo.UserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserLogins (
        LoginProvider       nvarchar(128)    NOT NULL,
        ProviderKey         nvarchar(128)    NOT NULL,
        ProviderDisplayName nvarchar(max)    NULL,
        UserId              uniqueidentifier NOT NULL,
        CONSTRAINT PK_UserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_UserLogins_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserLogins_UserId' AND object_id = OBJECT_ID(N'dbo.UserLogins'))
    CREATE INDEX IX_UserLogins_UserId ON dbo.UserLogins (UserId);
GO

IF OBJECT_ID(N'dbo.UserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserTokens (
        UserId        uniqueidentifier NOT NULL,
        LoginProvider nvarchar(128)    NOT NULL,
        Name          nvarchar(128)    NOT NULL,
        Value         nvarchar(max)    NULL,
        CONSTRAINT PK_UserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_UserTokens_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );
END;
GO

/* ===================== Reference data ===================== */

IF OBJECT_ID(N'dbo.States', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.States (
        Id               uniqueidentifier NOT NULL,
        Name             nvarchar(100)    NOT NULL,
        Code             varchar(2)       NOT NULL,
        IsUnionTerritory bit              NOT NULL CONSTRAINT DF_States_IsUnionTerritory DEFAULT 0,
        CreatedAt        datetime2        NOT NULL CONSTRAINT DF_States_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt        datetime2        NOT NULL CONSTRAINT DF_States_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_States PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_States_Name' AND object_id = OBJECT_ID(N'dbo.States'))
    CREATE UNIQUE INDEX UX_States_Name ON dbo.States (Name);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_States_Code' AND object_id = OBJECT_ID(N'dbo.States'))
    CREATE UNIQUE INDEX UX_States_Code ON dbo.States (Code);
GO

IF OBJECT_ID(N'dbo.Cities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cities (
        Id        uniqueidentifier NOT NULL,
        StateId   uniqueidentifier NOT NULL,
        Name      nvarchar(100)    NOT NULL,
        CreatedAt datetime2        NOT NULL CONSTRAINT DF_Cities_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2        NOT NULL CONSTRAINT DF_Cities_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Cities PRIMARY KEY (Id),
        CONSTRAINT FK_Cities_States_StateId FOREIGN KEY (StateId) REFERENCES dbo.States (Id)
    );
END;
GO

-- Also serves "cities in a state" lookups (StateId is the leading column).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Cities_StateId_Name' AND object_id = OBJECT_ID(N'dbo.Cities'))
    CREATE UNIQUE INDEX UX_Cities_StateId_Name ON dbo.Cities (StateId, Name);
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        Id        uniqueidentifier NOT NULL,
        ParentId  uniqueidentifier NULL,
        Name      nvarchar(100)    NOT NULL,
        Slug      varchar(120)     NOT NULL,
        SortOrder int              NOT NULL CONSTRAINT DF_Categories_SortOrder DEFAULT 0,
        IsActive  bit              NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1,
        CreatedAt datetime2        NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2        NOT NULL CONSTRAINT DF_Categories_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Categories PRIMARY KEY (Id),
        CONSTRAINT FK_Categories_Categories_ParentId FOREIGN KEY (ParentId) REFERENCES dbo.Categories (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_ParentId' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE INDEX IX_Categories_ParentId ON dbo.Categories (ParentId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Categories_Slug' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE UNIQUE INDEX UX_Categories_Slug ON dbo.Categories (Slug);
GO

/* ===================== Profiles & codes ===================== */

IF OBJECT_ID(N'dbo.CompanyProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyProfiles (
        Id               uniqueidentifier NOT NULL,
        UserId           uniqueidentifier NOT NULL,
        CompanyName      nvarchar(150)    NOT NULL,
        StateId          uniqueidentifier NOT NULL,
        CityId           uniqueidentifier NOT NULL,
        Mobile           varchar(15)      NOT NULL,
        Gstin            varchar(15)      NULL,
        IsMobileVerified bit              NOT NULL CONSTRAINT DF_CompanyProfiles_IsMobileVerified DEFAULT 0,
        IsGstinVerified  bit              NOT NULL CONSTRAINT DF_CompanyProfiles_IsGstinVerified DEFAULT 0,
        IsDeleted        bit              NOT NULL CONSTRAINT DF_CompanyProfiles_IsDeleted DEFAULT 0,
        DeletedAt        datetime2        NULL,
        CreatedAt        datetime2        NOT NULL CONSTRAINT DF_CompanyProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt        datetime2        NOT NULL CONSTRAINT DF_CompanyProfiles_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_CompanyProfiles PRIMARY KEY (Id),
        CONSTRAINT FK_CompanyProfiles_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_CompanyProfiles_States_StateId FOREIGN KEY (StateId) REFERENCES dbo.States (Id),
        CONSTRAINT FK_CompanyProfiles_Cities_CityId FOREIGN KEY (CityId) REFERENCES dbo.Cities (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CompanyProfiles_UserId' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
    CREATE UNIQUE INDEX UX_CompanyProfiles_UserId ON dbo.CompanyProfiles (UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_StateId' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
    CREATE INDEX IX_CompanyProfiles_StateId ON dbo.CompanyProfiles (StateId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_CityId' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
    CREATE INDEX IX_CompanyProfiles_CityId ON dbo.CompanyProfiles (CityId);
GO

-- One-time email codes. Purpose holds the OtpPurpose enum name: EmailVerification, Login, PasswordReset.
IF OBJECT_ID(N'dbo.EmailOtps', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailOtps (
        Id           uniqueidentifier NOT NULL,
        UserId       uniqueidentifier NOT NULL,
        Purpose      varchar(30)      NOT NULL,
        CodeHash     varbinary(32)    NOT NULL,
        ExpiresAt    datetime2        NOT NULL,
        AttemptCount int              NOT NULL CONSTRAINT DF_EmailOtps_AttemptCount DEFAULT 0,
        ConsumedAt   datetime2        NULL,
        CreatedAt    datetime2        NOT NULL CONSTRAINT DF_EmailOtps_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_EmailOtps PRIMARY KEY (Id),
        CONSTRAINT FK_EmailOtps_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );
END;
GO

-- Serves the "latest active code", resend cooldown and hourly cap lookups.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailOtps_UserId_Purpose_CreatedAt' AND object_id = OBJECT_ID(N'dbo.EmailOtps'))
    CREATE INDEX IX_EmailOtps_UserId_Purpose_CreatedAt ON dbo.EmailOtps (UserId, Purpose, CreatedAt);
GO

PRINT N'01_CreateTables.sql completed.';
GO
