USE user_jodyvyapar;
GO

/*
    JodVyapar - 03_ProfileAboutAndActivityEvents.sql
    Adds the profile "About" text and the ActivityEvents log.

    - Run after 01_CreateTables.sql and 02_SeedData.sql (see Database/README.md).
    - Idempotent: objects that already exist are skipped, so it is safe to re-run.
    - Must match the EF Core mapping (CompanyProfileConfiguration, ActivityEventConfiguration).
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/* ===================== CompanyProfiles.About ===================== */

IF COL_LENGTH(N'dbo.CompanyProfiles', N'About') IS NULL
    ALTER TABLE dbo.CompanyProfiles ADD About nvarchar(500) NULL;
GO

/* ===================== ActivityEvents ===================== */

-- Append-only log of user actions (e.g. ProfileViewed). No UpdatedAt or soft delete: rows never change.
-- ActorUserId is nullable so system or anonymous events can be logged later.
IF OBJECT_ID(N'dbo.ActivityEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ActivityEvents (
        Id          uniqueidentifier NOT NULL,
        EventType   varchar(50)      NOT NULL,
        ActorUserId uniqueidentifier NULL,
        TargetType  varchar(50)      NOT NULL,
        TargetId    uniqueidentifier NOT NULL,
        CreatedAt   datetime2        NOT NULL CONSTRAINT DF_ActivityEvents_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ActivityEvents PRIMARY KEY (Id),
        CONSTRAINT FK_ActivityEvents_Users_ActorUserId FOREIGN KEY (ActorUserId) REFERENCES dbo.Users (Id)
    );
END;
GO

-- Serves "events of a type in a time range" reports.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityEvents_EventType_CreatedAt' AND object_id = OBJECT_ID(N'dbo.ActivityEvents'))
    CREATE INDEX IX_ActivityEvents_EventType_CreatedAt ON dbo.ActivityEvents (EventType, CreatedAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityEvents_ActorUserId' AND object_id = OBJECT_ID(N'dbo.ActivityEvents'))
    CREATE INDEX IX_ActivityEvents_ActorUserId ON dbo.ActivityEvents (ActorUserId);
GO

PRINT N'03_ProfileAboutAndActivityEvents.sql completed.';
GO
