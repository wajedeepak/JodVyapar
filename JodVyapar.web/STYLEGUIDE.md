# JodVyapar style guide

Every page uses one shared design system. **Do not add colours, pixel sizes or one-off
styles in components.** Use the tokens and shared components below. If something is
missing, add it to the design system first.

| Where | What |
|---|---|
| `src/styles/_tokens.scss` | All design tokens (CSS custom properties) |
| `src/styles/_material.scss` | Angular Material theme mapped onto the tokens |
| `src/styles/_base.scss` | Reset, type scale, focus ring, reduced motion, `jv-` utilities |
| `src/styles/_breakpoints.scss` | Sass breakpoints and the `bp.up()` mixin (no CSS output) |
| `src/app/shared/ui/` | Shared standalone components (barrel: `shared/ui/index.ts`) |
| `src/app/layout/` | Page layouts: `AppShell` (signed in), `PublicLayout` (legal pages) |

Font: **Inter** (400/500/600/700). Icons: **Material Icons** (`<mat-icon>name</mat-icon>`).

## Tokens

Components use only the **semantic** tokens (`--jv-color-*` and the scales). The raw scales
(`--jv-blue-600`, `--jv-grey-200`, …) exist so the semantic layer can point at them. Dark
mode will re-point the semantic tokens under `[data-theme='dark']` (see the end of
`_tokens.scss`). Components won't change.

| Group | Tokens |
|---|---|
| Brand | `--jv-color-primary` (#1556c0), `-primary-hover`, `-primary-pressed`, `-primary-soft` (tinted bg), `-primary-border`, `-on-primary` |
| Surfaces | `--jv-color-bg` (page), `-surface` (cards/inputs), `-surface-muted` |
| Text | `--jv-color-text`, `-text-muted` (secondary, 6.6:1), `-text-subtle` (meta only, 4.7:1), `-text-inverse` |
| Borders | `--jv-color-border` (dividers, cards), `-border-strong`, `-border-input` (3:1, form controls) |
| Status | `--jv-color-{success,warning,error,info}` plus `-soft` (bg) and `-border` variants |
| Brand panel | `--jv-gradient-brand`, `--jv-color-on-brand-{tint,border,muted}` |
| Type | `--jv-font-sans`, `--jv-font-size-{xs,sm,md,lg,xl,2xl,3xl,4xl}` (12 to 36px), `--jv-font-weight-{regular,medium,semibold,bold}`, `--jv-line-height-{tight,snug,normal}`, `--jv-letter-spacing-{tight,wide}` |
| Spacing | `--jv-space-{0,1,2,3,4,5,6,8,10,12,16}` (4px base: `space-4` = 16px) |
| Shape | `--jv-radius-{sm,md,lg,xl,pill}` (inputs/buttons `md`, cards `lg`/`xl`), `--jv-border-width`, `--jv-border-width-strong` |
| Elevation | `--jv-shadow-{xs,sm,md,lg}` |
| Focus | `--jv-focus-outline`, `--jv-focus-outline-offset`: applied globally on `:focus-visible` |
| Loading | `--jv-color-skeleton`, `--jv-color-skeleton-shine` (used by `<app-skeleton>`) |
| Motion | `--jv-duration-{fast,base,slow}`, `--jv-ease`, `--jv-transition-colors`. These are 0 under `prefers-reduced-motion` |
| Sizes | `--jv-icon-size-{sm,md,lg}`, `--jv-control-height`, `--jv-content-width`, `--jv-prose-width`, `--jv-card-width(-wide)`, `--jv-brand-panel-width`, `--jv-top-bar-height`, `--jv-tab-bar-height`, `--jv-avatar-size-{sm,md,lg}` |

Headings `h1` to `h4` are styled globally. A page has one `h1`, which normally comes from `<app-page-header>`.

### Breakpoints (mobile-first)

```scss
@use 'breakpoints' as bp;   // resolved via stylePreprocessorOptions.includePaths

.thing {
  padding: var(--jv-space-4);
  @include bp.up(sm) { padding: var(--jv-space-6); }   // sm 600, nav 768, md 960, lg 1280
}
```

`nav` (768px) is where the app shell swaps the bottom tab bar for top-bar navigation.
Write the phone layout first, then widen it. Check every page at **360, 768 and 1280**.

### Utilities (`_base.scss`)

`jv-stack` (column, gap 16), `jv-stack-lg` (gap 32), `jv-grid-2` (1 column, 2 from `sm`),
`jv-field-error`, `jv-form-footer`, `jv-text-muted`, `jv-text-small`, `jv-sr-only`.

## Layouts

The root `App` is only a `<router-outlet>`. Each route group brings its own layout (see `app.routes.ts`):

| Routes | Layout |
|---|---|
| `/auth/*` | Each page wraps itself in `<app-auth-layout>` |
| `/legal/*` | `PublicLayout`: logo bar plus "Sign in" or "Back to JodVyapar" |
| Everything else | `AppShell` (behind `authGuard`) |

### `AppShell` (`layout/app-shell/`)
Wraps every signed-in page. Add a new signed-in page as a child of the shell route in
`app.routes.ts` (lazy `loadComponent`/`loadChildren`, plus a `title`). Never add a second toolbar.
- **Phones (below `nav`):** a slim top bar (logo, user menu) and a fixed bottom tab bar with
  icon + label, an active bar and `aria-current="page"`. It also adds safe-area padding
  (`viewport-fit=cover` is set in `index.html`). Page content gets bottom padding so nothing hides behind the tabs.
- **From `nav`:** the top bar holds the logo, the same nav items, `<app-mode-switcher>` and the user menu.
- **Nav items:** Home, Search, Messages and Profile, plus Admin for users who hold the Admin role.
- **User menu** (`UserMenu`): name, company, role badges, mode switcher (Buyer + Seller users),
  Profile, Admin and Sign out.
- A "Skip to main content" link appears on keyboard focus.

## Components

Import from `src/app/shared/ui` in lazy-loaded feature pages. In eagerly loaded code (the
`App` root) import the component's own file instead. The barrel would pull every shared
component into the initial bundle.

### `<app-auth-layout>`
Full-screen split layout: a brand panel and a form card. From `md` they sit side by side;
below that you get a compact brand header with the form underneath. Put the page content
(page header, form, footer line) in the slot. Inputs: `wide` (for longer forms). Only for the
`/auth/*` pages, which sit outside the app shell.

### `<app-page-header title subtitle>`
The page's `h1` and an optional subtitle. Project actions with `<div pageHeaderActions>…</div>`.

### `<app-primary-button>`
`label` (required), `loadingLabel`, `type` (`button` | `submit`), `loading`, `disabled`,
`fullWidth`. While `loading` the button is disabled, shows a spinner and sets `aria-busy`.
For `type="button"`, handle `(click)` on the element.

### `<app-text-field>`
The standard text input, bound to a reactive control:
`[control]` (required `FormControl<string>`), `label`, `type`, `autocomplete`, `inputmode`,
`maxlength`, `placeholder`, `prefix` (e.g. `+91`), `hint`, `[errorMessages]` (validator key
to message), `autofocus` (first field of a page only). Errors appear after blur or submit. They
are announced politely, and server errors set by `applyServerErrors` take priority.
For selects, use `<mat-form-field>` and `<mat-select>` directly. The theme styles them the same way.

### `<app-password-input>`
`[control]`, `label`, `autocomplete` (`current-password` | `new-password`), `[errorMessages]`,
`showRequirements` (live checklist and strength meter, for new passwords), `autofocus`. Includes
an accessible show/hide toggle.

### `<app-code-input>`
Entry for the 6-digit codes the API emails. `[control]`, `label` (default "6-digit code"), `hint`,
`[errorMessages]`, `autofocus`. A single numeric input with `autocomplete="one-time-code"`, so
paste and autofill work; non-digits are dropped. Give the control `Validators.required` and
`Validators.pattern(/^\d{6}$/)` (`CODE_LENGTH` is exported). Put `<app-resend-code>` under it.

### `<app-resend-code [secondsLeft] [sending] (resend)>`
The "Didn't get it? … Resend code" line under a code field. The button counts down while
`secondsLeft` > 0. Drive it with `resendCooldown()` from `core/auth/email-code.ts` (call
`.start()` each time a code is sent), and use `emailCodeErrorMessage(err)` from the same file
for the shared wrong-code and rate-limit messages. Used by Verify Email, Login (email code) and
Reset Password.

### `<app-alert-banner type title dismissible (dismissed)>`
Messages for a form or page: `error` | `warning` | `success` | `info`. Put the text in the slot.
Errors and warnings use `role="alert"`; success and info use `role="status"`.

### `<app-form-section title description>`
A `<fieldset>` with a legend, used to split a form into short groups ("About you", "Account").

### `<app-choice-cards formControlName [options] [multiple] aria-label>`
Large selectable cards. Each option is `{ value, title, description?, icon? }`.
- **Default (radio group):** the value is one option value. Tab moves into the group, arrow keys move and select, and Space/Enter select.
- **`[multiple]="true"` (checkbox group):** the value is an array in option order, such as
  `['Buyer', 'Seller']`. Every card is a tab stop and Space/Enter toggle. Use `Validators.required`
  to require at least one. Show the error in a `jv-field-error` paragraph and link it with `[aria-describedby]`.
  The Register page uses this for "I want to buy" / "I want to sell".

### `<app-state-city-fields [stateControl] [cityControl]>`
The dependent State and City selects (two columns from `sm`). They load states, and picking a
state clears the city and loads that state's cities. Each list shows loading, empty and
error-with-Retry hints. If the controls already have values (an edit form), the saved city is
kept. Server errors on `stateId` / `cityId` show under the fields. Used by Register and Edit profile.

### `<app-avatar [name] size>`
Round initials ("Asha Patel" gives AP). `size` is `sm` (top bar), `md` (default, menus) or `lg` (profile header).
Decorative (`aria-hidden`), so always show the name as text nearby.

### `<app-badge variant icon>`
A small pill. `variant` is `neutral` | `primary` | `success` | `warning` | `info`. Text goes in the slot.
Use `success` with `icon="verified"` for verified states, and `info` with `icon="schedule"` for "Coming soon".

### `<app-role-badges [roles]>`
One badge per role: Buyer (info, shopping bag), Seller (primary, storefront), Admin (warning).
Pass `auth.roles()` or a profile's `roles`, which are already in display order.

### `<app-empty-state icon title message headingLevel compact>`
The standard "nothing here" block, also used for not-found, 403 and "Coming soon" pages. Use
`[headingLevel]="1"` when it is the whole page, and `[compact]="true"` inside a card. Put buttons in
`<div emptyStateActions>` and an optional badge in `[emptyStateBadge]`. Always give the user a way
forward (for example "Back to home").

### `<app-skeleton shape width>`
Grey shimmer placeholder. `shape` is `text` | `title` | `circle` (avatar) | `block` (a card).
`width` is a max-width such as `'60%'`. Skeletons are `aria-hidden`, so put `aria-busy="true"` and a
`jv-sr-only` "Loading …" status on the container. The shimmer stops under reduced motion.

### `<app-mode-switcher [fullWidth]>`
The Buying / Selling segmented control. It renders **nothing** unless the user holds both Buyer and
Seller, so you can place it anywhere without a condition. It calls `auth.setMode()`. Used in the
desktop top bar, the user menu (`fullWidth`) and the Roles section of My Profile.

### `ConfirmDialog` and `confirm(dialog, data)`
A short yes/no question in a `MatDialog`: `confirm(this.dialog, { title, message, confirmLabel, cancelLabel? })`
returns `Observable<boolean>`. Cancel, Esc and the backdrop all mean no. Use it for "Add the Seller role?"
and "Discard your changes?".

### `<app-category-fields [parentControl] [categoryControl]>`
The dependent Category and Subcategory selects, laid out like `<app-state-city-fields>`. Changing the
category clears the subcategory; saved values are kept on an edit form. Server errors on `categoryId` show under it.

### `<app-unit-field [control]>`
The unit of measure select, with loading, empty and error-with-Retry hints.

### `<app-keyword-chips-field [control]>`
Keywords as chips, bound to a `FormControl<string[]>`. Enter, a comma or leaving the field adds one. Values are
normalized the same way as the API's `KeywordNormalizer`, up to 10.

### `<app-image-picker [(images)] [max]>`
Choose images by tap or drag and drop, with previews and remove. Checks type and size in the browser. It only
tracks the selection: items with `file` are new, items with `id` are already saved. The page uploads and deletes on save.

### `<app-listing-card title category facts description showImage imageUrl headingLevel>`
One product or requirement in a list. `facts` is a `ListingFact[]` (`productFacts()` / `requirementFacts()` in
`core/listings/listing-format.ts`). Put an `<app-status-badge listingBadge>` and a `<div listingActions>` in the slots.
Use `[headingLevel]="3"` when the list sits under an `h2`.

### `<app-status-badge [status]>`
A listing's status: Active (success), Paused (warning), Fulfilled (info), Expired (neutral).

### `<app-pager [page] [pageSize] [totalCount] (pageChange)>`
Previous, "Page 1 of 3" and Next under a list. Renders nothing when there is only one page.

### `<app-logo-mark size variant showWordmark>`
`size` is `sm` | `md` | `lg`. Use `variant="inverse"` on blue backgrounds.

## Roles and active mode

A user holds **Buyer, Seller or both** (Admin is separate and is never self-assignable). The
*active mode* only decides which tools the UI shows. **Permissions never depend on the mode.** The
API checks the roles in the JWT, and route guards check held roles.

Use the helpers on `AuthService` (`core/auth/auth.service.ts`). Never read localStorage or the token yourself:

| Helper | Use it for |
|---|---|
| `roles()` | Every role held, in display order (`['Buyer', 'Seller']`) |
| `hasRole(...roles)` | Show or hide things that need a role (the Admin nav item); true if any one is held |
| `activeMode()` | `'Buyer'`, `'Seller'` or `null` (Admin-only). Drives Home cards and other mode-specific UI |
| `canSwitchMode()` | True only for Buyer + Seller users (`<app-mode-switcher>` already checks it) |
| `setMode(mode)` | Switch mode. Ignored unless the role is held, and stored per user on this device (`jv.mode.<userId>`) |
| `addRole(role)` | POST `/profile/me/roles`. It replaces the session with the fresh token and switches to the new mode |

How the mode is chosen:
1. If the stored mode is a role the user holds, use it.
2. Otherwise use Buyer if they hold it. This is the default for Buyer + Seller users.
3. Otherwise use Seller. Admin-only accounts get `null`.

The function is `resolveActiveMode()` in `core/auth/active-mode.ts`.

- **Protecting a route:** use `canActivate: [roleGuard('Seller')]`. Without the role, the user lands
  on `/forbidden`. Never guard on `activeMode()`.
- **"Add Buyer/Seller role" buttons:** use `RoleActions` (`core/profile/role-actions.service.ts`).
  `missingRole()` returns the role to offer, `addRole(role)` handles the confirmation, the call and
  the snackbar, and `adding()` drives `[loading]`.

## Patterns

**Forms**
- Use `novalidate` with `(ngSubmit)`, so Enter submits the form.
- If the form is invalid, call `markAllAsTouched()` and then `focusFirstInvalid(host)` (from `core/forms/focus-invalid.ts`).
- On a server error, set the message with `applyServerErrors(form, err)`. It puts field errors on the matching controls; show whatever it returns in an `<app-alert-banner type="error">`.
- Disable the submit button with `[loading]` while the request runs.
- Messages are short and friendly: say what to do ("Please enter your email address."), never what the user did wrong.

**Loading, empty and error.** Use the same three states everywhere:
- **Loading:** a small `mat-spinner` with text ("Loading cities…"). For a page whose layout is known
  (profiles, forms), use `<app-skeleton>` shapes in that layout. For a button, use `[loading]`.
- **Empty:** a plain sentence saying what's missing ("No cities listed for this state yet."), never a blank area.
- **Error:** say what failed and give a **Retry** action. Inside a field, use a `mat-hint` in the error colour with a text button. For a page or form, use `<app-alert-banner type="error">`. Get the
  text with `errorMessage(err)` (`core/forms/form-errors.ts`).
- **Not found:** a full-page `<app-empty-state [headingLevel]="1">` with a way back.

Lookup lists use a `ListStatus` signal (`'idle' | 'loading' | 'ready' | 'empty' | 'error'`) to
drive these states. See `<app-state-city-fields>`.

**Listings.** A paged list extends `ListingListBase` (`core/listings/`): implement `fetch(page)` and use `status()`,
`items()`, `busyId()`, `runAction()`, `confirmAction()` and `confirmDelete()`. A listing form extends `ListingFormBase`,
which handles add vs edit by route `id`, the "More details" panel (it opens when a field inside has an error) and the
unsaved-changes warning. Lists and forms share `features/catalog/listing-pages.scss`.

**Unsaved changes.** An edit page implements `HasUnsavedChanges.confirmLeave()`, and its route sets
`canDeactivate: [unsavedChangesGuard]`. If the form is dirty, `confirmLeave()` asks with `confirm()`
("Discard your changes?"). The page also listens to `window:beforeunload`. See `EditProfilePage`.

**Success:** after an action that navigates away, show `MatSnackBar` with a short confirmation
(for example "Welcome to JodVyapar, Asha! Your account is ready.").

**Accessibility**
- Every input has a visible label.
- Don't remove the global `:focus-visible` ring.
- Icons next to text get `aria-hidden="true"`. Icon-only buttons need an `aria-label`.
- Links that open in a new tab say so to screen readers (`<span class="jv-sr-only"> (opens in a new tab)</span>`).
- Text must meet WCAG AA contrast: use `--jv-color-text-muted` or darker on white.

## Building a new page

```ts
@Component({
  imports: [ReactiveFormsModule, PageHeader, TextField, PrimaryButton, AlertBanner],
  template: `
    <app-page-header title="Add a product" subtitle="Buyers see this on your profile." />
    <form [formGroup]="form" (ngSubmit)="save()" novalidate class="jv-stack">
      @if (error(); as message) { <app-alert-banner type="error">{{ message }}</app-alert-banner> }
      <app-text-field [control]="form.controls.name" label="Product name" [autofocus]="true" />
      <app-primary-button type="submit" label="Save" [loading]="saving()" />
    </form>
  `,
})
```

Page styles should only arrange things, using flex or grid with token gaps. If you're about
to write a colour, shadow or radius in a component, use a token, or add one to `_tokens.scss`.
