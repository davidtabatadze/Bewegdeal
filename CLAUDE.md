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
- Used by: `Dashboard/Admin`, `Dashboard/Company`, `Dashboard/Customer`, `Settings/Index`, `User/List`
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

Admin pages:
- `/Dashboard` or `/Dashboard/Index` → Dashboard (role-dispatched: Admin / Company / Customer view)
- `/Home` or `/Home/Index` → redirects to `/Dashboard`
- `/Settings` or `/Settings/Index` → Settings
- `/User/List` → Users list

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
- `HomeController` — redirects `Index` → `Dashboard/Index`; kept for backwards-compatible `/Home` URL
- `DashboardController` — `Index()` dispatches to `Views/Dashboard/Admin`, `Company`, or `Customer` based on session role
- `UserController` — Users list (`List`), DataTables endpoint (`LoadUsers`), status toggle (`UpdateUserStatus`)
- `AccountController` — auth pages (Login, Register, ForgotPassword, VerifyEmail)
- `SettingsController` — Settings (`Index`), Terms upload (`SaveTermAndConditionSettings`), request config (`SaveRequestSettings`); **admin only**
- `FileController` — file download (`Download`); no auth required — files may be public
- `RequestController` — Create and Edit a request; `[RequireLogin]`; Customer + Active status only; both actions share `Views/Request/Form.cshtml`

## Account Views

All four auth views live in `Views/Account/` and use `Layout = "_BlankLayout"`.
They share the same visual shell: `authentication-wrapper authentication-basic`, centered card with logo, tree decoration images.

- `Login.cshtml` — email/password form, links to ForgotPassword and Register
- `Register.cshtml` — 3-step bs-stepper (max-width: 740px); steps: **Role → General → Account**. Driven by `wwwroot/js/pages-auth-multisteps.js`
  - Step 1 `#roleSelectionValidation`: Customer/Company radio cards, no default selection, FormValidation `notEmpty` on `role`
  - Step 2 `#personalInfoValidation`: roleIndicator badge in header; fields: Name (required always), Phone (required always), IdentificationNumber + Address (required for Company only). Manual `is-invalid` pattern for phone/id/address — NOT in FormValidation
  - Step 3 `#accountDetailsValidation`: Email, agreeTerms checkbox (T&C link is dynamic — loaded from Settings via `ViewBag.TermsFileKey`, opens `/File/Download/{key}` in a new tab; renders as plain `<span>` if no file is configured), Password, ConfirmPassword, `#servicesSection` (Company only, d-none toggle, 2×2 grid: Moving/Junk/Pickup/Vehicle, at least one required), `#companyTermsUpload` (Company only, d-none toggle, PDF only, not mandatory)
- `ForgotPassword.cshtml` — single email field, back to login link
- `VerifyEmail.cshtml` — 6-digit OTP input, driven by pages-auth-two-steps.js

## Settings Page

`Views/Settings/Index.cshtml` — two independent cards, each with its own `<form>` and Save button. Admin only.

**Terms & Conditions card** (`POST SaveTermAndConditionSettings`):
- Shows current file as a bold link (if one exists) → `<hr>` separator → file upload input + warning alert; all centered via `align-items-center`
- File upload: PDF only (`FileTypeEnum.PDF`); replaces the previous file via `fileService.Create(..., replaceId)`
- Success/error feedback via `TempData` (survives the redirect)

**Request card** (`POST SaveRequestSettings`):
- Three visual groups separated by `<hr class="my-6 mx-n4" />`: Negotiation Minutes / Image settings / Video settings
- All inputs are `type="number"`, `col-auto` with fixed `width: 200px`, centered via `justify-content-center`
- Controller rejects any value `<= 0` — all fields must be greater than zero

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

**Users** and **Settings** items are wrapped in `@if (Context.Session.GetString("UserRole") == UserRoleEnum.Administrator)` — they are invisible to non-admin users.

To add a menu item:
1. Add action to the appropriate controller (create a new dedicated controller if the feature is distinct)
2. Create the view with `Layout = "_HomeLayout"`
3. Add `<li>` entry to `_VerticalMenu.cshtml` with the correct path check (`ViewContext.HttpContext.Request.Path == "/Controller/Action"`)
4. Wrap in the admin `@if` block if the page is admin-only

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

### Access Control

Two action filter attributes in `Filters/`:

| Attribute | Check | On fail |
|-----------|-------|---------|
| `[RequireLogin]` | Session has `UserId` | Redirect → `Account/Login` |
| `[RequireAdmin]` | Logged in **and** `UserRole == "administrator"` | Not logged in → `Account/Login`; wrong role → `Home/Index` |

- `HomeController` — `[RequireLogin]` (any logged-in user)
- `UserController` — `[RequireAdmin]`
- `SettingsController` — `[RequireAdmin]`
- `RequestController` — `[RequireLogin]`; additionally validates `Role == Customer && Status == Active` inside each action
- `AccountController`, `FileController`, `LandingController` — no attribute (public)

### Project Structure

```
Data/
├── Base/
│   ├── IEntity.cs               # Marker interface for all entities
│   ├── IRepository.cs           # Marker interface for all repositories
│   ├── IRepositorySeedable.cs   # Adds Task Seed() — implemented by repos that need initial data
│   └── BaseFilter.cs            # Generic filter: Id<T>, Ids<List<T>>, SortField, SortDirection, Start, Length
├── Entities/
│   ├── UserEntity.cs            # Users table
│   ├── FileEntity.cs            # Files table (metadata only — bytes on disk)
│   ├── SettingsEntity.cs        # Settings table (single row, Id = 1, seeded at startup)
│   ├── ReferenceEntity.cs       # References table (lookup/reference data)
│   ├── RequestEntity.cs         # Requests table
│   └── RequestFileEntity.cs     # RequestFiles table — links a request to its uploaded files
├── Filters/
│   ├── UserFilter.cs            # Extends BaseFilter<long?>, adds Email, Search, Role, Status
│   └── FileFilter.cs            # Extends BaseFilter<long?>, adds Key
├── Repositories/
│   ├── Abstractions/
│   │   ├── IUserRepository.cs       # Get, Load, Count, Create, SetUserStatus, UpdatePassword
│   │   ├── IReferenceRepository.cs  # Get, Create, Update
│   │   ├── IFileRepository.cs       # Get(id), Load(BaseFilter<long>), Create, Delete(id)
│   │   ├── ISettingsRepository.cs   # Get(), Update(entity)
│   │   ├── IRequestRepository.cs    # Get(id), Create, Update
│   │   └── IRequestFileRepository.cs# Load(requestId), Create(List<>), SetMainImage, Delete(List<id>)
│   ├── UserRepository.cs        # EF Core impl + IRepositorySeedable (4 seed users)
│   │                            #   private ApplyFilters helper shared by Count and Load
│   ├── ReferenceRepository.cs   # EF Core impl + IRepositorySeedable (7 reference rows)
│   ├── FileRepository.cs        # EF Core impl; no seeding
│   ├── SettingsRepository.cs    # EF Core impl + IRepositorySeedable (1 row, Id = 1, default values seeded)
│   ├── RequestRepository.cs     # EF Core impl; no seeding
│   └── RequestFileRepository.cs # EF Core impl; no seeding
└── SqlContext.cs                # DbContext: all DbSets, EF config, value converters
                                 #   DateOnly↔DateTime and TimeOnly↔TimeSpan converters for SQLite compat
Enums/
├── UserRoleEnum.cs              # "administrator", "customer", "company"
├── UserStatusEnum.cs            # "active", "pending", "blocked", "unverified"
├── ServiceEnum.cs               # "moving", "removal", "pickup", "transport"
├── FileTypeEnum.cs              # MIME type constants: PDF, PNG, JPEG, MP4, MOV
├── SortFieldEnum.cs             # "status" (add new fields here as new sortable columns are added)
├── SortDirectionEnum.cs         # "asc", "desc"
├── ReferenceTypeEnum.cs         # "user-role", "user-status"
├── EmailStatusEnum.cs           # "sent", "failed"
├── RequestStatusEnum.cs         # "pending", "accepted", "completed", "cancelled"
├── RequestFileTypeEnum.cs       # "image", "video"
└── AnnotationEnum.cs            # Nested static string classes for user-facing messages (not DB values)
                                 #   AnnotationEnum.Account.Login.*, .Register.*, .ForgotPassword.*, etc.
                                 #   AnnotationEnum.Request.Requirement.*, .Media.*
                                 #   Uses string.Format("{0}", field) pattern for parameterised messages
Filters/
├── RequireLoginAttribute.cs     # Redirects to Login if no session UserId
└── RequireAdminAttribute.cs     # Redirects to Login if not logged in; to Home if not administrator
Models/
├── GridResultViewModel.cs       # Generic server-side DataTables response envelope
└── RequestViewModel.cs          # Create + Edit request form model (Id=0 on Create, KeepFileIds=[] on Create)
Services/
└── FileService.cs               # Scoped — validate MIME type, upload bytes, persist metadata, optionally delete old file
Storage/                         # Git-ignored. Local file storage root (configurable via Storage:Local:Path)
Tools/
├── PasswordTool.cs              # Static, PBKDF2/SHA-256, HashPassword() → (hash, salt), Verify()
├── BrevoTool.cs                 # Static, Configure(IConfiguration) at startup, Send() → EmailStatus string
├── IFileStorageTool.cs          # Create(stream, fileName, mimeType) → key; Delete(key); GetUrl(key)
└── FileStorageTool.cs           # Singleton local-filesystem impl; key = GUID + extension; base path from appsettings
Views/
└── Request/
    └── Form.cshtml              # Single shared view for both Create and Edit
                                 #   var req = ViewBag.Request as RequestEntity; var isEdit = req is not null
wwwroot/js/
└── request-form.js              # Dropzone (images + videos), flatpickr, jQuery Timepicker, inline validation,
                                 #   FormData submit via fetch; works for both Create and Edit
                                 #   Edit-only: loads existingFiles as Dropzone mock entries, tracks KeepFileIds/KeepMainFileId
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
| Interests | string[] | serialized as comma-separated string, max 128; values from `ServiceEnum` |
| TermsFileId | long? | FK to Files table — company terms of service PDF |

**FileEntity** (`Files` table):
| Field | Type | Notes |
|-------|------|-------|
| Id | long | PK, auto-increment |
| Key | string | unique storage key (GUID + extension), max 64 — used to locate bytes on disk |
| FileName | string | original upload name, max 256 |
| MimeType | string | e.g. `"application/pdf"`, max 16 |
| Size | long | file size in bytes |

**SettingsEntity** (`Settings` table — always exactly one row, Id = 1):
| Field | Type | Notes |
|-------|------|-------|
| Id | long | PK, `ValueGeneratedNever()` — always 1 |
| TermsAndConditionsFileId | long | FK to Files — platform T&C PDF; required, seeded to 1 |
| RequestNegotiationMinutes | short | SMALLINT |
| RequestImageMaxCount | short | SMALLINT |
| RequestImageMaxSize | short | SMALLINT, in MB |
| RequestVideoMaxCount | short | SMALLINT |
| RequestVideoMaxSize | short | SMALLINT, in MB |

**ReferenceEntity** (`References` table):
| Field | Type | Notes |
|-------|------|-------|
| Id | string | PK, human-readable key (e.g. "customer"), max 16, never auto-generated |
| Type | string | `ReferenceTypeEnum` value, max 16 |
| Name | string | display name, max 16 |

**RequestEntity** (`Requests` table):
| Field | Type | Notes |
|-------|------|-------|
| Id | long | PK, auto-increment |
| Code | Guid | unique, used for public-facing URLs |
| Status | string | `RequestStatusEnum` value, max 16 |
| Service | string | `ServiceEnum` value, max 16 |
| Title | string | max 128 |
| Description | string | max 2048 |
| SourceAddress | string | max 512 |
| DestinationAddress | string | max 512 |
| RequesterId | long | FK to Users |
| ExecutorId | long? | FK to Users |
| ProposedCost | decimal | precision 18,2 |
| ProposedCurrency | string | max 4, default "EUR" |
| ProposedASAP | bool | true = ASAP, false = use ProposedDate/ProposedTime |
| ProposedDate | DateOnly? | stored via DateTime converter for SQLite compat |
| ProposedTime | TimeOnly? | stored via TimeSpan converter for SQLite compat |
| AgreementDate | DateTime? | when negotiation was agreed |
| AgreedCost | decimal? | precision 18,2 |
| AgreedCurrency | string? | max 4 |
| AgreedDate | DateOnly? | stored via DateTime converter for SQLite compat |
| AgreedTime | TimeOnly? | stored via TimeSpan converter for SQLite compat |

**RequestFileEntity** (`RequestFiles` table):
| Field | Type | Notes |
|-------|------|-------|
| Id | long | PK, auto-increment |
| RequestId | long | FK to Requests |
| FileId | long | FK to Files |
| IsMain | bool | true for the primary display image; only one per request |
| Type | string | `RequestFileTypeEnum` value ("image" or "video"), max 8 |

### Filters

Filters are criteria bags — only non-null/non-empty fields are applied in the query. Add new fields to extend lookup without adding new repository methods. Always guard with `!string.IsNullOrWhiteSpace()`, never bare null checks.

- `BaseFilter<T>` — has `Id`, `Ids` (List<T>?), `SortField`, `SortDirection`, `Start` (int?), `Length` (int?) — Start/Length/Sort used by DataTables server-side mode
- `UserFilter : BaseFilter<long?>` — adds `Email`, `Search`, `Role`, `Status`
- `FileFilter : BaseFilter<long?>` — adds `Key`
- `ReferenceRepository` uses `BaseFilter<string>` directly (no dedicated filter class)
- `FileRepository.Load(BaseFilter<long>)` uses `Ids` to bulk-fetch by a list of IDs

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

**IFileStorageTool / FileStorageTool** (singleton — no DB dependency):
```csharp
// Stores bytes, returns a unique key (GUID + extension)
string key = await storageTool.Create(stream, fileName, mimeType);
// Deletes bytes from disk
await storageTool.Delete(key);
// Returns a download URL (/File/Download/{key})
string url = storageTool.GetUrl(key);
```
Configured via `Storage:Local:Path` in `appsettings.json`. Relative paths are resolved against `ContentRootPath`. The `Storage/` folder is git-ignored.

### Services

**FileService** (scoped — wraps `IFileStorageTool` + `IFileRepository`):
```csharp
// Upload a new file. Pass replaceId to delete an old file after the new one is saved.
// allowedMimeTypes: use FileTypeEnum constants. Pass none to skip MIME validation.
var (id, error) = await fileService.Create(formFile, replaceId: null, FileTypeEnum.PDF);
if (error is not null) { /* show error */ }
// id is the new FileEntity.Id
```
Use `FileService` anywhere a controller needs to handle file uploads — never duplicate the validate + upload + persist logic inline.

**FileController** (no auth — files may be public):
- `GET /File/Download/{key}` — streams the file; validates key against path traversal (`/`, `\`, `..`); resolves MIME type via `FileExtensionContentTypeProvider`; `enableRangeProcessing: true`

### Infrastructure (Program.cs)

- **Session**: HttpOnly, IsEssential, 8hr idle timeout, SecurePolicy = SameAsRequest
- **Database**: SQLite or MySQL via EF Core — provider selected by `Database:Provider` in `appsettings.json` (`"sqlite"` or `"mysql"`). Invalid value throws at startup.
- **Table prefix**: configurable via `Database:TablePrefix` (e.g. `"dev_"`) — applied to all table names in `SqlContext`
- **Connection strings**: live inside the `Database` section (`Database:Sqlite`, `Database:MySql`)
- **DI**: `IUserRepository` → `UserRepository` (scoped), `IReferenceRepository` → `ReferenceRepository` (scoped), `IFileRepository` → `FileRepository` (scoped), `ISettingsRepository` → `SettingsRepository` (scoped), `IFileStorageTool` → `FileStorageTool` (singleton), `FileService` (scoped)
- **Startup schema**: `SqlContext.EnsureTablesAsync()` — generates full DDL from the EF Core model and executes each statement with `IF NOT EXISTS`, safe on every run
- **Startup seeding**: References → Users → Settings (Settings has no inter-dependencies; order relative to Users/References does not matter)
- **BrevoTool.Configure** called at startup before app runs

### Seed Data

References (7 rows): administrator/customer/company (type: user-role), active/pending/blocked/unverified (type: user-status)

Users (4 rows, all Status=Active, passwords hashed at seed time):
- `admin@bewegdeal.at` — Role=Administrator
- `datiko.admin@bewegdeal.at` — Role=Administrator
- `datiko.customer@bewegdeal.at` — Role=Customer
- `datiko.company@bewegdeal.at` — Role=Company

---

## Request Feature

### Access guard
`RequestController` is `[RequireLogin]`. Each action additionally calls `ValidateUser()` which returns `null` (→ redirect to Dashboard) unless `Role == Customer && Status == Active`.

### Create flow (`GET /Request/Create` → `POST /Request/Create`)
1. GET: loads `ViewBag.Settings`, returns `Views/Request/Form.cshtml` (no `ViewBag.Request` → `isEdit = false`)
2. POST: `ValidateRequirement` → `ValidateMedia` (existingFiles = []) → create `RequestEntity` → set `model.Id = request.Id` → `UploadMedia`

### Edit flow (`GET /Request/Edit?id=` → `POST /Request/Edit`)
1. GET: validates request exists, `RequesterId == userId`, `Status == Pending`; loads existing files via `requestFileRepository.Load(id)` + `fileRepository.Load(BaseFilter<long> { Ids = ... })`; sets `ViewBag.Request` + `ViewBag.Files`; returns `Form.cshtml` (`isEdit = true`)
2. POST: same ownership/status guard → `ValidateRequirement` → load existingFiles → `ValidateMedia(existingFiles)` → update entity → `UploadMedia(existingFiles)`

### UploadMedia helper
- Deletes `RequestFileEntity` rows + storage for files not in `model.KeepFileIds`
- Uploads new images (PNG/JPEG) and videos (MP4/MOV) via `FileService`
- Inserts new `RequestFileEntity` rows
- Calls `requestFileRepository.SetMainImage(requestId, keepMainFileId)` — first resets all image rows to `IsMain = false`, then sets the target (falls back to first image by Id if target not found)
- Returns error string or null

### RequestViewModel
```csharp
public long          Id                 // 0 on Create
public string        Service            // ServiceEnum value
public string        Title
public string?       Description
public string        SourceAddress
public string        DestinationAddress
public decimal       ProposedCost       // 1–10000
public bool          IsASAP             // bound from radio value="true"/"false"
public string?       ProposedDate       // "yyyy-MM-dd", required if !IsASAP
public string?       ProposedTime       // "HH:mm", required if !IsASAP
public IFormFile[]?  Images
public IFormFile[]?  Videos
public int           MainImageIndex     // index into new Images array
public long[]        KeepFileIds        // existing file IDs to preserve ([] on Create)
public long          KeepMainFileId     // existing FileId that is main (0 = main is a new upload)
```

### Form.cshtml rendering logic
- `var req = ViewBag.Request as RequestEntity; var isEdit = req is not null;`
- Title/button text, `asp-action`, hidden `Id` field, input `value=`, radio `checked`, `scheduled-fields` class, and `existingFiles` JS const all conditioned on `isEdit`
- For `checked` on `isASAP` radios: `!isEdit || req!.ProposedASAP` → "ASAP" checked (covers both Create default and Edit restore)

### request-form.js key behaviours
- `Dropzone.autoDiscover = false` set before IIFE
- `existingFiles` defaults to `[]` via `typeof existingFiles !== 'undefined'` guard (Create has no inline const)
- New files: `addedfile` handler emits `uploadprogress(100%)` + `success` + `complete` to show progress bar and ✓ mark immediately (no actual POST through Dropzone)
- Existing mock files: same three events emitted during load loop
- `loadingExisting` flag prevents auto-setMainFile during mock-file population; server's `isMain` restored explicitly after loop
- Inline validation: Bootstrap `is-invalid` + `invalid-feedback` for all fields; Notyf only for server errors + one summary toast on failed client-side submit
- Cost input: blocks `- + e E` on keydown, clamps to 10000 on input, clamps to min 1 on blur, truncates to 2 decimal places

---

## DataTables

DataTables is used for all admin list views. The full reference and checklist is in `.claude/skills/datatables.md`. Key project conventions:

### Mode — always serverSide: true

All tables use `serverSide: true`. DataTables sends `draw`, `start`, `length`, and order params; the server returns `{ draw, recordsTotal, recordsFiltered, data }` via `GridResultViewModel<T>`.

### Controller naming convention

- Page action: e.g. `List()` — loads ViewBag stats, returns the view
- Data action: `[HttpGet] LoadXxx(...)` — returns `GridResultViewModel<object>`
- Mutation action: `[HttpPost] UpdateXxxStatus(long id)` — self-protection check first

### Response model

Use `Models/GridResultViewModel<T>` — never an anonymous `new { draw, recordsTotal, ... }`.

### Repository convention

Every DataTable repository method pair:
- `Count(filter)` — filtered count, no paging (used for `recordsFiltered`)
- `Load(filter)` — filtered + sorted + paged via `Skip`/`Take`
- Both share a private `ApplyFilters` helper to avoid duplicated filtering logic
- `Count(new Filter())` gives the unfiltered total (`recordsTotal`)
- EF Core `DbContext` is not thread-safe — these three calls must be awaited sequentially

### JS file location

Each DataTable has its own file in `wwwroot/js/`, e.g. `app-user-list.js`. Use that file as the template for new tables.

### Filters

Filters live in the `card-header` as plain HTML (search input + bootstrap-select dropdowns). They call `dt.ajax.reload(null, true)` on change (reset to page 1). Search uses 500ms debounce.

### Loading indicator

Use Notiflix `Block.pulse('.card-datatable')` on `preXhr.dt`, removed on `xhr.dt`. Never use DataTables' built-in `processing: true`.

### Mobile / responsive

Use `scrollX: true` and `responsive: false`. The Responsive extension conflicts with `scrollX` and must always be disabled.

### Materio layout tweaks

Always apply the `setTimeout` class-adjustment block after initialization (see `app-user-list.js`).
