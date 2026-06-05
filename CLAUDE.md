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
- Used by: `Dashboard/Admin`, `Dashboard/Company`, `Settings/Index`, `User/List`, `Request/*`
- Loads: Inter font, iconify-icons, node-waves, pickr-themes, core.css, demo.css, perfect-scrollbar.css, site.css, VendorStyles/PageStyles, then head scripts (helpers.js, **no template-customizer**, config.js)
- Body scripts: jquery, popper, bootstrap, node-waves, @algolia/autocomplete-js, pickr, perfect-scrollbar, hammer, i18n, menu.js, site.js, VendorScripts, **main.js**, PageScripts
- Renders: vertical menu → body → `_FooterHome` (**navbar is intentionally removed**)
- `<html>` has class `layout-menu-fixed layout-navbar-hidden`; `.layout-page` has `padding-top: 0px !important`
- Template Customizer is intentionally **not loaded** on admin pages
- Mobile menu toggle is provided by the `<div class="menu-mobile-toggler d-xl-none rounded-1">` block at the bottom of `_VerticalMenu.cshtml` (template's built-in pattern for `layout-navbar-hidden`)
- Renders `_TermsModal` partial at the end of body; when `Context.Items["ShowTCModal"]` is set, opens it in locked mode automatically on page load

### `_BlankLayout` — authentication pages
- Location: `Views/Shared/_BlankLayout.cshtml`
- Used by: all `Account/` views
- Loads: Inter font, iconify-icons, node-waves, core.css, demo.css, site.css, VendorStyles/PageStyles, head scripts (helpers.js, config.js, **no template-customizer**)
- Body scripts: jquery, popper, bootstrap, node-waves, site.js, VendorScripts, **main.js**, PageScripts
- Renders: body only — no navbar, sidebar, or footer
- html element has `customizer-hide` class

## Routing

Default route: `{controller=Landing}/{action=Index}` → public landing page at `/`

Admin pages:
- `/Dashboard` or `/Dashboard/Index` → Dashboard (role-dispatched: Admin / Company / Customer view)
- `/Home` or `/Home/Index` → HIW check then redirects to `/Dashboard`
- `/Settings` or `/Settings/Index` → Settings
- `/User/List` → Users list
- `/Request/List` → Requests list (admin only)
- `/Request/View?number=` → Request detail view
- `/HowItWorks/Customer` → How It Works for customers
- `/HowItWorks/Company` → How It Works for companies

Account pages live under `/Account`:
- `/Account/Login`
- `/Account/Register`
- `/Account/ForgotPassword`
- `/Account/ResetPassword`
- `/Account/VerifyAccount`

## Partials

```
Views/
├── _Partials/
│   └── _Macros.cshtml                   # Materio SVG logo
├── Shared/
│   ├── _LandingLayout.cshtml
│   ├── _HomeLayout.cshtml
│   ├── _BlankLayout.cshtml
│   ├── _Partials/
│   │   └── _TermsModal.cshtml           # Shared T&C fullscreen modal; injects ISettingsRepository;
│   │                                    #   AcceptMode (ViewData) = shows accept footer;
│   │                                    #   LockedMode (ViewData) = no close button, backdrop=static
│   └── Sections/
│       ├── Menu/
│       │   └── _VerticalMenu.cshtml     # Dashboard; Admin: Users+Requests; Customer: New Request+HowItWorks; Company: HowItWorks
│       │                                #   reads Name/Initials/PictureUrl from claims; role checks via User.IsInRole()
│       ├── Navbar/
│       │   ├── _NavbarLanding.cshtml    # Public landing navbar
│       │   └── _NavbarHome.cshtml       # Admin navbar (theme switcher + notifications + user dropdown)
│       └── Footer/
│           ├── _FooterLanding.cshtml    # Public landing footer
│           └── _FooterHome.cshtml       # Admin footer; T&C link opens #termsModal (rendered by _HomeLayout)
```

## Controllers

- `XBaseController` — base controller; provides `GetClaim<T>`, `HasClaim`, `RefreshClaim` claim helpers; all app controllers inherit from it
- `LandingController` — public landing page; checks `User.Identity!.IsAuthenticated` to redirect logged-in users
- `HomeController` — `[Authorize]`; checks `AcquaintedHIW` claim and role, redirects to `HowItWorks` or `Dashboard`
- `DashboardController` — `[Authorize]`; `Index()` dispatches via `User.IsInRole()` to Admin/Company views or Request/List
- `HowItWorksController` — `[Authorize]`; `Customer()` `[Authorize(Roles=Customer)]`; `Company()` `[Authorize(Roles=Company)]`
- `UserController` — `[Authorize(Roles=Administrator)]` on List/LoadUsers/UpdateUserStatus; `[Authorize]` on all Profile actions; actions: `UpdateAvatar`, `UpdateTheme`, `UpdateProfile`, `UpdatePassword`, `AcceptHIW`, `AcceptTerms`
- `AccountController` — public; Login (issues cookie via `SignInAsync`), Logout (`SignOutAsync`), Register, ForgotPassword, ResetPassword, VerifyAccount, VerifyResend
- `SettingsController` — `[Authorize(Roles=Administrator)]`; Index, SaveTermAndConditionSettings (Quill HTML content), SaveRequestSettings
- `FileController` — public; `GET /File/Download?key=`
- `RequestController` — `[Authorize]`; Create, Edit, View, List; Create/Edit gate on `GetClaim<string>(Role) == Customer`
- `ChatController` — `[Authorize]`; Visibility, Initiate, Conversation, Cancel

## Account Views

All auth views live in `Views/Account/` and use `Layout = "_BlankLayout"`.
They share the same visual shell: `authentication-wrapper authentication-basic`, centered card with logo, decoration images.

- `Login.cshtml` — email/password form; on success redirects to HowItWorks if `!AcquaintedHIW && Role != Administrator`, otherwise to Home
- `Register.cshtml` — 3-step bs-stepper (max-width: 740px); steps: **Role → General → Account**
  - Step 2 (General): **Customer** sees only Name + Phone; **Company** sees Name + Phone + UID (disabled) + Address
  - Step 3: `agreeTerms` checkbox intercepts clicks — opens `_TermsModal` in AcceptMode; user must scroll to bottom to unlock "I Accept"; accepting checks the checkbox; closing without accepting leaves it unchecked; if no T&C content configured, checkbox works normally
  - Services/interests posted as `string[]? Interests` (checkbox `name="Interests"`)
- `ForgotPassword.cshtml` — single email field; always shows success — never reveals whether email exists
- `ResetPassword.cshtml` — token-validated password reset form; token passed via query string
- `VerifyAccount.cshtml` — two 6-digit OTP inputs (email code + mobile code); driven by `pages-auth-two-steps.js`; both codes sent to `POST /Account/VerifyAccount`; Resend sends to `POST /Account/VerifyResend`

## Settings Page

`Views/Settings/Index.cshtml` — two independent cards, each with its own `<form>` and Save button. Admin only.

**Terms & Conditions card** (`POST SaveTermAndConditionSettings`):
- Full Quill editor (full toolbar from template's `forms-editors.js`) with KaTeX support
- Hidden input `#termsContent` receives `quill.root.innerHTML` on submit
- Saving updates `TermsAndConditionsContent` + sets `TermsAndConditionsContentDate = DateTime.Now`
- Changing content forces all non-admin users to re-accept on their next visit

**Request card** (`POST SaveRequestSettings`):
- Three visual groups: Negotiation Minutes / Image settings / Video settings
- All inputs are `type="number"`, `col-auto` with fixed `width: 200px`, centered via `justify-content-center`
- Controller rejects any value `<= 0`

## Landing Page Sections

`Views/Landing/Index.cshtml` — do NOT add `data-bs-spy="scroll"` to the wrapper div (causes nav items to falsely activate on load).

Sections: `id="banner"` hero, `id="services"` four service cards, `id="hiw"` how it works (has `style="isolation: isolate;"`), `id="faq"` FAQ accordion.

Navbar links (`_NavbarLanding.cshtml`): Home, Services (`#services`), How it works (`#hiw`), FAQ (`#faq`), Login/Register → `/Account/Login`.

## Vertical Menu

Menu items are hardcoded in `_VerticalMenu.cshtml`. Active state determined by comparing `ViewContext.HttpContext.Request.Path`.

**User badge** — reads `ClaimTypes.Name` for display name, computes initials from name, reads `"AvatarUrl"` claim for avatar. Both `menuBadgeImg` / `menuBadgeInitials` elements always rendered, one hidden via `display:none`.

**Role visibility** — all `@if` blocks use `User.IsInRole(UserRoleEnum.*)` (no session reads).

**Logout** — hidden form `<form id="menuLogoutForm">` at bottom of `<aside>`.

## Menu Behavior

`enableMenuLocalStorage: false` in `wwwroot/js/config.js` — menu always starts expanded.

## Front-Page CSS Load Order (important)

`front-page.css` loaded **after** VendorStyles/PageStyles in `_LandingLayout` — do not change this order.

---

## Backend Architecture

### C# Coding Conventions

- **Async/await throughout** — no sync equivalents, no `Async` suffix on method names
- **Entities = pure DB objects** — no business logic, no methods
- **Repositories = CRUD only** — no business logic, no service calls
- **String constant classes instead of C# enums** — values are lowercase strings stored directly in the DB
- **Always use `{}` for all control flow blocks** — even single-line `if`, `foreach`, `for`, `while`, `using` bodies
- **Never pass explicit `StringComparison`** — rely on the default (`Ordinal`, case-sensitive)

### Access Control

All controllers use standard ASP.NET Core attributes — no custom filter attributes:

| Attribute | Check | On fail |
|-----------|-------|---------|
| `[Authorize]` | Valid auth cookie | Redirect → `Account/Login` |
| `[Authorize(Roles = "administrator")]` | Authenticated + role claim = "administrator" | Redirect → `Home/Index` |

`LoginPath = "/Account/Login"`, `AccessDeniedPath = "/Home/Index"` configured in `Program.cs`.

### XBaseController Pattern

All app controllers inherit from `XBaseController` (which itself inherits `Controller`). It provides:

```csharp
// Read a claim value, converted to T; returns default if missing or unparseable
protected T? GetClaim<T>(string type)

// Check if a claim equals a given value (uses value.ToString())
protected bool HasClaim(string type, object value)

// Re-issue the auth cookie with one claim updated; used after DB writes that affect user state
protected async Task RefreshClaim(string type, object value)
```

Common claim keys: `ClaimTypes.NameIdentifier` (user ID), `ClaimTypes.Role`, `ClaimTypes.Name`, `ClaimTypes.Email`, `"AvatarUrl"`, `"AcquaintedHIW"`, `"TermsAcceptDate"`, `"TermsAccepted"` (internal cache-bypass flag).

### Project Structure

```
Controllers/
├── XBaseController.cs           # Base: GetClaim<T>, HasClaim, RefreshClaim
├── LandingController.cs
├── HomeController.cs            # [Authorize]; HIW + role check → redirect
├── HowItWorksController.cs      # [Authorize]; Customer/Company [Authorize(Roles=...)]
├── DashboardController.cs       # [Authorize]; Index() dispatches via User.IsInRole()
├── AccountController.cs         # public; Login/Logout/Register/ForgotPassword/ResetPassword/VerifyAccount/VerifyResend
├── UserController.cs            # [Authorize(Roles=Admin)] on List/LoadUsers/UpdateUserStatus;
│                                #   [Authorize] on UpdateAvatar/UpdateTheme/UpdateProfile/UpdatePassword/AcceptHIW/AcceptTerms
├── SettingsController.cs        # [Authorize(Roles=Admin)]; Index, SaveTermAndConditionSettings, SaveRequestSettings
├── RequestController.cs         # [Authorize]; Create, Edit, View, List
├── ChatController.cs            # [Authorize]; Visibility, Initiate, Conversation, Cancel
└── FileController.cs            # public; Download
Hubs/
└── ChatHub.cs                   # SignalR hub; reads userId from Context.User claims;
                                 #   JoinNotifications, JoinChat, SendMessage, MarkRead
Data/
├── Base/
│   ├── IEntity.cs               # Marker interface
│   ├── IRepository.cs           # Marker interface
│   ├── IRepositorySeedable.cs   # Adds Task Seed()
│   └── BaseFilter.cs            # Id<T>, Ids<List<T>>, SortField, SortDirection, Start, Length
├── Entities/
│   ├── UserEntity.cs            # Users table
│   ├── FileEntity.cs            # Files table (metadata only)
│   ├── SettingsEntity.cs        # Settings table (single row, Id = 1)
│   ├── ReferenceEntity.cs       # References table
│   ├── RequestEntity.cs         # Requests table
│   └── RequestFileEntity.cs     # RequestFiles table
├── Filters/
│   ├── UserFilter.cs            # Extends BaseFilter<long?>, adds Email, Search, Role, Status
│   ├── FileFilter.cs            # Extends BaseFilter<long?>, adds Key
│   └── RequestFilter.cs         # Extends BaseFilter<long?>, adds Search, Status, Service, Viewer*
├── Repositories/
│   ├── Abstractions/
│   │   ├── IUserRepository.cs
│   │   ├── IReferenceRepository.cs
│   │   ├── IFileRepository.cs
│   │   ├── ISettingsRepository.cs
│   │   ├── IRequestRepository.cs
│   │   ├── IRequestFileRepository.cs
│   │   └── IChatRepository.cs
│   ├── UserRepository.cs        # EF Core + IRepositorySeedable; Update() switches on UserUpdateAreaEnum
│   ├── ReferenceRepository.cs   # EF Core + IRepositorySeedable
│   ├── FileRepository.cs        # EF Core
│   ├── SettingsRepository.cs    # EF Core + IRepositorySeedable
│   ├── RequestRepository.cs     # EF Core; private ApplyFilters
│   ├── RequestFileRepository.cs # EF Core
│   └── ChatRepository.cs        # EF Core; ChatEntity + ChatMessageEntity
└── SqlContext.cs                # DbContext; DateOnly↔DateTime and TimeOnly↔TimeSpan converters
Enums/
├── UserRoleEnum.cs              # "administrator", "customer", "company"
├── UserStatusEnum.cs            # "active", "pending", "blocked", "unverified"
├── UserUpdateAreaEnum.cs        # Status, Password, Theme, Profile, Avatar, AcceptHIW, AcceptTerms
├── ChatStatusEnum.cs            # "active", "cancelled", "resolved"
├── ServiceEnum.cs               # "moving", "removal", "pickup", "transport"
├── FileTypeEnum.cs              # MIME type constants: PDF, PNG, JPEG, MP4, MOV
├── SortFieldEnum.cs             # "status"
├── SortDirectionEnum.cs         # "asc", "desc"
├── ReferenceTypeEnum.cs         # "user-role", "user-status"
├── EmailStatusEnum.cs           # "sent", "failed"
├── SmsStatusEnum.cs             # "sent", "failed"
├── RequestStatusEnum.cs         # "pending", "negotiation", "resolved", "cancelled"
├── RequestFileTypeEnum.cs       # "image", "video"
├── ConstantEnum.cs              # UserCacheTimeout=60, ResetPasswordTimeout=10, VerificationTimeout=10
└── AnnotationEnum.cs            # Nested string classes for user-facing messages
Models/
├── GridResultViewModel.cs       # Server-side DataTables response envelope
├── RequestViewModel.cs          # Create + Edit request form model
├── ChatHistoryViewModel.cs      # Model for _ChatHistory.cshtml partial
├── UserProfileModel.cs          # Profile page model: UserEntity User, Avatar, ServiceTermsFileName/Url
└── UserAvatarModel.cs           # Url, Initials, Name
ViewModels/
├── RegisterViewModel.cs         # Registration form; string[]? Interests (replaces per-service bools)
└── ProfileViewModel.cs          # Profile update form; Role (hidden), Name, Address, Interests, ServiceTermsFile, DeleteServiceTerms
Services/
├── FileService.cs               # Scoped — validate, upload, persist; GetUrl(fileId)
├── SettingService.cs            # Scoped — Get() only; wraps ISettingsRepository
└── UserService.cs               # Scoped — CRUD + GetAvatar + GetProfile + UpdateProfile + UpdateAvatar + LoadGrid
Storage/                         # Git-ignored; local file storage root
Tools/
├── PasswordTool.cs              # Static; HashPassword() → (hash, salt); Verify()
├── BrevoTool.cs                 # Static; Configure(IConfiguration); SendEmail() → EmailStatusEnum; SendSms() → SmsStatusEnum
│                                #   Config keys: Brevo:ApiKey, Brevo:FromEmail, Brevo:FromName, Brevo:SmsFrom
├── IFileStorageTool.cs          # Create(stream, fileName, mimeType) → key; Delete(key); GetUrl(key)
├── FileStorageTool.cs           # Singleton local-filesystem impl; GetUrl returns /File/Download?key=
├── UserIdentityTool.cs          # Static; BuildPrincipal(UserEntity, avatarUrl?) → ClaimsPrincipal
│                                #   Claims: NameIdentifier, Role, Name, Email, Theme,
│                                #           AvatarUrl, AcquaintedHIW, TermsAcceptDate
├── UserRefreshTool.cs           # Middleware; runs after UseAuthentication; uses "TermsAccepted" cookie claim
│                                #   as cache bypass flag; on cache miss: fetches user status + settings;
│                                #   sets HttpContext.Items["ShowTCModal"] if TermsAcceptDate < ContentDate
│                                #   (non-admin only); signs out blocked/missing users; TTL = UserCacheTimeout
└── CacheKeyTool.cs              # Static helper for building IMemoryCache keys
Views/
├── Dashboard/
│   ├── Admin.cshtml
│   └── Company.cshtml           # Period filter + 6 stat cards; Quill stars via Raty
├── HowItWorks/
│   ├── Customer.cshtml          # Sticky header with AcceptHIW button when !AcquaintedHIW; POST /User/AcceptHIW
│   └── Company.cshtml
├── Settings/
│   └── Index.cshtml             # T&C: full Quill editor; Request: number inputs
├── User/
│   ├── List.cshtml              # DataTable with real user avatars (URL or initials from server)
│   └── Profile.cshtml           # Picture/Theme/Personal/Password cards; uses ViewBag.Profile (UserProfileModel)
│                                #   Company: Name+Mobile(disabled)+UID(disabled)+Address+Interests+ServiceTerms
│                                #   Customer: Name+Mobile(disabled) only
└── Request/
    ├── Form.cshtml              # Create + Edit (isEdit = ViewBag.Request is not null)
    ├── View.cshtml              # Swiper gallery; floating chat tab; requester avatar
    └── List.cshtml              # DataTable; empty state for customers
Views/Shared/_Partials/
├── _Macros.cshtml               # Materio SVG logo
├── _TermsModal.cshtml           # Fullscreen Bootstrap modal; injects ISettingsRepository;
│                                #   ViewData["AcceptMode"]=true → accept footer with scroll-unlock;
│                                #   ViewData["LockedMode"]=true → no close button, backdrop=static;
│                                #   renders nothing if TermsAndConditionsContent is empty
└── _ChatHistory.cshtml          # Chat panel partial; @model ChatHistoryModel
wwwroot/js/
├── app-company-dashboard.js
├── app-user-list.js             # User DataTable; shows real avatars (img or initials from full['avatar'])
├── pages-auth-two-steps.js      # Two independent OTP wrappers (#emailOtpWrapper / #mobileOtpWrapper)
├── pages-auth-multisteps.js     # Register stepper; Step 2 fields toggled by role; Interests via name="Interests"
├── request-form.js
├── app-request-list.js
└── chat.js
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
| Number | string? | company identification number, max 16 |
| Address | string? | max 512 |
| Interests | string[] | comma-separated, max 128; values from `ServiceEnum` |
| ServiceTermsFileId | long? | FK to Files — company terms of service PDF |
| AvatarFileId | long? | FK to Files — profile picture |
| Theme | string | "light" or "dark"; default "light" |
| AcquaintedHIW | bool | whether user has seen the How It Works page |
| CreateDate | DateTime | UTC |
| TermsAndConditionsAcceptDate | DateTime | when user last accepted platform T&C; compared against `SettingsEntity.TermsAndConditionsContentDate` |

**FileEntity** (`Files` table):
| Field | Type | Notes |
|-------|------|-------|
| Id | long | PK, auto-increment |
| Key | string | unique storage key (GUID + extension), max 64 |
| FileName | string | original upload name, max 256 |
| MimeType | string | max 16 |
| Size | long | bytes |

**SettingsEntity** (`Settings` table — always exactly one row, Id = 1):
| Field | Type | Notes |
|-------|------|-------|
| Id | long | PK, `ValueGeneratedNever()` — always 1 |
| TermsAndConditionsContent | string | HTML content edited via Quill; empty string = no T&C configured |
| TermsAndConditionsContentDate | DateTime | updated to `DateTime.Now` whenever content is saved |
| RequestNegotiationMinutes | short | SMALLINT |
| RequestImageMaxCount | short | SMALLINT |
| RequestImageMaxSize | short | SMALLINT, in MB |
| RequestVideoMaxCount | short | SMALLINT |
| RequestVideoMaxSize | short | SMALLINT, in MB |

**ReferenceEntity**, **RequestEntity**, **RequestFileEntity**, **ChatEntity**, **ChatMessageEntity** — unchanged.

### Filters

Filters are criteria bags — only non-null/non-empty fields are applied. Always guard with `!string.IsNullOrWhiteSpace()`.

### Tools

**UserIdentityTool** (static):
```csharp
// Call at login; avatarUrl is resolved via FileService.GetUrl() before calling
ClaimsPrincipal principal = UserIdentityTool.BuildPrincipal(user, avatarUrl);
```

**UserRefreshTool** (middleware — registered after `UseAuthentication`):
- Per-user cache key: `CacheKeyTool.Get("bewegdeal_user", userId)` — TTL 60 min
- On cache miss: fetches user + settings; kicks blocked users; sets `HttpContext.Items["ShowTCModal"] = true` for non-admin when `TermsAcceptDate < TermsAndConditionsContentDate`
- `_HomeLayout` reads `Context.Items["ShowTCModal"]` to render locked T&C modal

**PasswordTool**, **BrevoTool**, **IFileStorageTool / FileStorageTool** — unchanged.

### Services

**FileService** (scoped):
```csharp
var (id, error) = await fileService.Create(formFile, replaceId: null, FileTypeEnum.PDF);
string? url = await fileService.GetUrl(fileId); // null if fileId is null or file not found
```

**SettingService** (scoped):
```csharp
SettingsEntity settings = await settingService.Get();
```

**UserService** (scoped):
```csharp
UserAvatarModel avatar = await userService.GetAvatar(user);      // from entity (no extra DB hit)
UserAvatarModel avatar = await userService.GetAvatar(userId);    // fetches entity first
UserAvatarModel avatar = userService.GetAvatar(user, fileEntity); // fully synchronous overload
UserProfileModel? profile = await userService.GetProfile(userId);
```

### Infrastructure (Program.cs)

- **Cookie auth**: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` — `LoginPath = "/Account/Login"`, `AccessDeniedPath = "/Home/Index"`, `ExpireTimeSpan = 8h`, `SlidingExpiration = true`
- **Remember-me**: `IsPersistent = true` + `ExpiresUtc = +30 days` in `AuthenticationProperties` passed to `SignInAsync`
- **No session** — sessions were fully removed; `IMemoryCache` is used by `UserRefreshTool`
- **Middleware order**: `UseRouting` → `UseAuthentication` → `UseMiddleware<UserRefreshTool>` → `UseAuthorization`
- **Database**: SQLite or MySQL via EF Core — `Database:Provider` in appsettings (`"sqlite"` or `"mysql"`)
- **Table prefix**: `Database:TablePrefix`
- **DI**: all repositories scoped, `IFileStorageTool` singleton, all services scoped
- **Startup**: `EnsureTablesAsync()` → seed Users → seed Settings

### Seed Data

Users (7 rows, all Status=Active):
- `admin@bewegdeal.at` / `datiko.admin@bewegdeal.at` / `gio.admin@bewegdeal.at` — Role=Administrator
- `datiko.customer@bewegdeal.at` / `gio.customer@bewegdeal.at` — Role=Customer
- `datiko.company@bewegdeal.at` / `gio.company@bewegdeal.at` — Role=Company

---

## T&C Feature

### Admin side (Settings page)
- Admin edits T&C HTML content via full Quill editor
- On save: `TermsAndConditionsContent` updated, `TermsAndConditionsContentDate = DateTime.Now`

### Re-acceptance enforcement
- `UserRefreshTool` middleware compares `TermsAcceptDate` claim with `TermsAndConditionsContentDate` from DB (cached 60 min)
- If `TermsAcceptDate < TermsAndConditionsContentDate` (and role ≠ Administrator): sets `HttpContext.Items["ShowTCModal"] = true`
- `_HomeLayout` renders `_TermsModal` in locked mode (`backdrop=static`, no close button) and auto-opens it via JS
- User cannot dismiss — must click "I Accept"
- `POST /User/AcceptTerms` → `UserUpdateAreaEnum.AcceptTerms` → `RefreshClaim("TermsAcceptDate", ...)` + `RefreshClaim("TermsAccepted", true)` → redirect to Home

### Registration T&C flow
- `agreeTerms` checkbox click intercept: opens `_TermsModal` in AcceptMode (with close button)
- User must scroll to bottom to unlock "I Accept"
- Accepting: closes modal, checks the checkbox
- Closing without accepting: checkbox stays unchecked
- If no T&C content configured: checkbox works normally, no modal

### _TermsModal.cshtml usage
The partial owns all scroll-unlock JS when `AcceptMode=true` — no page needs to duplicate it.

```razor
// Accept mode with close button (Register.cshtml — rendered inside @section PageScripts, after Bootstrap)
@{ ViewData["AcceptMode"] = true; }
@await Html.PartialAsync("~/Views/Shared/_Partials/_TermsModal.cshtml")
@{ ViewData.Remove("AcceptMode"); }

// Locked mode — no close, auto-opened (_HomeLayout when ShowTCModal)
@{ ViewData["AcceptMode"] = true; ViewData["LockedMode"] = true; }
@await Html.PartialAsync("~/Views/Shared/_Partials/_TermsModal.cshtml")
// _HomeLayout then: bootstrap.Modal.getOrCreateInstance(...).show();

// View-only mode (footer link) — _HomeLayout renders it without any ViewData
```

**Important:** in `_BlankLayout`, `@RenderBody()` runs **before** Bootstrap loads. The partial's `<script>` references `bootstrap`, so it must only be rendered inside `@section PageScripts` (which renders after Bootstrap) — never inline in the body.

---

## How It Works Feature

First-time onboarding for Customer/Company roles. Administrators are exempt.

### Flow
1. After login: if `!AcquaintedHIW claim && Role != Administrator` → redirect to `HowItWorks/Customer` or `HowItWorks/Company`
2. User reads the page; sticky header shows "I understand" button only when `ViewBag.ShowBar = !HasClaim("AcquaintedHIW", true)`
3. `POST /User/AcceptHIW` → DB write via `UserUpdateAreaEnum.AcceptHIW` → `RefreshClaim("AcquaintedHIW", true)` → redirect to Dashboard

---

## Request Feature

### Access guard
`RequestController` is `[Authorize]`. Create/Edit additionally gate on `GetClaim<string>(ClaimTypes.Role) == UserRoleEnum.Customer`.

### Role-based list visibility
`List` and `LoadRequests` use `ViewerRole`, `ViewerId`, `ViewerInterests`, `ViewerFocus` on `RequestFilter`. Repository `ApplyFilters` branches on role:
- **Customer** → `WHERE RequesterId == viewerId`
- **Company** → filtered by `ViewerFocus` (Mine / Potential / default=all); interest matching uses individual `bool` variables per service — **never** use `interests.Contains()` in LINQ (EF Core SQLite cannot translate it)
- **Administrator** → no extra filter

### View flow (`GET /Request/View?number=`)
- Loads request, files, requester avatar
- `ViewBag.Files` = ordered anonymous list (images first, main image first within images)

### Create/Edit flows — unchanged from original implementation

---

## Company Dashboard

### Controller
`DashboardController` inherits from `XBaseController`. `Index()` dispatches via `User.IsInRole()`. `CompanyStats([HttpGet])` returns JSON for the period filter.

### app-company-dashboard.js — Raty usage rule
Always initialise Raty with:
```js
new Raty(el, {
    starType: 'i',
    starOn:   'icon-base ri ri-star-fill text-warning',
    starHalf: 'icon-base ri ri-star-half-line text-warning',
    starOff:  'icon-base ri ri-star-line text-muted',
    score: value, half: true, readOnly: true
});
```
Never use image paths or data URLs for Raty stars — causes `getAttribute` crash.

---

## DataTables

All tables use `serverSide: true`. Key conventions:
- Page action: `List()` — loads ViewBag stats, returns view
- Data action: `[HttpGet] LoadXxx(...)` — returns `GridResultViewModel<object>`
- Mutation action: `[HttpPost] UpdateXxxStatus(long id, string status)` — posts current status alongside id; self-protection + stale-state check first
- Each table has its own JS file in `wwwroot/js/`
- Loading indicator: Notiflix `Block.pulse('.card-datatable')` — never use `processing: true`
- `scrollX: true`, `responsive: false` — Responsive extension conflicts with scrollX

---

## Chat Feature

### Business rules
- One active chat per request; Company initiates only
- `ChatHub` reads userId from `Context.User` claims (not session)
- Group name: `"chat-{chatKey}"`

### Two-phase loading
- Phase 1 `GET /Chat/Visibility?requestNumber=` — minimal check, returns `{ mode: "none"|"initiate"|"active" }`
- Phase 2 `GET /Chat/Conversation?requestNumber=` — full data, returns partial view

### SignalR
- `JoinNotifications`, `JoinChat(chatKey)`, `SendMessage(chatKey, content)`, `MarkRead(chatKey)`
- Client lib: `wwwroot/vendor/libs/signalr/signalr.min.js`
