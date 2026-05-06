# Bewegdeal

ASP.NET Core MVC web application targeting .NET 10.

## Template

All visual design comes from the purchased **Materio Bootstrap HTML ASP.NET Core MVC Admin Template v3.0.0**.
Template source: `C:\Software Templates\Materio\AspnetCoreMvcFull`

**Rule: never create custom CSS or JS files.** Every style and script must be imported from the template's wwwroot assets already copied into this project.

## Static Assets

The full template `wwwroot` (vendor libs, css, img, js, svg, json) has been copied to this project's `wwwroot/`. When a new template component needs an asset not yet present, copy only what is needed from the template source.

## Layout Architecture

There are two layouts, each self-contained (no `_CommonMasterLayout` chain, no TempData dependencies):

### `_FrontLayout` — public-facing pages
- Location: `Views/Shared/_FrontLayout.cshtml`
- Used by: `Landing/Index`
- Loads: Inter font, iconify-icons, node-waves, core.css, demo.css, pickr-themes, site.css, then VendorStyles/PageStyles sections, then **front-page.css last** (so `first-section-pt` always wins over any `section-py` redefinition in page-specific sheets), then head scripts (helpers.js, template-customizer.js, front-config.js, dropdown-hover.js, mega-dropdown.js)
- Body scripts: popper, bootstrap, node-waves, pickr, site.js, VendorScripts, **front-main.js**, PageScripts
- Renders: `_NavbarFront` → body → `_FooterFront`

### `_AppLayout` — admin/app pages
- Location: `Views/Shared/_AppLayout.cshtml`
- Used by: `Home/Index`, `Home/Users`, `Home/Settings`
- Loads: Inter font, iconify-icons, node-waves, pickr-themes, core.css, demo.css, perfect-scrollbar.css, site.css, VendorStyles/PageStyles, then head scripts (helpers.js, **no template-customizer**, config.js)
- Body scripts: jquery, popper, bootstrap, node-waves, @algolia/autocomplete-js, pickr, perfect-scrollbar, hammer, i18n, menu.js, site.js, VendorScripts, **main.js**, PageScripts
- Renders: vertical menu → `_NavbarAdmin` → body → `_Footer`
- Template Customizer is intentionally **not loaded** on admin pages

## Routing

Default route: `{controller=Landing}/{action=Index}` → public landing page at `/`

Admin pages live under `/Home`:
- `/Home` or `/Home/Index` → Dashboard
- `/Home/Users` → Users
- `/Home/Settings` → Settings

## Partials

```
Views/
├── _Partials/
│   └── _Macros.cshtml           # Materio SVG logo
├── Shared/
│   ├── _FrontLayout.cshtml
│   ├── _AppLayout.cshtml
│   └── Sections/
│       ├── Menu/
│       │   └── _VerticalMenu.cshtml     # 3 items: Dashboard, Users, Settings
│       ├── Navbar/
│       │   ├── _NavbarFront.cshtml      # Public landing navbar
│       │   └── _NavbarAdmin.cshtml      # Admin navbar (no search, theme switcher + notifications + user dropdown)
│       └── Footer/
│           ├── _FooterFront.cshtml      # Public landing footer (newsletter, links, social)
│           └── _Footer.cshtml           # Admin footer (copyright + 5 placeholder links)
```

## Vertical Menu

Menu items are hardcoded in `_VerticalMenu.cshtml`. Active state is determined server-side by comparing `ViewContext.HttpContext.Request.Path`.

To add a menu item:
1. Add action to `HomeController` (or a new controller)
2. Create the view with `Layout = "_AppLayout"`
3. Add `<li>` entry to `_VerticalMenu.cshtml` with the correct path check

## Menu Behavior

`enableMenuLocalStorage: false` in `wwwroot/js/config.js` — menu state is never persisted to localStorage, so the menu always starts **expanded**.

## Front-Page CSS Load Order (important)

`front-page-landing.css` redefines `.section-py` which would override `.first-section-pt` (the tall header padding). To prevent this, `front-page.css` is loaded **after** VendorStyles and PageStyles sections in `_FrontLayout`. Do not change this order.

## Adding a New Front Page

1. Create controller action
2. Create view with `Layout = "_FrontLayout"`
3. Add required page-specific CSS/JS via `@section VendorStyles`, `@section PageStyles`, `@section VendorScripts`, `@section PageScripts`

## Adding a New Admin Page

1. Add action to an existing controller (or create a new one)
2. Create view with `Layout = "_AppLayout"`
3. Add menu item to `_VerticalMenu.cshtml` if needed
