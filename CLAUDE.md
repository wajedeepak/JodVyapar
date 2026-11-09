# Project: JodVyapar (India B2B matchmaking MVP)

## Context
JodVyapar is a two-sided B2B marketplace for India. Sellers list products, buyers post
requirements, a rule-based engine matches them, and users message each other directly.
Core journey: Search -> Match -> Profile -> Connect. It is an MVP for market validation,
so keep everything simple and avoid over-engineering.

Repo contains two existing empty projects:
- JodVyaparAPI: .NET 8 Web API, C#
- JodVyapar: Angular 21 app
Database: SQL Server (use EF Core). Please inspect both projects first and work within them.

## Rules (apply to all work)
- Modular monolith. One API project, organised by FEATURE folders (Identity, Reference,
  and later Profiles, Catalog, Requirements, Matching, Messaging, Admin), plus a shared
  Common/Infrastructure area. No microservices, no CQRS/MediatR, no generic repository
  over EF Core. Plain services + DbContext are fine.
- Never return EF entities from controllers; use DTOs.
- Use async everywhere, nullable reference types enabled, and validation on all input.
- Entities use Guid keys, CreatedAt/UpdatedAt audit columns, and soft delete where
  relevant (not needed for reference data).
- Angular: standalone components, lazy-loaded feature routes, signals for state,
  no NgRx. Mobile-first and responsive. Use Angular Material.
- Keep forms short and every button must work.
- Secrets (JWT key, connection string) must not be committed: use user-secrets or
  environment variables, with placeholders in appsettings.
- Database schema is managed by numbered SQL scripts in /Database (see Database/README.md),
  NOT by EF Core migrations. EF Core is for data access only: no migrations, no
  EnsureCreated/Migrate. Every schema change is a new idempotent script (03_..., 04_...)
  plus the matching EF mapping change; table names are plural PascalCase (Users, Roles, ...).
- Do NOT build anything beyond the scope below. No products, requirements, search,
  matching, messaging, or admin screens yet.

  ## UI conventions
- All new pages use the shared design tokens and components described in STYLEGUIDE.md.
- No hard-coded colors or sizes; use tokens.
- Every page needs loading, empty and error states, and must work from 360px wide.
- Reuse shared components (AuthLayout, PageHeader, PrimaryButton, FormField, AlertBanner) before creating new ones.
## Current progress
- Done: auth (register, verify email, password/OTP login, forgot/reset password), reference data
  (states, cities, categories), admin seeding.
- Done (API): dual roles, Profile API (`/api/v1/profile`: GET/PUT me, POST me/roles,
  GET {userId}), ActivityEvents + IActivityLogger. Schema up to `Database/03_...sql`.
- Done (Angular): app shell (`layout/app-shell`: top bar + bottom tabs below 768px), Home with
  mode-based action cards, mode switcher, register with Buyer/Seller/both, My Profile, Edit
  profile (unsaved-changes guard), Public Profile (`/profile/:userId`), 404 and 403 pages.
  Search, Messages, Admin and the Home card targets are "Coming soon" placeholders
  (`features/placeholders/coming-soon-page`, configured in `app.routes.ts`).
- Not started (out of scope until asked): Products, Requirements, Search, Matching, Messaging, Admin.

### Role and mode rules
- A user holds Buyer, Seller or both (rows in the Identity `UserRoles` table; there is no other
  roles table). Admin is separate and never self-assignable (not at registration, not via
  POST /profile/me/roles). Removing roles is out of scope.
- The JWT carries one `role` claim per held role. Adding a role returns a fresh token
  (`AuthResponse`) so the new role takes effect immediately; adding a held role is harmless.
- API authorization uses only the held roles: `[Authorize(Policy = AuthPolicies.RequireBuyer |
  RequireSeller | RequireAdmin)]`. Never read a "mode" from the client.
- "Active mode" (Buyer or Seller) is a UI preference only, stored in the browser. Users with both
  roles default to Buyer; a stored mode the user doesn't hold falls back safely; single-role users
  have no switcher and their mode is their role.
- Angular: use `AuthService.roles() / hasRole() / activeMode() / canSwitchMode() / setMode()` and
  `RoleActions` for "Add role" buttons; guard routes with `roleGuard(...)` (held roles, sends to
  /forbidden), never with the mode. Details in JodVyapar.web/STYLEGUIDE.md, "Roles and active mode".
- Adding a role requires a company profile (Admin-only accounts must fill in company details first).
- The public profile never exposes email, mobile or the GSTIN number, and never shows the Admin role.
