# DataTables Reference Skill

When the user asks to add a DataTable, build a table component, wire table to an API, or filter/search table data — apply this reference in full.

---

## What DataTables Is

DataTables is a jQuery plug-in that enhances any `<table>` with sorting, filtering, pagination, Ajax loading, export, and responsive collapsing. In this project it is loaded via `datatables-bs5/datatables-bootstrap5.js` — a single bundle that includes the base library plus the Responsive, Buttons, and Select extensions.

---

## Initialization

```javascript
// Modern constructor (preferred — returns API instance directly)
const table = new DataTable('#myTable', options);

// jQuery style (also fine — returns API instance with capital D)
const table = $('#myTable').DataTable(options);
```

---

## Our Standard: Server-Side Mode

All DataTables in this project use **`serverSide: true`**. This means DataTables never sorts, filters, or paginates client-side — every interaction triggers an Ajax request to the server, and the server returns exactly the page of data to display.

### Why serverSide: true

- Sorting and filtering happen in the database (correct, consistent, handles large datasets)
- No double-sort issues (DataTables won't re-sort locally what the server already sorted)
- Paging is handled by the server (`Skip` / `Take` on the query)

### What DataTables sends automatically

On every draw, DataTables includes in the Ajax request:
- `draw` — monotonic counter; must be echoed back in the response
- `start` — row offset (0-based), binds to `filter.Start`
- `length` — page size, binds to `filter.Length`
- `order[0][column]` — column index of the sorted column (nested; we map it ourselves)
- `order[0][dir]` — `"asc"` or `"desc"` (nested; we map it ourselves)
- `search[value]`, `search[regex]` — DataTables' own search fields (we delete and replace)

### ajax.data — mapping to our filter model

We always override `ajax.data` to:
1. Extract `d.order[0]` → flat `d.sortField` / `d.sortDirection` (mapping column index to field name)
2. Delete the nested params DataTables auto-adds (`d.order`, `d.columns`, `d.search`)
3. Inject our custom filter params as plain strings

```javascript
// Column index → sort field (only orderable columns listed)
const columnToField = { 0: 'status' };

ajax: {
  url: '/User/LoadUsers',
  data: function (d) {
    // Map DT's nested order to flat params the controller expects
    const order      = d.order && d.order[0];
    d.sortField      = columnToField[order ? order.column : 0] || 'status';
    d.sortDirection  = order ? order.dir : 'desc';

    // Remove DT's nested auto-params — controller doesn't use them
    delete d.order;
    delete d.columns;
    delete d.search;   // replaced below with our own plain-string value

    // Custom filter params (bind directly into the filter model)
    d.search = document.getElementById('searchInput').value;
    d.role   = document.getElementById('filterRole').value;
    d.status = document.getElementById('filterStatus').value;

    return d;
  }
},
```

`d.sortField` and `d.sortDirection` bind straight into `filter.SortField` and `filter.SortDirection` on the server (ASP.NET Core model binding is case-insensitive). `d.start` and `d.length` bind into `filter.Start` and `filter.Length`.

### Server response shape — GridResultViewModel<T>

The controller returns `Json(new GridResultViewModel<object>(draw, total, filtered, data))`.

`GridResultViewModel<T>` lives in `Models/GridResultViewModel.cs`:

```csharp
public class GridResultViewModel<T>
{
    public int            Draw            { get; init; }
    public int            RecordsTotal    { get; init; }
    public int            RecordsFiltered { get; init; }
    public IEnumerable<T> Data            { get; init; }

    public GridResultViewModel(int draw, int recordsTotal, int recordsFiltered, IEnumerable<T> data)
    {
        Draw            = draw;
        RecordsTotal    = recordsTotal;
        RecordsFiltered = recordsFiltered;
        Data            = data;
    }
}
```

ASP.NET Core's default camelCase serializer produces exactly `{ draw, recordsTotal, recordsFiltered, data }` — what DataTables expects.

- `recordsTotal` — total rows with no filters (used in "filtered from N total" info text)
- `recordsFiltered` — rows after filters but before paging (drives pagination math)
- `data` — the current page of rows

### Standard controller action

```csharp
[HttpGet]
public async Task<IActionResult> LoadUsers([FromQuery] UserFilter filter, [FromQuery] int draw = 1)
{
    var total    = await userRepository.Count(new UserFilter());  // unfiltered total
    var filtered = await userRepository.Count(filter);            // filtered count (no paging)
    var users    = await userRepository.Load(filter);             // filtered + sorted + paged

    var data = users.Select(u => new
    {
        id        = u.Id,
        name      = u.Name,
        email     = u.Email,
        // ... only fields the table needs
    });

    return Json(new GridResultViewModel<object>(draw, total, filtered, data));
}
```

`Count` and `Load` in the repository use a shared private `ApplyFilters` helper so filtering logic lives in one place. `Count` ignores `Start`/`Length`; `Load` applies `Skip`/`Take` only when both are set.

EF Core's `DbContext` is not thread-safe — `total`, `filtered`, and `users` must be awaited **sequentially**, not with `Task.WhenAll`.

### BaseFilter — server-side fields

`BaseFilter<T>` (in `Data/Base/BaseFilter.cs`) already has everything needed for server-side DataTables:

```csharp
public class BaseFilter<T>
{
    public T?      Id            { get; set; }
    public string? SortField     { get; set; }
    public string? SortDirection { get; set; }
    public int?    Start         { get; set; }
    public int?    Length        { get; set; }
}
```

Concrete filters (e.g. `UserFilter`) extend this and add domain-specific search fields (`Search`, `Role`, `Status`, etc.).

### SortField and SortDirection enums

Use `SortFieldEnum` and `SortDirectionEnum` (both in `Enums/`) for all sort field/direction constants:

```csharp
SortFieldEnum.Status   // "status"
SortDirectionEnum.Asc  // "asc"
SortDirectionEnum.Desc // "desc"
```

The repository's `Load` uses a `switch` on `filter.SortField`:

```csharp
var desc = filter.SortDirection == SortDirectionEnum.Desc;
query = filter.SortField switch
{
    SortFieldEnum.Status => desc ? query.OrderByDescending(u => u.Status) : query.OrderBy(u => u.Status),
    _                    => desc ? query.OrderByDescending(u => u.Id)     : query.OrderBy(u => u.Id)
};
```

---

## Standard JS Config

```javascript
const dt = new DataTable('.datatables-xxx', {
  serverSide: true,
  scrollX:    true,       // horizontal scroll instead of Responsive extension
  responsive: false,      // Responsive extension conflicts with scrollX — always disable
  pageLength: 10,
  order: [[0, 'desc']],   // default sort column and direction

  ajax: { /* see ajax.data pattern above */ },

  columns: [
    { data: 'status' },   // 0 — sortable
    { data: 'name'   },   // 1 — etc.
  ],

  columnDefs: [ /* render functions */ ],

  drawCallback: function () {
    // Re-initialize Bootstrap tooltips on every draw
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
      if (!bootstrap.Tooltip.getInstance(el)) {
        new bootstrap.Tooltip(el);
      }
    });
  },

  layout: {
    topStart:    null,
    topEnd:      null,
    bottomStart: { rowClass: 'row mx-3 justify-content-between', features: ['info'] },
    bottomEnd:   'paging'
  },

  language: {
    search: '',
    paginate: {
      next:     '<i class="icon-base ri ri-arrow-right-s-line scaleX-n1-rtl icon-22px"></i>',
      previous: '<i class="icon-base ri ri-arrow-left-s-line  scaleX-n1-rtl icon-22px"></i>',
      first:    '<i class="icon-base ri ri-skip-back-mini-line    scaleX-n1-rtl icon-22px"></i>',
      last:     '<i class="icon-base ri ri-skip-forward-mini-line scaleX-n1-rtl icon-22px"></i>'
    }
  },
});
```

---

## Loading Indicator — Notiflix Pulse

Use **Notiflix `Block.pulse`** on the `.card-datatable` element. Do NOT use DataTables' built-in `processing: true`.

```javascript
// In VendorStyles
// <link rel="stylesheet" href="~/vendor/libs/notiflix/notiflix.css" />

// In VendorScripts
// <script src="~/vendor/libs/notiflix/notiflix.js"></script>

// After DataTable initialization — call immediately for initial load,
// because preXhr.dt does not fire for the very first request:
Block.pulse('.card-datatable');

dt.on('preXhr.dt', function () {
  Block.pulse('.card-datatable');
});

dt.on('xhr.dt', function () {
  Block.remove('.card-datatable');
});
```

---

## External Filters

Filters live in the `card-header` above the table — **not** built via DataTables' `initComplete`. They are plain HTML elements whose `change`/`input` events trigger `ajax.reload`.

### Filter reload rules

- **Filter change** (search, dropdown) → `ajax.reload(null, true)` — resets to page 1
- **In-place update** (status toggle, local row edit) → `dtRow.data(newData).draw(false)` — stays on current page, no server call

```javascript
// Search with debounce
let searchTimeout;
document.getElementById('searchInput').addEventListener('input', function () {
  clearTimeout(searchTimeout);
  searchTimeout = setTimeout(function () { dt.ajax.reload(null, true); }, 500);
});

// Dropdown filters — immediate
document.getElementById('filterRole').addEventListener('change', function () {
  dt.ajax.reload(null, true);
});
```

### bootstrap-select for dropdowns

Role and Status filter dropdowns use bootstrap-select with a divider after the "All" option:

```html
@* VendorStyles *@
<link rel="stylesheet" href="~/vendor/libs/bootstrap-select/bootstrap-select.css" />

@* VendorScripts *@
<script src="~/vendor/libs/bootstrap-select/bootstrap-select.js"></script>
```

```html
<select id="filterRole" class="selectpicker w-100" data-style="btn-default" data-width="100%">
  <option value="">All Roles</option>
  <option data-divider="true"></option>
  <option value="@UserRoleEnum.Customer">Customer</option>
  <option value="@UserRoleEnum.Company">Company</option>
</select>
```

Always use enum values (e.g. `@UserRoleEnum.Customer`) in option values — never raw strings.

### Search input

```html
<div class="input-group input-group-merge">
  <span class="input-group-text"><i class="icon-base ri ri-search-line icon-20px"></i></span>
  <input type="text" id="searchInput" class="form-control" placeholder="Search..." />
</div>
```

### Filter layout — card-header

```html
<div class="card-header border-bottom">
  <div class="row g-3 align-items-center">
    <div class="col-12 col-md-7">
      <!-- search input -->
    </div>
    <div class="col-12 col-md-5">
      <div class="row g-3">
        <div class="col-6"><!-- role select --></div>
        <div class="col-6"><!-- status select --></div>
      </div>
    </div>
  </div>
</div>
```

60/40 split between search and selects. On narrow viewports the selects wrap below the search naturally.

---

## Local Row Update (Status Toggle)

After a successful status change, update the row in DataTables' internal data store and redraw without hitting the server:

```javascript
const dtRow = dt.row(btn.closest('tr'));

fetch('/User/UpdateUserStatus', { method: 'POST', ... })
  .then(res => res.json())
  .then(body => {
    const rowData  = dtRow.data();
    rowData.status = body.status;
    dtRow.data(rowData).draw(false);   // false = stay on current page
  });
```

### Self-status protection

In the controller, always prevent a user from changing their own status:

```csharp
if (id.ToString() == HttpContext.Session.GetString("UserId"))
{
    return BadRequest();
}
```

Return `BadRequest()` (400). In JS, handle `res.status === 400` with a "Not allowed" SweetAlert2 before the generic error case.

---

## SweetAlert2 Confirmations

Use SweetAlert2 for all destructive or status-change confirmations. Always use HTML in the `html:` field to color the target status:

```javascript
const confirmTextMap = {
  active:  'Sure you want to change status to <span class="text-danger fw-medium">Blocked</span>?',
  blocked: 'Sure you want to change status to <span class="text-success fw-medium">Active</span>?',
  pending: 'Sure you want to change status to <span class="text-success fw-medium">Active</span>?'
};

Swal.fire({
  title: 'Confirm Action',
  html:  confirmTextMap[currentStatus],
  icon:  'warning',
  showCancelButton:  true,
  confirmButtonText: 'Yes, change it',
  cancelButtonText:  'Cancel',
  customClass: {
    confirmButton: 'btn btn-primary me-3',
    cancelButton:  'btn btn-label-secondary'
  },
  buttonsStyling: false
});
```

```html
@* VendorStyles *@
<link rel="stylesheet" href="~/vendor/libs/sweetalert2/sweetalert2.css" />

@* VendorScripts *@
<script src="~/vendor/libs/sweetalert2/sweetalert2.js"></script>
```

---

## Standard HTML Shell

```html
<div class="card">
  <div class="card-header border-bottom">
    <!-- filter row -->
  </div>
  <div class="card-datatable">
    <table class="datatables-xxx table">
      <thead>
        <tr>
          <th>Column A</th>
          <th>Column B</th>
        </tr>
      </thead>
    </table>
  </div>
</div>
```

`Block.pulse` targets `.card-datatable` — do not remove or rename that class.

---

## Required Vendor Assets (every DataTable page)

```html
@* VendorStyles — always include *@
<link rel="stylesheet" href="~/vendor/libs/datatables-bs5/datatables.bootstrap5.css" />
<link rel="stylesheet" href="~/vendor/libs/datatables-responsive-bs5/responsive.bootstrap5.css" />
<link rel="stylesheet" href="~/vendor/libs/datatables-buttons-bs5/buttons.bootstrap5.css" />

@* Plus for loading indicator and filters *@
<link rel="stylesheet" href="~/vendor/libs/notiflix/notiflix.css" />
<link rel="stylesheet" href="~/vendor/libs/bootstrap-select/bootstrap-select.css" />
<link rel="stylesheet" href="~/vendor/libs/sweetalert2/sweetalert2.css" />

@* VendorScripts — always include *@
<script src="~/vendor/libs/datatables-bs5/datatables-bootstrap5.js"></script>

@* Plus for loading indicator, filters and confirmations *@
<script src="~/vendor/libs/notiflix/notiflix.js"></script>
<script src="~/vendor/libs/bootstrap-select/bootstrap-select.js"></script>
<script src="~/vendor/libs/sweetalert2/sweetalert2.js"></script>
```

The DT bundle already includes Responsive, Buttons, and Select extensions — no extra extension scripts needed.

---

## Materio Layout Tweaks

Always apply after initialization so buttons and layout elements match the Materio theme:

```javascript
setTimeout(() => {
  [
    { selector: '.dt-buttons .btn',            classToRemove: 'btn-secondary' },
    { selector: '.dt-length .form-select',     classToAdd: 'ms-0' },
    { selector: '.dt-length',                  classToAdd: 'mb-md-4 mb-0' },
    { selector: '.dt-layout-end',              classToRemove: 'justify-content-between', classToAdd: 'd-flex gap-md-4 justify-content-md-between justify-content-center gap-md-2 flex-wrap mt-0' },
    { selector: '.dt-layout-start',            classToAdd: 'mt-md-0 mt-5' },
    { selector: '.dt-layout-start .dt-buttons',classToAdd: 'd-md-flex d-block gap-4 justify-content-center' },
    { selector: '.dt-layout-end .dt-buttons',  classToAdd: 'd-md-flex d-block gap-4 mb-md-0 mb-5 justify-content-center' },
    { selector: '.dt-layout-table',            classToRemove: 'row mt-2' },
    { selector: '.dt-layout-full',             classToRemove: 'col-md col-12' },
    { selector: '.dt-layout-full .table',      classToAdd: 'table-responsive' }
  ].forEach(({ selector, classToRemove, classToAdd }) => {
    document.querySelectorAll(selector).forEach(el => {
      if (classToRemove) { classToRemove.split(' ').forEach(c => el.classList.remove(c)); }
      if (classToAdd)    { classToAdd.split(' ').forEach(c => el.classList.add(c)); }
    });
  });
}, 100);
```

---

## `columns` vs `columnDefs`

| | `columns` | `columnDefs` |
|---|---|---|
| One entry per column? | Yes, in order | No — use `targets` |
| Good for | Declaring which data fields map to which column | Customizing render, width, orderable per column |
| Priority | **Always wins** over `columnDefs` | Lower priority |

```javascript
// columns — positional; one object per column
columns: [
  { data: 'status' },
  { data: 'name'   },
]

// columnDefs — targets selects which columns to configure
columnDefs: [
  { targets: 0,  width: '120px' },
  { targets: -1, orderable: false, searchable: false },
  { targets: '_all', defaultContent: '—' }
]
```

---

## `columns.render` — Transforming Data

```javascript
// Simple HTML
render: function(data, type, row) {
  return '<strong>' + data + '</strong>';
}

// Type-aware (show HTML for display, raw value for sort/filter)
render: function(data, type, row) {
  if (type === 'display') {
    return '<span class="badge bg-label-success">' + data + '</span>';
  }
  return data;
}
```

DataTables strips HTML tags before searching — a badge `<span>Active</span>` is searchable as `"Active"`.

---

## Key API Methods

```javascript
// Draw / refresh
table.draw();

// Ajax reload — reset to page 1 (use for filter changes)
table.ajax.reload(null, true);

// Ajax reload — stay on current page (use for background refreshes)
table.ajax.reload(null, false);

// Update one row locally without server call
const row = table.row(trElement);
const data = row.data();
data.status = 'active';
row.data(data).draw(false);

// Events for before/after each Ajax request
table.on('preXhr.dt', function () { /* show spinner */ });
table.on('xhr.dt',    function () { /* hide spinner */ });
```

---

## Checklist: Adding a New DataTable Page

1. **Controller** — create a dedicated controller (e.g. `UserController`) with `[RequireLogin]`:
   - Page action (e.g. `List()`) — loads ViewBag stats, returns the view
   - `[HttpGet] LoadXxx([FromQuery] XxxFilter filter, [FromQuery] int draw = 1)` — returns `GridResultViewModel<object>`
   - `[HttpPost] UpdateXxxStatus(long id)` — self-protection check first, then status logic

2. **Repository** — ensure the filter class extends `BaseFilter<T>` (already has `SortField`, `SortDirection`, `Start`, `Length`). Add domain-specific filter fields. Implement:
   - Private `ApplyFilters` helper
   - `Count(filter)` — filtered count without paging
   - `Load(filter)` — filtered + sorted + paged

3. **JS file** — create `wwwroot/js/app-xxx-list.js`:
   - Define `columnToField` map
   - Initialize DataTable with `serverSide: true`, `scrollX: true`, `responsive: false`, `pageLength: 10`
   - Wire `preXhr.dt` / `xhr.dt` to `Block.pulse` / `Block.remove`
   - Wire filter element events to `ajax.reload(null, true)`
   - Add Materio layout tweaks block

4. **View** — `Views/Xxx/List.cshtml` with `Layout = "_HomeLayout"`:
   - `@section VendorStyles` / `@section VendorScripts` with required assets
   - Card-header filter row (60/40 search/selects)
   - `<div class="card-datatable"><table class="datatables-xxx table">...`

5. **Menu** — add `<li>` to `_VerticalMenu.cshtml` pointing to the new controller/action

6. **Vendor assets** — verify all required libs are in `wwwroot/vendor/libs/`; copy missing ones from `C:\Software Templates\Materio\AspnetCoreMvcFull\wwwroot\vendor\libs\`
