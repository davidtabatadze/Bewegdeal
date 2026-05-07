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
- Used by: all `Authentication/` views
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

Authentication pages live under `/Authentication`:
- `/Authentication/Login`
- `/Authentication/Register`
- `/Authentication/ForgotPassword`
- `/Authentication/VerifyEmail`

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
- `AuthenticationController` — auth pages (Login, Register, ForgotPassword, VerifyEmail)

## Authentication Views

All four auth views live in `Views/Authentication/` and use `Layout = "_BlankLayout"`.
They share the same visual shell: `authentication-wrapper authentication-basic`, centered card with logo, tree decoration images.

- `Login.cshtml` — email/password form, links to ForgotPassword and Register
- `Register.cshtml` — 3-step bs-stepper inside the basic card shell (max-width: 740px); steps: Account → Personal → Role (Customer/Company). Driven by `wwwroot/js/pages-auth-multisteps.js` (modified: step 3 uses `#roleSelectionValidation` instead of `#billingLinksValidation`, no card field validation)
- `ForgotPassword.cshtml` — single email field, back to login link
- `VerifyEmail.cshtml` — 6-digit OTP input, driven by pages-auth-two-steps.js

## Landing Page Sections

`Views/Landing/Index.cshtml` — do NOT add `data-bs-spy="scroll"` to the wrapper div (causes nav items to falsely activate on load).

Sections with IDs (navbar anchor targets):
- `id="banner"` — hero / header
- `id="services"` — four service cards
- `id="hiw"` — how it works (timeline, has `style="isolation: isolate;"` to prevent timeline icons overlapping the fixed navbar)
- `id="faq"` — FAQ accordion

Navbar links (`_NavbarLanding.cshtml`): Home (tag helper), Services (`#services`), How it works (`#hiw`), FAQ (`#faq`), Login/Register button → `/Authentication/Login`.

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

1. Add action to `AuthenticationController`
2. Create view in `Views/Authentication/` with `Layout = "_BlankLayout"`
3. Use `authentication-wrapper authentication-basic container-p-y` shell with card + tree images
