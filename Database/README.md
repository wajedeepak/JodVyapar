# JodVyapar database scripts

The schema is managed with plain SQL scripts. EF Core is used only for data access and **never**
creates or changes tables (no migrations, no `EnsureCreated`).

| Script | What it does | Re-runnable |
|---|---|---|
| `01_CreateTables.sql` | All tables, keys, foreign keys, unique constraints, defaults and indexes | Yes (`IF NOT EXISTS`) |
| `02_SeedData.sql` | Roles (Buyer, Seller, Admin), all states/UTs, major cities, starter category tree | Yes (`MERGE` on fixed GUIDs) |
| `03_ProfileAboutAndActivityEvents.sql` | Adds `CompanyProfiles.About` (nvarchar(500), nullable) and the `ActivityEvents` log table with its indexes | Yes (`COL_LENGTH` / `IF NOT EXISTS`) |
| `99_DropAll.sql` | **Dev only.** Drops every JodVyapar table for a clean reset | Yes |

The admin user is not in these scripts. The API creates it at startup from `AdminSeed:Email` /
`AdminSeed:Password` (user-secrets), once the tables exist.

## Run order

New database: `01` → `02` → `03`. Then start the API.

Existing database: run only the scripts you haven't run yet, in order (for example just `03`).

Reset a local database: `99` → `01` → `02` → `03`.

## How to run

The examples use the local database from the API's connection string (`localhost`, `JodvyaparDB`).
Create the database once if it doesn't exist: `CREATE DATABASE JodvyaparDB;`

**SSMS / Azure Data Studio:** connect, select the `JodvyaparDB` database, open each script in order
and execute (F5).

**sqlcmd** (from the repo root):

```powershell
sqlcmd -S localhost -E -C -d JodvyaparDB -b -i Database/01_CreateTables.sql
sqlcmd -S localhost -E -C -d JodvyaparDB -b -i Database/02_SeedData.sql
sqlcmd -S localhost -E -C -d JodvyaparDB -b -i Database/03_ProfileAboutAndActivityEvents.sql

# Clean reset (dev only): drop everything, then run 01, 02 and 03 again
sqlcmd -S localhost -E -C -d JodvyaparDB -b -i Database/99_DropAll.sql
```

`-E` uses Windows authentication (use `-U <user> -P <password>` for SQL logins), `-C` trusts the
server certificate, and `-b` stops on the first error.

## Rule for schema changes

**Every future schema change gets a new numbered script**: `04_<Description>.sql`,
`05_<Description>.sql`, and so on. Never edit a script that has already been run on a shared
database.

- Make new scripts idempotent too (`IF COL_LENGTH(...) IS NULL`, `IF NOT EXISTS (...)`).
- Update the EF Core mapping (`Features/*/*EntityConfigurations.cs`) in the same change so the
  two match exactly: table, column, type, length, nullability.
- Add any new tables to `99_DropAll.sql` (children before parents).
- The API tests build their database from every numbered script in this folder, in order, so a
  script that doesn't match the mapping fails the test run.
