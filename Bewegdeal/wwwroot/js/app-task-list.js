/**
 * My Requests (Task List) — Bewegdeal
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  const dt_task_table = document.querySelector('.datatables-tasks');

  // Status → badge class + label
  const statusObj = {
    active:    { title: 'Active',    class: 'bg-label-success'   },
    pending:   { title: 'Pending',   class: 'bg-label-warning'   },
    completed: { title: 'Completed', class: 'bg-label-primary'   },
    cancelled: { title: 'Cancelled', class: 'bg-label-danger'    }
  };

  // Type → icon + color
  const typeObj = {
    moving:    { icon: 'ri-truck-line',          color: 'primary', label: 'Moving'    },
    removal:   { icon: 'ri-delete-bin-7-line',   color: 'danger',  label: 'Removal'   },
    pickup:    { icon: 'ri-store-2-line',         color: 'info',    label: 'Pickup'    },
    transport: { icon: 'ri-car-line',             color: 'warning', label: 'Transport' }
  };

  if (!dt_task_table) { return; }

  const dt_task = new DataTable(dt_task_table, {
    ajax: '/Home/GetTasks',
    columns: [
      { data: 'id'        },  // 0 — responsive control
      { data: 'id'        },  // 1 — checkbox
      { data: 'name'      },  // 2 — image + name + type badge
      { data: 'createdAt' },  // 3 — creation date
      { data: 'cost'      },  // 4 — cost
      { data: 'status'    },  // 5 — status badge
      { data: 'views'     },  // 6 — view count
      { data: 'type'      },  // 7 — type (hidden, used for filter)
      { data: 'id'        }   // 8 — actions
    ],
    columnDefs: [
      {
        // Responsive expand control
        className: 'control',
        searchable: false,
        orderable: false,
        responsivePriority: 2,
        targets: 0,
        render: () => ''
      },
      {
        // Checkbox
        targets: 1,
        orderable: false,
        searchable: false,
        responsivePriority: 4,
        render: () => '<input type="checkbox" class="dt-checkboxes form-check-input">',
        checkboxes: {
          selectAllRender: '<input type="checkbox" class="form-check-input">'
        }
      },
      {
        // Image slot + request name + type label
        targets: 2,
        responsivePriority: 1,
        render: function (data, type, full) {
          const name  = full['name'];
          const type_ = full['type'];
          const image = full['image'];
          const info  = typeObj[type_] || { icon: 'ri-file-list-line', color: 'secondary', label: type_ };

          let imageSlot;
          if (image) {
            imageSlot =
              '<div class="rounded overflow-hidden" style="width:48px;height:48px;flex-shrink:0;">' +
                '<img src="' + image + '" alt="" style="width:100%;height:100%;object-fit:cover;" />' +
              '</div>';
          } else {
            imageSlot =
              '<div class="avatar-initial rounded bg-label-' + info.color + '" style="width:48px;height:48px;flex-shrink:0;display:flex;align-items:center;justify-content:center;">' +
                '<i class="icon-base ri ' + info.icon + ' icon-22px"></i>' +
              '</div>';
          }

          return (
            '<div class="d-flex align-items-center gap-3">' +
              imageSlot +
              '<div class="d-flex flex-column">' +
                '<span class="text-heading fw-medium text-truncate">' + name + '</span>' +
                '<small class="text-muted text-capitalize">' + info.label + '</small>' +
              '</div>' +
            '</div>'
          );
        }
      },
      {
        // Creation date
        targets: 3,
        render: (data) => '<span>' + (data || '—') + '</span>'
      },
      {
        // Cost
        targets: 4,
        render: function (data) {
          if (data === null || data === undefined) { return '<span class="text-muted">—</span>'; }
          return '<span>€ ' + Number(data).toLocaleString('de-AT', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + '</span>';
        }
      },
      {
        // Status badge
        targets: 5,
        render: function (data) {
          const obj = statusObj[data] || { title: data, class: 'bg-label-secondary' };
          return '<span class="badge rounded-pill ' + obj.class + ' text-capitalize">' + obj.title + '</span>';
        }
      },
      {
        // Views count
        targets: 6,
        render: function (data) {
          return (
            '<span class="d-flex align-items-center gap-1">' +
              '<i class="icon-base ri ri-eye-line icon-16px text-muted"></i>' +
              '<span>' + (data || 0) + '</span>' +
            '</span>'
          );
        }
      },
      {
        // Type — hidden column used only for filter
        targets: 7,
        visible: false,
        searchable: true
      },
      {
        // Actions — edit + delete buttons
        targets: -1,
        title: 'Actions',
        searchable: false,
        orderable: false,
        render: (data, type, full) => {
          const status     = full['status'];
          const canEdit    = status === 'active';
          const canDelete  = status !== 'completed';

          const editBtn = canEdit
            ? '<a href="/Home/EditTask/' + data + '" class="btn btn-icon btn-text-secondary rounded-pill" title="Edit">' +
                '<i class="icon-base ri ri-edit-box-line icon-md"></i>' +
              '</a>'
            : '<span class="btn btn-icon btn-text-secondary rounded-pill disabled opacity-50" title="Editing is not available in this status">' +
                '<i class="icon-base ri ri-lock-line icon-md"></i>' +
              '</span>';

          const deleteBtn = canDelete
            ? '<button type="button" class="btn btn-icon btn-text-danger rounded-pill dt-delete-btn"' +
                ' data-id="' + data + '" data-name="' + (full['name'] || '').replace(/"/g, '&quot;') + '" title="Delete">' +
                '<i class="icon-base ri ri-delete-bin-7-line icon-md"></i>' +
              '</button>'
            : '';

          return '<div class="d-flex align-items-center">' + editBtn + deleteBtn + '</div>';
        }
      }
    ],
    select: {
      style: 'multi',
      selector: 'td:nth-child(2)'
    },
    order: [[3, 'desc']],
    layout: {
      topStart: {
        rowClass: 'row m-2 my-0 mt-0 justify-content-between',
        features: [
          {
            buttons: [
              {
                extend: 'collection',
                className: 'btn btn-outline-secondary dropdown-toggle waves-effect',
                text: '<span class="d-flex align-items-center gap-2"><i class="icon-base ri ri-download-line icon-16px me-sm-1"></i><span class="d-inline-block">Export</span></span>',
                buttons: [
                  {
                    extend: 'print',
                    text: '<span class="d-flex align-items-center"><i class="icon-base ri ri-printer-line me-1"></i>Print</span>',
                    className: 'dropdown-item',
                    exportOptions: {
                      columns: [2, 3, 4, 5, 6],
                      format: {
                        body: function (inner) {
                          if (!inner.length) { return inner; }
                          const el = new DOMParser().parseFromString(inner, 'text/html').body.childNodes;
                          let result = '';
                          el.forEach(item => {
                            const fw = item.querySelector && item.querySelector('span.fw-medium');
                            result += fw ? fw.textContent : (item.textContent || item.innerText || '');
                          });
                          return result;
                        }
                      }
                    },
                    customize: function (win) {
                      win.document.body.style.color           = config.colors.headingColor;
                      win.document.body.style.borderColor     = config.colors.borderColor;
                      win.document.body.style.backgroundColor = config.colors.bodyBg;
                      const table = win.document.body.querySelector('table');
                      table.classList.add('compact');
                      table.style.color           = 'inherit';
                      table.style.borderColor     = 'inherit';
                      table.style.backgroundColor = 'inherit';
                    }
                  },
                  {
                    extend: 'csv',
                    text: '<span class="d-flex align-items-center"><i class="icon-base ri ri-file-text-line me-1"></i>Csv</span>',
                    className: 'dropdown-item',
                    exportOptions: { columns: [2, 3, 4, 5, 6] }
                  },
                  {
                    extend: 'excel',
                    text: '<span class="d-flex align-items-center"><i class="icon-base ri ri-file-excel-line me-1"></i>Excel</span>',
                    className: 'dropdown-item',
                    exportOptions: { columns: [2, 3, 4, 5, 6] }
                  },
                  {
                    extend: 'pdf',
                    text: '<span class="d-flex align-items-center"><i class="icon-base ri ri-file-pdf-line me-1"></i>Pdf</span>',
                    className: 'dropdown-item',
                    exportOptions: { columns: [2, 3, 4, 5, 6] }
                  },
                  {
                    extend: 'copy',
                    text: '<i class="icon-base ri ri-file-copy-line me-1"></i>Copy',
                    className: 'dropdown-item',
                    exportOptions: { columns: [2, 3, 4, 5, 6] }
                  }
                ]
              }
            ]
          }
        ]
      },
      topEnd: {
        features: [
          {
            search: {
              placeholder: 'Search Request',
              text: '_INPUT_'
            }
          }
        ]
      },
      bottomStart: {
        rowClass: 'row mx-3 justify-content-between',
        features: ['info']
      },
      bottomEnd: 'paging'
    },
    language: {
      paginate: {
        next:     '<i class="icon-base ri ri-arrow-right-s-line scaleX-n1-rtl icon-22px"></i>',
        previous: '<i class="icon-base ri ri-arrow-left-s-line  scaleX-n1-rtl icon-22px"></i>',
        first:    '<i class="icon-base ri ri-skip-back-mini-line    scaleX-n1-rtl icon-22px"></i>',
        last:     '<i class="icon-base ri ri-skip-forward-mini-line scaleX-n1-rtl icon-22px"></i>'
      }
    },
    responsive: {
      details: {
        display: DataTable.Responsive.display.modal({
          header: function (row) {
            return 'Details of ' + row.data()['name'];
          }
        }),
        type: 'column',
        renderer: function (api, rowIdx, columns) {
          const data = columns
            .map(col =>
              col.title !== ''
                ? `<tr data-dt-row="${col.rowIndex}" data-dt-column="${col.columnIndex}">
                     <td>${col.title}:</td><td>${col.data}</td>
                   </tr>`
                : ''
            )
            .join('');
          if (!data) { return false; }
          const div   = document.createElement('div');
          div.classList.add('table-responsive');
          const table = document.createElement('table');
          table.classList.add('table');
          const tbody = document.createElement('tbody');
          tbody.innerHTML = data;
          table.appendChild(tbody);
          div.appendChild(table);
          return div;
        }
      }
    },
    initComplete: function () {
      const api = this.api();

      // Type filter (searches hidden column 7)
      const typeContainer = document.querySelector('.task_type');
      if (typeContainer) {
        const typeSelect = document.createElement('select');
        typeSelect.id = 'TaskType';
        typeSelect.className = 'form-select text-capitalize';
        typeSelect.innerHTML = '<option value="">Select Type</option>';
        typeContainer.appendChild(typeSelect);
        typeSelect.addEventListener('change', () => {
          const val = typeSelect.value ? `^${typeSelect.value}$` : '';
          api.column(7).search(val, true, false).draw();
        });
        Array.from(new Set(api.column(7).data().toArray())).sort().forEach(d => {
          const info = typeObj[d] || { label: d };
          const opt  = document.createElement('option');
          opt.value       = d;
          opt.textContent = info.label;
          typeSelect.appendChild(opt);
        });
      }

      // Status filter (column 5 — search against rendered badge text)
      const statusContainer = document.querySelector('.task_status');
      if (statusContainer) {
        const statusSelect = document.createElement('select');
        statusSelect.id = 'TaskStatus';
        statusSelect.className = 'form-select text-capitalize';
        statusSelect.innerHTML = '<option value="">Select Status</option>';
        statusContainer.appendChild(statusSelect);
        statusSelect.addEventListener('change', () => {
          const val = statusSelect.value ? `^${statusSelect.value}$` : '';
          api.column(5).search(val, true, false).draw();
        });
        Array.from(new Set(api.column(5).data().toArray())).sort().forEach(d => {
          const obj = statusObj[d] || { title: d };
          const opt = document.createElement('option');
          opt.value       = obj.title;
          opt.textContent = obj.title;
          statusSelect.appendChild(opt);
        });
      }
    }
  });

  // ── Row click → preview offcanvas ────────────────────────────────────────
  const previewOffcanvasEl = document.querySelector('#requestPreviewOffcanvas');
  const previewOffcanvas   = previewOffcanvasEl
    ? new bootstrap.Offcanvas(previewOffcanvasEl)
    : null;

  // Helper: currency symbol
  function currencySymbol(c) { return c === 'USD' ? '$' : '€'; }

  function openPreview(data) {
    if (!previewOffcanvasEl) { return; }

    const type   = data['type']   || '';
    const status = data['status'] || '';
    const info   = typeObj[type]   || { icon: 'ri-file-list-line', color: 'secondary', label: type };
    const st     = statusObj[status] || { title: status, class: 'bg-label-secondary' };

    // ── Image banner ───────────────────────────────────────────────────────
    const imgEl          = previewOffcanvasEl.querySelector('#previewImage');
    const placeholder    = previewOffcanvasEl.querySelector('#previewImagePlaceholder');
    const placeholderIcon = previewOffcanvasEl.querySelector('#previewTypePlaceholderIcon');
    const imageWrap      = previewOffcanvasEl.querySelector('#previewImageWrap');

    if (data['image']) {
      imgEl.src          = data['image'];
      imgEl.style.display = 'block';
      placeholder.style.display = 'none';
    } else {
      imgEl.style.display       = 'none';
      placeholder.style.display = 'flex';
      placeholderIcon.className  = `ri ${info.icon} text-${info.color}`;
      imageWrap.style.background = `rgba(var(--bs-${info.color}-rgb),.08)`;
    }

    // ── Badges ─────────────────────────────────────────────────────────────
    const typeBadge   = previewOffcanvasEl.querySelector('#previewTypeBadge');
    const statusBadge = previewOffcanvasEl.querySelector('#previewStatusBadge');
    typeBadge.className   = `badge rounded-pill bg-label-${info.color}`;
    typeBadge.textContent = info.label;
    statusBadge.className   = `badge rounded-pill ${st.class}`;
    statusBadge.textContent = st.title;

    // ── Title & description ────────────────────────────────────────────────
    previewOffcanvasEl.querySelector('#previewName').textContent        = data['name'] || '—';
    previewOffcanvasEl.querySelector('#previewDescription').textContent = data['description'] || '';

    // ── Meta ───────────────────────────────────────────────────────────────
    const cost = data['cost'];
    previewOffcanvasEl.querySelector('#previewCost').textContent =
      cost != null ? `${currencySymbol(data['currency'])} ${Number(cost).toLocaleString('de-AT', { minimumFractionDigits: 2 })}` : '—';
    previewOffcanvasEl.querySelector('#previewDate').textContent  = data['createdAt'] || '—';
    previewOffcanvasEl.querySelector('#previewViews').textContent = data['views'] ?? 0;

    // ── Addresses ─────────────────────────────────────────────────────────
    const pickup   = (data['pickupAddress']   || '').trim();
    const delivery = (data['deliveryAddress'] || '').trim();
    const addrSection   = previewOffcanvasEl.querySelector('#previewAddressSection');
    const pickupWrap    = previewOffcanvasEl.querySelector('#previewPickupWrap');
    const deliveryWrap  = previewOffcanvasEl.querySelector('#previewDeliveryWrap');

    if (pickup || delivery) {
      addrSection.classList.remove('d-none');
      if (pickup) {
        pickupWrap.classList.remove('d-none');
        previewOffcanvasEl.querySelector('#previewPickup').textContent = pickup;
      } else { pickupWrap.classList.add('d-none'); }
      if (delivery) {
        deliveryWrap.classList.remove('d-none');
        previewOffcanvasEl.querySelector('#previewDelivery').textContent = delivery;
      } else { deliveryWrap.classList.add('d-none'); }
    } else {
      addrSection.classList.add('d-none');
    }

    // ── Additional media ───────────────────────────────────────────────────
    const mediaPaths  = (data['media'] || '').split(',').filter(Boolean);
    const mediaSection = previewOffcanvasEl.querySelector('#previewMediaSection');
    const mediaGrid    = previewOffcanvasEl.querySelector('#previewMediaGrid');
    mediaGrid.innerHTML = '';

    const videoExts = ['.mp4', '.mov', '.avi', '.webm', '.mkv'];
    const isVideo   = p => videoExts.includes(p.split('.').pop().toLowerCase().replace(/^/, '.'));

    if (mediaPaths.length) {
      mediaSection.classList.remove('d-none');
      mediaPaths.forEach(path => {
        if (isVideo(path)) {
          const el = document.createElement('div');
          el.className = 'd-flex align-items-center gap-2 p-2 border rounded-3 bg-light-subtle';
          el.innerHTML = `<i class="ri ri-video-line text-primary icon-20px"></i>
                          <span class="small text-truncate" style="max-width:140px">${path.split('/').pop()}</span>`;
          mediaGrid.appendChild(el);
        } else {
          const img = document.createElement('img');
          img.src   = path;
          img.alt   = '';
          img.style.cssText = 'width:80px;height:80px;object-fit:cover;border-radius:8px;border:1px solid var(--bs-border-color)';
          mediaGrid.appendChild(img);
        }
      });
    } else {
      mediaSection.classList.add('d-none');
    }

    // ── Edit button ────────────────────────────────────────────────────────
    const editBtn = previewOffcanvasEl.querySelector('#previewEditBtn');
    if (status === 'active') {
      editBtn.href               = `/Home/EditTask/${data['id']}`;
      editBtn.classList.remove('d-none', 'disabled', 'opacity-50');
    } else {
      editBtn.classList.add('d-none');
    }

    // ── Delete button in offcanvas ─────────────────────────────────────────
    const deleteBtn = previewOffcanvasEl.querySelector('#previewDeleteBtn');
    if (deleteBtn) {
      if (status !== 'completed') {
        deleteBtn.classList.remove('d-none');
        deleteBtn.onclick = function () {
          previewOffcanvas.hide();
          openDeleteModal(data['id'], data['name'] || '');
        };
      } else {
        deleteBtn.classList.add('d-none');
      }
    }

    previewOffcanvas.show();
  }

  // ── Delete confirmation modal ─────────────────────────────────────────────
  const deleteModalEl = document.querySelector('#deleteTaskModal');
  const deleteModal   = deleteModalEl ? new bootstrap.Modal(deleteModalEl) : null;

  function openDeleteModal(id, name) {
    const nameEl = document.querySelector('#deleteTaskName');
    const idEl   = document.querySelector('#deleteTaskId');
    if (nameEl) { nameEl.textContent = name; }
    if (idEl)   { idEl.value = id; }
    if (deleteModal) { deleteModal.show(); }
  }

  // Delete button clicks inside the DataTable actions column
  dt_task_table.addEventListener('click', function (e) {
    const btn = e.target.closest('.dt-delete-btn');
    if (!btn) { return; }
    e.stopPropagation();
    openDeleteModal(btn.dataset.id, btn.dataset.name || '');
  });

  // Bind row clicks — skip checkbox column (col 1), actions column (last)
  dt_task.on('click', 'tbody tr td:not(:nth-child(2)):not(:last-child)', function () {
    // Skip responsive detail rows
    if (this.closest('tr')?.classList.contains('child')) { return; }
    const row = dt_task.row(this.closest('tr'));
    if (!row || !row.data()) { return; }
    openPreview(row.data());
  });

  // Pointer cursor on hoverable cells
  dt_task_table.style.cursor = 'default';
  dt_task.on('draw', function () {
    dt_task_table.querySelectorAll('tbody tr td:not(:nth-child(2)):not(:last-child)')
      .forEach(td => { td.style.cursor = 'pointer'; });
  });

  // Layout tweaks (Materio theme)
  setTimeout(() => {
    [
      { selector: '.dt-buttons .btn',       classToRemove: 'btn-secondary' },
      { selector: '.dt-length .form-select', classToAdd: 'ms-0' },
      { selector: '.dt-length',             classToAdd: 'mb-md-4 mb-0' },
      { selector: '.dt-layout-end',         classToRemove: 'justify-content-between', classToAdd: 'd-flex gap-md-4 justify-content-md-between justify-content-center gap-md-2 flex-wrap mt-0' },
      { selector: '.dt-layout-start',       classToAdd: 'mt-md-0 mt-5' },
      { selector: '.dt-layout-start .dt-buttons', classToAdd: 'd-md-flex d-block gap-4 justify-content-center' },
      { selector: '.dt-layout-end .dt-buttons',   classToAdd: 'd-md-flex d-block gap-4 mb-md-0 mb-5 justify-content-center' },
      { selector: '.dt-layout-table',       classToRemove: 'row mt-2' },
      { selector: '.dt-layout-full',        classToRemove: 'col-md col-12' },
      { selector: '.dt-layout-full .table', classToAdd: 'table-responsive' }
    ].forEach(({ selector, classToRemove, classToAdd }) => {
      document.querySelectorAll(selector).forEach(el => {
        if (classToRemove) { classToRemove.split(' ').forEach(c => el.classList.remove(c)); }
        if (classToAdd)    { classToAdd.split(' ').forEach(c => el.classList.add(c)); }
      });
    });
  }, 100);
});
