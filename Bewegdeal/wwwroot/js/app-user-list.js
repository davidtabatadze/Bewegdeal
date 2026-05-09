/**
 * Users List — Bewegdeal
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  const dt_user_table = document.querySelector('.datatables-users');

  // Status → badge class + display label
  const statusObj = {
    active:      { title: 'Active',      class: 'bg-label-success' },
    pending:     { title: 'Pending',     class: 'bg-label-warning' },
    blocked:     { title: 'Blocked',     class: 'bg-label-danger'  },
    unverified:  { title: 'Unverified',  class: 'bg-label-secondary' }
  };

  // Role → icon HTML
  const roleBadgeObj = {
    customer:      '<i class="icon-base ri ri-user-line       icon-22px text-primary me-2"></i>',
    company:       '<i class="icon-base ri ri-building-line   icon-22px text-info    me-2"></i>',
    administrator: '<i class="icon-base ri ri-computer-line   icon-22px text-danger  me-2"></i>'
  };

  if (!dt_user_table) { return; }

  const dt_user = new DataTable(dt_user_table, {
    ajax: '/Home/GetUsers',
    columns: [
      { data: 'id'     },   // 0 — responsive control
      { data: 'id'     },   // 1 — checkbox
      { data: 'name'   },   // 2 — user cell
      { data: 'mobile' },   // 3 — mobile
      { data: 'role'   },   // 4 — role
      { data: 'status' },   // 5 — status
      { data: 'id'     }    // 6 — actions
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
        // User cell: avatar initials + name + email
        targets: 2,
        responsivePriority: 4,
        render: function (data, type, full) {
          const name = full['name'];
          const email = full['email'];

          const states = ['success', 'danger', 'warning', 'info', 'dark', 'primary', 'secondary'];
          const state  = states[Math.floor(Math.random() * states.length)];
          const initials = ((name.match(/\b\w/g) || []).map(c => c.toUpperCase()));
          const badge = ((initials.shift() || '') + (initials.pop() || '')).toUpperCase();

          const avatar = '<span class="avatar-initial rounded-circle bg-label-' + state + '">' + badge + '</span>';

          return (
            '<div class="d-flex justify-content-start align-items-center user-name">' +
              '<div class="avatar-wrapper">' +
                '<div class="avatar avatar-sm me-4">' + avatar + '</div>' +
              '</div>' +
              '<div class="d-flex flex-column">' +
                '<span class="text-heading fw-medium text-truncate">' + name + '</span>' +
                '<small class="text-muted">' + email + '</small>' +
              '</div>' +
            '</div>'
          );
        }
      },
      {
        // Mobile
        targets: 3,
        render: (data, type, full) => '<span>' + (full['mobile'] || '—') + '</span>'
      },
      {
        // Role
        targets: 4,
        render: function (data, type, full) {
          const role = full['role'];
          const icon = roleBadgeObj[role] || '';
          const label = role ? (role.charAt(0).toUpperCase() + role.slice(1)) : role;
          return "<span class='text-truncate d-flex align-items-center text-heading'>" + icon + label + '</span>';
        }
      },
      {
        // Status badge
        targets: 5,
        render: function (data, type, full) {
          const status = full['status'];
          const obj = statusObj[status] || { title: status, class: 'bg-label-secondary' };
          return '<span class="badge rounded-pill ' + obj.class + ' text-capitalize">' + obj.title + '</span>';
        }
      },
      {
        // Actions
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
              <a href="javascript:;" class="dropdown-item">Block</a>
            </div>
          </div>`
      }
    ],
    select: {
      style: 'multi',
      selector: 'td:nth-child(2)'
    },
    order: [[2, 'asc']],
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
                      columns: [2, 3, 4, 5],
                      format: {
                        body: function (inner) {
                          if (!inner.length) { return inner; }
                          const el = new DOMParser().parseFromString(inner, 'text/html').body.childNodes;
                          let result = '';
                          el.forEach(item => {
                            if (item.classList && item.classList.contains('user-name')) {
                              result += item.querySelector('span.fw-medium')?.textContent || '';
                            } else {
                              result += item.textContent || item.innerText || '';
                            }
                          });
                          return result;
                        }
                      }
                    },
                    customize: function (win) {
                      win.document.body.style.color = config.colors.headingColor;
                      win.document.body.style.borderColor = config.colors.borderColor;
                      win.document.body.style.backgroundColor = config.colors.bodyBg;
                      const table = win.document.body.querySelector('table');
                      table.classList.add('compact');
                      table.style.color = 'inherit';
                      table.style.borderColor = 'inherit';
                      table.style.backgroundColor = 'inherit';
                    }
                  },
                  {
                    extend: 'csv',
                    text: '<span class="d-flex align-items-center"><i class="icon-base ri ri-file-text-line me-1"></i>Csv</span>',
                    className: 'dropdown-item',
                    exportOptions: { columns: [2, 3, 4, 5] }
                  },
                  {
                    extend: 'excel',
                    text: '<span class="d-flex align-items-center"><i class="icon-base ri ri-file-excel-line me-1"></i>Excel</span>',
                    className: 'dropdown-item',
                    exportOptions: { columns: [2, 3, 4, 5] }
                  },
                  {
                    extend: 'pdf',
                    text: '<span class="d-flex align-items-center"><i class="icon-base ri ri-file-pdf-line me-1"></i>Pdf</span>',
                    className: 'dropdown-item',
                    exportOptions: { columns: [2, 3, 4, 5] }
                  },
                  {
                    extend: 'copy',
                    text: '<i class="icon-base ri ri-file-copy-line me-1"></i>Copy',
                    className: 'dropdown-item',
                    exportOptions: { columns: [2, 3, 4, 5] }
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
              placeholder: 'Search User',
              text: '_INPUT_'
            }
          },
          {
            buttons: [
              {
                text: '<i class="icon-base ri ri-add-line icon-sm me-0 me-sm-2 d-sm-none d-inline-block"></i><span class="d-inline-block">Add New User</span>',
                className: 'add-new btn btn-primary',
                attr: {
                  'data-bs-toggle': 'offcanvas',
                  'data-bs-target': '#offcanvasAddUser'
                }
              }
            ]
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
          const div = document.createElement('div');
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

      // Role filter (column 4 — raw data is the role string)
      const roleContainer = document.querySelector('.user_role');
      if (roleContainer) {
        const roleSelect = document.createElement('select');
        roleSelect.id = 'UserRole';
        roleSelect.className = 'form-select text-capitalize';
        roleSelect.innerHTML = '<option value="">Select Role</option>';
        roleContainer.appendChild(roleSelect);
        roleSelect.addEventListener('change', () => {
          const val = roleSelect.value ? `^${roleSelect.value}$` : '';
          api.column(4).search(val, true, false).draw();
        });
        Array.from(new Set(api.column(4).data().toArray())).sort().forEach(d => {
          const opt = document.createElement('option');
          opt.value = d;
          opt.textContent = d.charAt(0).toUpperCase() + d.slice(1);
          roleSelect.appendChild(opt);
        });
      }

      // Status filter (column 5)
      const statusContainer = document.querySelector('.user_status');
      if (statusContainer) {
        const statusSelect = document.createElement('select');
        statusSelect.id = 'UserStatus';
        statusSelect.className = 'form-select text-capitalize';
        statusSelect.innerHTML = '<option value="">Select Status</option>';
        statusContainer.appendChild(statusSelect);
        statusSelect.addEventListener('change', () => {
          // Search against rendered text (badge label)
          const val = statusSelect.value ? `^${statusSelect.value}$` : '';
          api.column(5).search(val, true, false).draw();
        });
        Array.from(new Set(api.column(5).data().toArray())).sort().forEach(d => {
          const obj = statusObj[d] || { title: d };
          const opt = document.createElement('option');
          opt.value = obj.title;
          opt.textContent = obj.title;
          statusSelect.appendChild(opt);
        });
      }
    }
  });

  // Delete row (client-side only for now)
  function deleteRecord(event) {
    let row = document.querySelector('.dtr-expanded');
    if (event) { row = event.target.closest('tr'); }
    if (row) { dt_user.row(row).remove().draw(); }
  }

  function bindDeleteEvent() {
    const table = document.querySelector('.datatables-users');
    const modal = document.querySelector('.dtr-bs-modal');

    if (table && table.classList.contains('collapsed')) {
      modal?.addEventListener('click', function (e) {
        if (e.target.closest('.delete-record')) {
          deleteRecord();
          modal.querySelector('.btn-close')?.click();
        }
      });
    } else {
      table?.querySelector('tbody')?.addEventListener('click', function (e) {
        if (e.target.closest('.delete-record')) { deleteRecord(e); }
      });
    }
  }

  bindDeleteEvent();
  document.addEventListener('show.bs.modal', e => { if (e.target.classList.contains('dtr-bs-modal')) { bindDeleteEvent(); } });
  document.addEventListener('hide.bs.modal', e => { if (e.target.classList.contains('dtr-bs-modal')) { bindDeleteEvent(); } });

  // Layout tweaks (same as template)
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

  // Add New User — phone mask & form validation
  const phoneMaskList = document.querySelectorAll('.phone-mask');
  const addNewUserForm = document.getElementById('addNewUserForm');

  if (phoneMaskList.length) {
    phoneMaskList.forEach(phoneMask => {
      phoneMask.addEventListener('input', e => {
        const clean = e.target.value.replace(/\D/g, '');
        phoneMask.value = formatGeneral(clean, { blocks: [3, 3, 4], delimiters: [' ', ' '] });
      });
      registerCursorTracker({ input: phoneMask, delimiter: ' ' });
    });
  }

  if (addNewUserForm) {
    FormValidation.formValidation(addNewUserForm, {
      fields: {
        userName: {
          validators: {
            notEmpty: { message: 'Please enter a full name' }
          }
        },
        userEmail: {
          validators: {
            notEmpty:     { message: 'Please enter an email address' },
            emailAddress: { message: 'The value is not a valid email address' }
          }
        }
      },
      plugins: {
        trigger:      new FormValidation.plugins.Trigger(),
        bootstrap5:   new FormValidation.plugins.Bootstrap5({ eleValidClass: '', rowSelector: () => '.form-control-validation' }),
        submitButton: new FormValidation.plugins.SubmitButton(),
        autoFocus:    new FormValidation.plugins.AutoFocus()
      }
    });
  }
});
