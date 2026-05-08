# Bewegdeal

ASP.NET Core MVC web application targeting .NET 10.

## Template

All visual design comes from the purchased **Materio Bootstrap HTML ASP.NET Core MVC Admin Template v3.0.0**.
Template source: `C:\Software Templates\Materio\AspnetCoreMvcFull`

**Rule: never create custom CSS or JS files.** Every style and script must be imported from the template's wwwroot assets already copied into this project. Existing template JS files in `wwwroot/js/` may be modified to fit project needs, but ask permission first.

## Static Assets

The full template `wwwroot` (vendor libs, css, img, js, svg, json) has been copied to this project's `wwwroot/`. When a new template component needs an asset not yet present, copy only what is needed from the template source.

## Layout Architecture

Three layouts, each self-contained (no `_CommonMasterLayout` chain, no TempData dependencies):

### `_LandingLayout` — public landing page
- Location: `Views/Shared/_LandingLayout.cshtml`
- Used by: `Landing/Index`
- Loads: Inter font, iconify-icons, node-waves, core.css, demo.css, pickr-themes, site.css, then VendorStyles/PageStyles sections, then **front-page.css last** (so `first-section-pt` always wins over any `section-py` redefinition in page-specific sheets), then head scripts (helpers.js, template-customizer.js, front-config.js, dropdown-hover.js, mega-dropdown.js)
- Body scripts: popper, bootstrap, node-waves, pickr, site.js, VendorScripts, **front-main.js**, PageScripts
- Renders: `_NavbarLanding` → body → `_FooterLanding`

### `_HomeLayout` — admin/app pages
- Location: `Views/Shared/_HomeLayout.cshtml`
- Used by: `Home/Index`, `Home/Users`, `Home/Settings`
- Loads: Inter font, iconify-icons, node-waves, pickr-themes, core.css, demo.css, perfect-scrollbar.css, site.css, VendorStyles/PageStyles, then head scripts (helpers.js, **no template-customizer**, config.js)
- Body scripts: jquery, popper, bootstrap, node-waves, @algolia/autocomplete-js, pickr, perfect-scrollbar, hammer, i18n, menu.js, site.js, VendorScripts, **main.js**, PageScripts
- Renders: vertical menu → `_NavbarHome` → body → `_FooterHome`
- Template Customizer is intentionally **not loaded** on admin pages

### `_BlankLayout` — authentication pages
- Location: `Views/Shared/_BlankLayout.cshtml`
- Used by: all `Account/` views
- Loads: Inter font, iconify-icons, node-waves, core.css, demo.css, site.css, VendorStyles/PageStyles, head scripts (helpers.js, config.js, **no template-customizer**)
- Body scripts: jquery, popper, bootstrap, node-waves, site.js, VendorScripts, **main.js**, PageScripts
- Renders: body only — no navbar, sidebar, or footer
- html element has `customizer-hide` class

## Routing

Default route: `{controller=Landing}/{action=Index}` → public landing page at `/`

Admin pages live under `/Home`:
- `/Home` or `/Home/Index` → Dashboard
- `/Home/Users` → Users
- `/Home/Settings` → Settings

Account pages live under `/Account`:
- `/Account/Login`
- `/Account/Register`
- `/Account/ForgotPassword`
- `/Account/VerifyEmail`

## Partials

```
Views/
├── _Partials/
│   └── _Macros.cshtml                   # Materio SVG logo
├── Shared/
│   ├── _LandingLayout.cshtml
│   ├── _HomeLayout.cshtml
│   ├── _BlankLayout.cshtml
│   └── Sections/
│       ├── Menu/
│       │   └── _VerticalMenu.cshtml     # Dashboard, Users, Settings
│       ├── Navbar/
│       │   ├── _NavbarLanding.cshtml    # Public landing navbar
│       │   └── _NavbarHome.cshtml       # Admin navbar (theme switcher + notifications + user dropdown)
│       └── Footer/
│           ├── _FooterLanding.cshtml    # Public landing footer
│           └── _FooterHome.cshtml       # Admin footer
```

## Controllers

- `LandingController` — public landing page
- `HomeController` — admin/app pages (Dashboard, Users, Settings)
- `AccountController` — auth pages (Login, Register, ForgotPassword, VerifyEmail)

## Account Views

All four auth views live in `Views/Account/` and use `Layout = "_BlankLayout"`.
They share the same visual shell: `authentication-wrapper authentication-basic`, centered card with logo, tree decoration images.

- `Login.cshtml` — email/password form, links to ForgotPassword and Register
- `Register.cshtml` — 3-step bs-stepper (max-width: 740px); steps: **Role → General → Account**. Driven by `wwwroot/js/pages-auth-multisteps.js`
  - Step 1 `#roleSelectionValidation`: Customer/Company radio cards, no default selection, FormValidation `notEmpty` on `role`
  - Step 2 `#personalInfoValidation`: roleIndicator badge in header; fields: Name (required always), Phone (required always), IdentificationNumber + Address (required for Company only). Manual `is-invalid` pattern for phone/id/address — NOT in FormValidation
  - Step 3 `#accountDetailsValidation`: Email, agreeTerms checkbox (links to `/terms`), Password, ConfirmPassword, `#servicesSection` (Company only, d-none toggle, 2×2 grid: Moving/Junk/Pickup/Vehicle, at least one required), `#companyTermsUpload` (Company only, d-none toggle, PDF only, not mandatory)
- `ForgotPassword.cshtml` — single email field, back to login link
- `VerifyEmail.cshtml` — 6-digit OTP input, driven by pages-auth-two-steps.js

## Landing Page Sections

`Views/Landing/Index.cshtml` — do NOT add `data-bs-spy="scroll"` to the wrapper div (causes nav items to falsely activate on load).

Sections with IDs (navbar anchor targets):
- `id="banner"` — hero / header
- `id="services"` — four service cards
- `id="hiw"` — how it works (timeline, has `style="isolation: isolate;"` to prevent timeline icons overlapping the fixed navbar)
- `id="faq"` — FAQ accordion

Navbar links (`_NavbarLanding.cshtml`): Home (tag helper), Services (`#services`), How it works (`#hiw`), FAQ (`#faq`), Login/Register button → `/Account/Login`.

## Vertical Menu

Menu items are hardcoded in `_VerticalMenu.cshtml`. Active state is determined server-side by comparing `ViewContext.HttpContext.Request.Path`.

To add a menu item:
1. Add action to `HomeController` (or a new controller)
2. Create the view with `Layout = "_HomeLayout"`
3. Add `<li>` entry to `_VerticalMenu.cshtml` with the correct path check

## Menu Behavior

`enableMenuLocalStorage: false` in `wwwroot/js/config.js` — menu state is never persisted to localStorage, so the menu always starts **expanded**.

## Front-Page CSS Load Order (important)

`front-page-landing.css` redefines `.section-py` which would override `.first-section-pt` (the tall header padding). To prevent this, `front-page.css` is loaded **after** VendorStyles and PageStyles sections in `_LandingLayout`. Do not change this order.

## Adding a New Landing Page

1. Create controller action
2. Create view with `Layout = "_LandingLayout"`
3. Add required page-specific CSS/JS via `@section VendorStyles`, `@section PageStyles`, `@section VendorScripts`, `@section PageScripts`

## Adding a New Admin Page

1. Add action to an existing controller (or create a new one)
2. Create view with `Layout = "_HomeLayout"`
3. Add menu item to `_VerticalMenu.cshtml` if needed

## Adding a New Auth Page

1. Add action to `AccountController`
2. Create view in `Views/Account/` with `Layout = "_BlankLayout"`
3. Use `authentication-wrapper authentication-basic container-p-y` shell with card + tree images

---

## Backend Architecture

### C# Coding Conventions

- **Async/await throughout** — no sync equivalents, no `Async` suffix on method names
- **Entities = pure DB objects** — no business logic, no methods
- **Repositories = CRUD only** — no business logic, no service calls
- **String constant classes instead of C# enums** — values are lowercase strings stored directly in the DB
- **Always use `{}` for all control flow blocks** — even single-line `if`, `foreach`, `for`, `while`, `using` bodies
- **Never pass explicit `StringComparison`** — rely on the default (`Ordinal`, case-sensitive); only deviate when culture-aware comparison is genuinely needed

### Project Structure

```
Data/
├── Base/
│   ├── IEntity.cs               # Marker interface for all entities
│   ├── IRepository.cs           # Marker interface for all repositories
│   ├── IRepositorySeedable.cs   # Adds Task Seed() — implemented by repos that need initial data
│   └── BaseFilter.cs            # Generic filter with Id<T>
├── Entities/
│   ├── UserEntity.cs            # Users table
│   └── ReferenceEntity.cs       # References table (lookup/reference data)
├── Filters/
│   └── UserFilter.cs            # Extends BaseFilter<long?>, adds Email
├── Repositories/
│   ├── IUserRepository.cs       # Get(UserFilter), Create(UserEntity), Update(UserEntity)
│   ├── IReferenceRepository.cs  # Get(BaseFilter<string>), Create(ReferenceEntity), Update(ReferenceEntity)
│   ├── UserRepository.cs        # EF Core impl + IRepositorySeedable (2 admin users)
│   └── ReferenceRepository.cs  # EF Core impl + IRepositorySeedable (7 reference rows)
└── SqlContext.cs                # DbContext: Users + References DbSets, EF config, value converters
Enums/
├── UserRoleEnum.cs              # "administrator", "customer", "company"
├── UserStatusEnum.cs            # "active", "pending", "blocked", "unverified"
├── ServiceEnum.cs               # "moving", "removal", "pickup", "transport"
├── ReferenceTypeEnum.cs         # "user-role", "user-status" (note: UseRole field has a typo — should be UserRole)
└── EmailStatusEnum.cs           # "sent", "failed"
Tools/
├── PasswordTool.cs              # Static, PBKDF2/SHA-256, HashPassword() → (hash, salt), Verify()
└── BrevoTool.cs                 # Static, Configure(IConfiguration) at startup, Send() → EmailStatus string
```

### Entities

**UserEntity** (`Users` table):
| Field | Type | Notes |
|-------|------|-------|
| Id | long | PK, auto-increment |
| Role | string | `UserRoleEnum` value, max 16 |
| Status | string | `UserStatusEnum` value, max 16 |
| Name | string | max 128 |
| Email | string | unique index, max 128 |
| Mobile | string | max 16 |
| Password | string | PBKDF2 hash (Base64) |
| Salt | string | random salt (Base64) |
| Code | string? | verification/reset code, max 16 |
| Address | string? | max 512 |
| Interests | string[] | serialized as comma-separated string, max 128; values from `ServiceEnum` |

**ReferenceEntity** (`References` table):
| Field | Type | Notes |
|-------|------|-------|
| Id | string | PK, human-readable key (e.g. "customer"), max 16, never auto-generated |
| Type | string | `ReferenceTypeEnum` value, max 16 |
| Name | string | display name, max 16 |

### Filters

Filters are criteria bags — only non-null fields are applied in the query. Add new fields to extend lookup without adding new repository methods.

- `BaseFilter<T>` — has `Id`
- `UserFilter : BaseFilter<long?>` — adds `Email`
- `ReferenceRepository` uses `BaseFilter<string>` directly (no dedicated filter class)

### Tools

**PasswordTool** (static):
```csharp
var (hash, salt) = PasswordTool.HashPassword(plainText);
bool ok = PasswordTool.Verify(plainText, storedHash, storedSalt);
```

**BrevoTool** (static, configured once in `Program.cs`):
```csharp
// appsettings.json sections required: Brevo:ApiKey, Brevo:FromEmail, Brevo:FromName
string status = await BrevoTool.Send(email, subject, htmlContent, optionalText);
// returns EmailStatusEnum.Sent or EmailStatusEnum.Failed
```

### Infrastructure (Program.cs)

- **Session**: HttpOnly, IsEssential, 8hr idle timeout, SecurePolicy = SameAsRequest
- **Database**: SQLite or MySQL via EF Core — provider selected by `Database:Provider` in `appsettings.json` (`"sqlite"` or `"mysql"`). Invalid value throws at startup.
- **Table prefix**: configurable via `Database:TablePrefix` (e.g. `"dev_"`) — applied to all table names in `SqlContext`
- **Connection strings**: live inside the `Database` section (`Database:Sqlite`, `Database:MySql`)
- **DI**: `IUserRepository` → `UserRepository` (scoped), `IReferenceRepository` → `ReferenceRepository` (scoped)
- **Startup schema**: `SqlContext.EnsureTablesAsync()` — generates full DDL from the EF Core model and executes each statement with `IF NOT EXISTS`, safe on every run
- **Startup seeding**: seed References → seed Users (order matters — users depend on role values)
- **BrevoTool.Configure** called at startup before app runs

### Seed Data

References (7 rows): administrator/customer/company (type: user-role), active/pending/blocked/unverified (type: user-status)

Users (2 rows): `admin@bewegdeal.at` and `david.tabatadze@outlook.com`, both Role=Administrator, Status=Active, password hashed at seed time.
