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

## Data Sources

### Ajax (our standard)
DataTables expects `{ "data": [...] }` by default — which is exactly what `return Json(new { data })` produces in ASP.NET MVC.

```javascript
// Simple — 'data' property is the default dataSrc
ajax: '/Home/GetUsers'

// Explicit dataSrc
ajax: { url: '/Home/GetUsers', dataSrc: 'data' }

// Top-level array (no wrapper object)
ajax: { url: '/api/items', dataSrc: '' }

// Custom property name
ajax: { url: '/api/items', dataSrc: 'staff' }
```

### JavaScript array / object
```javascript
new DataTable('#t', {
    data: [ { name: 'Alice', age: 30 }, ... ],
    columns: [ { data: 'name' }, { data: 'age' } ]
});
```

---

## `columns` vs `columnDefs`

| | `columns` | `columnDefs` |
|---|---|---|
| One entry per column? | Yes, in order | No — use `targets` |
| Good for | Defining all columns | Overriding specific columns |
| Priority | **Always wins** over `columnDefs` | Lower priority |

```javascript
// columns — one object per column, positionally matched
columns: [
    { data: 'id'     },
    { data: 'name'   },
    { data: 'status' }
]

// columnDefs — targets selects which columns to apply to
columnDefs: [
    { targets: 0,      searchable: false, orderable: false },  // first column
    { targets: -1,     searchable: false, orderable: false },  // last column
    { targets: [1, 2], className: 'text-center' },             // multiple
    { targets: '_all', defaultContent: '—' }                   // fallback for all
]
```

`targets` accepts: positive integer (from left), negative integer (from right, -1 = last), array of those, string `'_all'`, CSS class selector string (DT2).

---

## `columns.data` — Reading Row Data

| Value | Meaning |
|---|---|
| `'name'` | Property by name |
| `'address.city'` | Nested property (dot notation) |
| `0` | Array index |
| `null` | Pass the whole row object to `render` |
| `function(row, type, set, meta)` | Custom getter/setter |
| `{ _: 'raw', display: 'html', filter: 'plain' }` | Orthogonal data per operation type |

```javascript
{ data: 'name' }                          // simple property
{ data: 'hr.position' }                   // nested
{ data: null, defaultContent: 'N/A' }     // whole row → render
{ data: function(row, type) {             // computed
    return row.firstName + ' ' + row.lastName;
}}
```

---

## `columns.render` — Transforming Data for Display

Render is called with `(data, type, row, meta)`. The `type` tells you why:

| type | Used for |
|---|---|
| `'display'` | What the cell shows |
| `'filter'` / `'search'` | What the search index sees |
| `'sort'` / `'order'` | The sort key |
| `'type'` | Type detection |

```javascript
// Simple HTML wrapper
render: function(data, type, row) {
    return '<strong>' + data + '</strong>';
}

// Type-aware — show HTML for display, raw for sort/filter
render: function(data, type, row) {
    if (type === 'display') {
        return '<span class="badge bg-label-success">' + data + '</span>';
    }
    return data;   // sort and filter get the raw value
}

// Truncate long text in display only
render: function(data, type) {
    return type === 'display' && data.length > 40
        ? '<span title="' + data + '">' + data.substr(0, 38) + '…</span>'
        : data;
}
```

**Key insight**: DataTables strips HTML tags before searching, so a badge like `<span>Active</span>` is searchable as "Active" — no special handling needed.

**Built-in helpers**:
```javascript
render: DataTable.render.number(',', '.', 2, '€')   // number formatting
render: DataTable.render.select()                     // checkbox
render: DataTable.render.text()                       // HTML-escape
```

---

## `layout` — Placing Controls

Default positions: `topStart=pageLength`, `topEnd=search`, `bottomStart=info`, `bottomEnd=paging`.

```javascript
layout: {
    topStart: {
        rowClass: 'row m-2 my-0 justify-content-between',
        features: [{ buttons: [ exportDropdown ] }]
    },
    topEnd: {
        features: [
            { search: { placeholder: 'Search…', text: '_INPUT_' } },
            { buttons: [ addNewButton ] }
        ]
    },
    bottomStart: {
        rowClass: 'row mx-3 justify-content-between',
        features: ['info']
    },
    bottomEnd: 'paging'
}
```

Available position names: `topStart`, `topEnd`, `top`, `top2Start`, `top2End`…, `bottomStart`, `bottomEnd`, `bottom`, `bottom2Start`…

Feature values: `'info'`, `'paging'`, `'search'`, `'pageLength'`, `'div'`, `{ buttons: [...] }`, DOM node, function returning DOM node, or `null` to disable.

---

## Buttons Extension

### Export dropdown (collection)
```javascript
{
    extend: 'collection',
    className: 'btn btn-outline-secondary dropdown-toggle waves-effect',
    text: '<i class="ri ri-download-line"></i> Export',
    buttons: [
        {
            extend: 'print',
            text: '<i class="ri ri-printer-line me-1"></i>Print',
            className: 'dropdown-item',
            exportOptions: { columns: [2, 3, 4, 5] },
            customize: function(win) {
                win.document.body.style.color = config.colors.headingColor;
                win.document.body.style.backgroundColor = config.colors.bodyBg;
            }
        },
        { extend: 'csv',   className: 'dropdown-item', exportOptions: { columns: [2,3,4,5] } },
        { extend: 'excel', className: 'dropdown-item', exportOptions: { columns: [2,3,4,5] } },
        { extend: 'pdf',   className: 'dropdown-item', exportOptions: { columns: [2,3,4,5] } },
        { extend: 'copy',  className: 'dropdown-item', exportOptions: { columns: [2,3,4,5] } }
    ]
}
```

### Custom button (e.g. "Add New")
```javascript
{
    text: '<i class="ri ri-add-line"></i> Add New',
    className: 'btn btn-primary',
    attr: {
        'data-bs-toggle': 'offcanvas',
        'data-bs-target': '#offcanvasAddRecord'
    }
}
```

`exportOptions.columns` accepts column indices — always exclude the control (0), checkbox (1), and actions (-1) columns.

---

## Responsive Extension

```javascript
responsive: {
    details: {
        display: DataTable.Responsive.display.modal({
            header: function(row) {
                return 'Details of ' + row.data()['name'];
            }
        }),
        type: 'column',   // column 0 (className: 'control') is the expand trigger
        renderer: function(api, rowIdx, columns) {
            const rows = columns
                .filter(col => col.title !== '')
                .map(col =>
                    `<tr data-dt-row="${col.rowIndex}" data-dt-column="${col.columnIndex}">
                       <td>${col.title}:</td><td>${col.data}</td>
                     </tr>`
                ).join('');
            if (!rows) { return false; }
            const div = document.createElement('div');
            div.className = 'table-responsive';
            const table = document.createElement('table');
            table.className = 'table';
            table.innerHTML = '<tbody>' + rows + '</tbody>';
            div.appendChild(table);
            return div;
        }
    }
}
```

**`columns.responsivePriority`**: lower = stays visible longer as screen shrinks.
- `1` — always visible (like name/actions)
- `2`–`4` — hidden second
- default `10000` — standard column
- `10001`+ — hide first

---

## Select Extension

```javascript
select: {
    style: 'multi',           // 'single' | 'multi' | 'os' | 'multi+shift'
    selector: 'td:nth-child(2)'  // click target (checkbox column)
}
```

---

## Key API Methods

```javascript
const table = new DataTable('#t', options);

// Draw / refresh
table.draw();

// Global search
table.search('Alice').draw();

// Column search (DT2 exact match — no need for regex)
table.column(4).search('customer', { exact: true }).draw();

// Column search (regex — DT1 style, still works in DT2)
table.column(4).search('^customer$', true, false).draw();
// params: (searchStr, isRegex, isSmartSearch)

// Clear column search
table.column(4).search('').draw();

// Ajax reload (resets to page 1)
table.ajax.reload();

// Ajax reload (stay on current page)
table.ajax.reload(null, false);

// Ajax reload with callback
table.ajax.reload(function(json) { console.log(json); });

// Add one row
table.row.add({ name: 'Bob', status: 'active' }).draw();

// Add multiple rows
table.rows.add([ row1, row2 ]).draw();

// Remove a row (tr element)
table.row(trElement).remove().draw();

// Get all row data
const allData = table.rows().data().toArray();

// Get specific column values
const statuses = table.column(5).data().toArray();

// Get row node after adding
const node = table.row.add(data).draw().node();
```

---

## Column Filter Pattern (our standard)

The `initComplete` callback builds filter dropdowns after the table loads, so they are populated from actual data:

```javascript
initComplete: function() {
    const api = this.api();

    // For a column where raw data IS the search term (e.g. role = 'customer')
    const roleSelect = document.createElement('select');
    roleSelect.className = 'form-select text-capitalize';
    roleSelect.innerHTML = '<option value="">Select Role</option>';
    document.querySelector('.user_role').appendChild(roleSelect);
    roleSelect.addEventListener('change', () => {
        api.column(4).search(roleSelect.value, { exact: true }).draw();
    });
    Array.from(new Set(api.column(4).data().toArray())).sort().forEach(val => {
        const opt = document.createElement('option');
        opt.value = val;
        opt.textContent = val.charAt(0).toUpperCase() + val.slice(1);
        roleSelect.appendChild(opt);
    });

    // For a column where raw data maps to a display label (e.g. status → 'Active')
    // The filter value must match the RENDERED TEXT (DataTables strips HTML when searching)
    const statusSelect = document.createElement('select');
    statusSelect.className = 'form-select text-capitalize';
    statusSelect.innerHTML = '<option value="">Select Status</option>';
    document.querySelector('.user_status').appendChild(statusSelect);
    statusSelect.addEventListener('change', () => {
        api.column(5).search(statusSelect.value, { exact: true }).draw();
    });
    Array.from(new Set(api.column(5).data().toArray())).sort().forEach(rawVal => {
        const label = statusObj[rawVal]?.title || rawVal;
        const opt = document.createElement('option');
        opt.value = label;          // search against rendered text
        opt.textContent = label;
        statusSelect.appendChild(opt);
    });
}
```

---

## Special Row Properties (in Ajax JSON)

Include in each row object for automatic DOM behaviour:

| Property | Effect |
|---|---|
| `DT_RowId` | Sets `<tr id="...">` |
| `DT_RowClass` | Adds CSS class to `<tr>` |
| `DT_RowData` | Attaches data via jQuery `.data()` on `<tr>` |
| `DT_RowAttr` | Adds arbitrary HTML attributes to `<tr>` |

---

## Server-Side Processing (for large datasets)

Use when the table has more rows than it's practical to load client-side (rough threshold: >10,000 rows).

```javascript
new DataTable('#t', {
    serverSide: true,
    ajax: { url: '/Home/GetUsers', type: 'POST' },
    columns: [...]
});
```

DataTables sends per request: `draw`, `start`, `length`, `search[value]`, `order[0][column]`, `order[0][dir]`, `columns[i][data]`…

Server must return:
```json
{
    "draw": 1,
    "recordsTotal": 5000,
    "recordsFiltered": 42,
    "data": [ ... ]
}
```

For client-side (our current setup), the server just returns `{ "data": [...] }` and DataTables handles sorting/filtering/paging entirely in the browser.

---

## Standard Layout Tweaks (Materio-specific)

After initialization, apply these class adjustments so buttons match the Materio theme:

```javascript
setTimeout(() => {
    [
        { selector: '.dt-buttons .btn',            classToRemove: 'btn-secondary' },
        { selector: '.dt-length .form-select',     classToAdd: 'ms-0' },
        { selector: '.dt-length',                  classToAdd: 'mb-md-4 mb-0' },
        { selector: '.dt-layout-end',              classToRemove: 'justify-content-between', classToAdd: 'd-flex gap-md-4 justify-content-md-between justify-content-center flex-wrap mt-0' },
        { selector: '.dt-layout-start',            classToAdd: 'mt-md-0 mt-5' },
        { selector: '.dt-layout-start .dt-buttons',classToAdd: 'd-md-flex d-block gap-4 justify-content-center' },
        { selector: '.dt-layout-end .dt-buttons',  classToAdd: 'd-md-flex d-block gap-4 mb-md-0 mb-5 justify-content-center' },
        { selector: '.dt-layout-table',            classToRemove: 'row mt-2' },
        { selector: '.dt-layout-full',             classToRemove: 'col-md col-12' },
        { selector: '.dt-layout-full .table',      classToAdd: 'table-responsive' }
    ].forEach(({ selector, classToRemove, classToAdd }) => {
        document.querySelectorAll(selector).forEach(el => {
            classToRemove?.split(' ').forEach(c => el.classList.remove(c));
            classToAdd?.split(' ').forEach(c => el.classList.add(c));
        });
    });
}, 100);
```

---

## Checklist: Adding a New DataTable Page in Bewegdeal

1. **Controller** — add `[HttpGet] GetXxx()` returning `Json(new { data })` + a page action passing ViewBag stats
2. **JS file** — create `wwwroot/js/app-xxx-list.js` following the standard pattern (statusObj, roleBadgeObj, layout, initComplete filters, layout tweaks, bindDeleteEvent)
3. **View** — `@section VendorStyles` with the four CSS libs, `@section VendorScripts` with the bundle + form-validation + cleave-zen, `@section PageScripts` with the JS file
4. **Vendor assets** — verify all four are in `wwwroot/vendor/libs/`; copy missing ones from `C:\Software Templates\Materio\AspnetCoreMvcFull\wwwroot\vendor\libs\`

### Required vendor assets for every DataTable

**CSS** (in `@section VendorStyles`):
```html
<link rel="stylesheet" href="~/vendor/libs/datatables-bs5/datatables.bootstrap5.css" />
<link rel="stylesheet" href="~/vendor/libs/datatables-responsive-bs5/responsive.bootstrap5.css" />
<link rel="stylesheet" href="~/vendor/libs/datatables-buttons-bs5/buttons.bootstrap5.css" />
<link rel="stylesheet" href="~/vendor/libs/@("@form-validation")/form-validation.css" />
```

**JS** (in `@section VendorScripts`):
```html
<script src="~/vendor/libs/datatables-bs5/datatables-bootstrap5.js"></script>
<script src="~/vendor/libs/@("@form-validation")/popular.js"></script>
<script src="~/vendor/libs/@("@form-validation")/bootstrap5.js"></script>
<script src="~/vendor/libs/@("@form-validation")/auto-focus.js"></script>
<script src="~/vendor/libs/cleave-zen/cleave-zen.js"></script>
```

### ASP.NET JSON endpoint convention
```csharp
[HttpGet]
public async Task<IActionResult> GetXxx()
{
    var items = await repository.GetAll(new XxxFilter());
    var data = items.Select(x => new { x.Id, x.Name, /* only fields the table needs */ });
    return Json(new { data });
}
```

### Standard HTML table shell
```html
<div class="card">
    <div class="card-header border-bottom">
        <h6 class="card-title mb-0">Filters</h6>
        <div class="d-flex justify-content-between align-items-center row pt-4 pb-2 gap-4 gap-md-0 gx-5">
            <div class="col-md-6 filter_one"></div>
            <div class="col-md-6 filter_two"></div>
        </div>
    </div>
    <div class="card-datatable">
        <table class="datatables-xxx table">
            <thead>
                <tr>
                    <th></th>   <!-- responsive control -->
                    <th></th>   <!-- checkbox -->
                    <th>Name</th>
                    <!-- ... -->
                    <th>Actions</th>
                </tr>
            </thead>
        </table>
    </div>
</div>
```

### Standard column 0 — responsive control
```javascript
{
    className: 'control',
    searchable: false,
    orderable: false,
    responsivePriority: 2,
    targets: 0,
    render: () => ''
}
```

### Standard column 1 — checkbox
```javascript
{
    targets: 1,
    orderable: false,
    searchable: false,
    responsivePriority: 4,
    render: () => '<input type="checkbox" class="dt-checkboxes form-check-input">',
    checkboxes: { selectAllRender: '<input type="checkbox" class="form-check-input">' }
}
```

### Standard actions column
```javascript
{
    targets: -1,
    title: 'Actions',
    searchable: false,
    orderable: false,
    render: (data, type, full) => `
      <div class="d-flex align-items-center">
        <a href="javascript:;" class="btn btn-icon btn-text-secondary rounded-pill delete-record">
          <i class="icon-base ri ri-delete-bin-7-line icon-md"></i>
        </a>
        <a href="javascript:;" class="btn btn-icon btn-text-secondary rounded-pill dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
          <i class="icon-base ri ri-more-2-line icon-md"></i>
        </a>
        <div class="dropdown-menu dropdown-menu-end m-0">
          <a href="javascript:;" class="dropdown-item">Edit</a>
        </div>
      </div>`
}
```
