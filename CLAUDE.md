# JodVyapar

## Context
JodVyapar is a two-sided B2B marketplace for India. Sellers list products, buyers post
requirements, a rule-based engine matches them, and users message each other directly.
Core journey: Search -> Match -> Profile -> Connect. It is an MVP for market validation,
so keep everything simple and avoid over-engineering. Anything that doesn't serve the core
journey waits.

## Repo layout
- `JodVyapar.api`: .NET 8 Web API (C#), EF Core, SQL Server
- `JodVyapar.web`: Angular 21 app (Angular Material)
- `Database/`: numbered SQL scripts and README
- `JodVyapar.web/STYLEGUIDE.md`: design tokens and shared components

## Commands
- API: `dotnet run --project JodVyapar.api`
- API tests: `dotnet test`
- Angular: `cd JodVyapar.web && ng serve`, build with `ng build`
- Database: run scripts in `Database/` in numeric order (see Database/README.md)

## Rules (apply to all work)
- Modular monolith: one API project organised by FEATURE folders (Identity, Reference, Profiles,
  Catalog, Requirements, and later Matching, Messaging, Admin) plus a shared Common/Infrastructure
  area. No microservices, CQRS/MediatR or generic repository. Plain services + DbContext.
- Controllers return DTOs, never EF entities. Routes are under `/api/v1`. Errors use ProblemDetails.
- Async everywhere, nullable reference types on, validation on all input.
- Entities use Guid keys, CreatedAt/UpdatedAt, and soft delete where relevant (not reference data).
- Database schema is managed by numbered SQL scripts in `/Database`, NOT by EF migrations. EF Core
  is for data access only: no migrations, no EnsureCreated/Migrate. Every schema change is a new
  idempotent script (next number after the latest) plus the matching EF mapping change. Seed data
  goes in the seed script. Table names are plural PascalCase (Users, Roles, ...).
- Secrets (JWT key, SMTP password, admin seed password, connection strings with passwords) come from
  user-secrets locally and environment variables on servers, never committed. Remember user-secrets
  override appsettings in Development.
- Auth codes (email verification, login OTP, password reset): 6 digits, hashed at rest, 10-minute
  expiry, single use, 5 attempts, resend cooldown and hourly caps. Anonymous endpoints never reveal
  whether an email exists. Never log codes.
- Angular: standalone components, lazy-loaded feature routes, signals for state, no NgRx.
  Mobile-first and responsive. Keep forms short and make every button work.
- Do not add dependencies beyond those already used without asking first.
- Do not build beyond the scope of the current task.

## UI conventions
- All pages use the shared design tokens and components in STYLEGUIDE.md.
- No hard-coded colours or sizes; use tokens.
- Every page needs loading, empty and error states, and must work from 360px wide.
- Reuse shared components (AuthLayout, PageHeader, PrimaryButton, FormField, AlertBanner, Avatar,
  Badge, EmptyState, ModeSwitcher) before creating new ones.

## Product decisions (settled)
- Login: email + password or email code. Email must be verified before a token is issued. Mobile is
  stored but not verified in V1.
- Contact privacy: public profiles never show email, mobile or GSTIN number, and never show the
  Admin role. Users connect through in-app messages.
- Admin accounts are assigned only through the database. Never self-assignable.
- Genuine connection = a conversation where both sides have sent at least one message.
- Categories are admin-managed, two levels. Listings must use a subcategory.
- A user must never be matched with, or messaged by, themselves.
- Matching: rule-based behind `IMatchingEngine`. Each rule is its own class returning a score and a
  reason. Starting weights: Category 40, Keywords 20, Location 15, Budget 15, Quantity 10 (in
  configuration). Labels: 85%+ "Best Match", 60-84% "Good Match". Score on demand; log shown and
  opened matches as ActivityEvents.
- Messaging: database-backed with polling, not SignalR. One conversation per buyer-seller pair,
  optionally linked to the product or requirement that started it.
- Product images: up to 3, behind `IFileStorage` (local disk now, Azure Blob later).
- Requirements expire after 60 days (renewable). Products stay active until paused.

## Role and mode rules
- A user holds Buyer, Seller or both (rows in Identity `UserRoles`; no other roles table). Admin is
  separate. Removing roles is out of scope.
- The JWT carries one `role` claim per held role. Adding a role returns a fresh token.
- API authorization uses held roles only: `[Authorize(Policy = AuthPolicies.RequireBuyer |
  RequireSeller | RequireAdmin)]`. Never read a "mode" from the client.
- "Active mode" (Buyer or Seller) is a browser-stored UI preference only. Users with both roles
  default to Buyer; an invalid stored mode falls back safely; single-role users have no switcher.
- Angular: use `AuthService.roles() / hasRole() / activeMode() / canSwitchMode() / setMode()` and
  `RoleActions`; guard routes with `roleGuard(...)` (held roles, redirects to /forbidden), never
  with the mode. Details in STYLEGUIDE.md, "Roles and active mode".
- Adding a role requires a company profile.

## Current progress
Done:
- Auth: register (Buyer/Seller/both), email verification, password and OTP login, forgot/reset password.
- Reference data (states, cities, categories, units), admin seeding.
- Dual roles, Profile API and screens, public profile (`/profile/:userId`), ActivityEvents + IActivityLogger.
- Products (up to 3 images) and Requirements (pause, fulfil, renew, hourly expiry job), public lists at
  `/api/v1/profile/{userId}/products|requirements`, shared `KeywordNormalizer`, images served at
  `/api/v1/media/product-images/{imageId}`.
- App shell (top bar, bottom tabs below 768px), Home with mode-based cards and counts, My products,
  My requirements, add/edit forms, 404 and 403 pages.
- Schema is at `Database/04_...sql`.

Placeholders ("Coming soon"): Search, Messages, Admin, "Browse buyer requirements", and the public
profile "Message" button.

Next, in order: Search and matching, Messaging, Dashboard with real data, Admin and metrics,
hardening and deployment.

## Working agreement
1. Inspect the code, then reply with a SHORT plan and wait for approval before writing code.
2. Build the API and SQL script first (build and tests passing), pause for review, then the Angular side.
3. Run `dotnet build`, `dotnet test` and `ng build` before finishing a step.
4. When done: update "Current progress", Database/README.md and STYLEGUIDE.md where relevant, and tell
   me which scripts to run and what to click to verify.

## Notes
- .NET 8 reaches end of support in November 2026. Plan the move to .NET 10 (LTS) before launch.