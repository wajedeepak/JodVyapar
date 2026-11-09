/*
    JodVyapar - 99_DropAll.sql

    !!! DEVELOPMENT ONLY - DESTROYS ALL DATA !!!
    Drops every JodVyapar table so the numbered scripts (01, 02, 03, ...) can rebuild a clean
    database. Never run this against a shared, staging or production database.

    Tables are dropped children-first so foreign keys never block a drop. Also removes the old
    EF Core migration tables (AspNet* and __EFMigrationsHistory) if they are still around.
*/
SET NOCOUNT ON;
GO

DROP TABLE IF EXISTS dbo.ActivityEvents;
DROP TABLE IF EXISTS dbo.EmailOtps;
DROP TABLE IF EXISTS dbo.CompanyProfiles;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.Cities;
DROP TABLE IF EXISTS dbo.States;
DROP TABLE IF EXISTS dbo.UserTokens;
DROP TABLE IF EXISTS dbo.UserLogins;
DROP TABLE IF EXISTS dbo.UserClaims;
DROP TABLE IF EXISTS dbo.RoleClaims;
DROP TABLE IF EXISTS dbo.UserRoles;
DROP TABLE IF EXISTS dbo.Users;
DROP TABLE IF EXISTS dbo.Roles;

-- Leftovers from the EF Core migrations era (pre SQL scripts).
DROP TABLE IF EXISTS dbo.AspNetUserTokens;
DROP TABLE IF EXISTS dbo.AspNetUserLogins;
DROP TABLE IF EXISTS dbo.AspNetUserClaims;
DROP TABLE IF EXISTS dbo.AspNetRoleClaims;
DROP TABLE IF EXISTS dbo.AspNetUserRoles;
DROP TABLE IF EXISTS dbo.AspNetUsers;
DROP TABLE IF EXISTS dbo.AspNetRoles;
DROP TABLE IF EXISTS dbo.__EFMigrationsHistory;
GO

PRINT N'99_DropAll.sql completed: all JodVyapar tables dropped.';
GO
