/**
 * Fraud Dictionary — Bewegdeal
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  const dt_table = document.querySelector('.datatables-fraud-words');
  if (!dt_table) { return; }

  const statusObj = {
    active:   { title: 'Active',   class: 'btn-text-success' },
    disabled: { title: 'Disabled', class: 'btn-text-danger'  }
  };

  const dt = new DataTable(dt_table, {
    serverSide: true,
    scrollX: true,
    ajax: {
      url: '/FraudWord/LoadWords',
      data: function (d) {
        delete d.order;
        delete d.columns;
        delete d.search;

        d.search = document.getElementById('fraudSearch').value;
        d.status = document.getElementById('filterStatus').value;

        return d;
      }
    },
    columns: [
      { data: 'word'          },   // 0
      { data: 'description'   },   // 1
      { data: 'status'        },   // 2
      { data: 'createdAt'     },   // 3
      { data: 'createdByName' },   // 4
      { data: null            },   // 5 — actions
    ],
    columnDefs: [
      {
        targets: 2,
        orderable: false,
        render: function (data, type, full) {
          const obj = statusObj[full.status] || { title: full.status, class: 'btn-text-secondary' };
          return '<span class="badge ' + (full.status === 'active' ? 'bg-label-success' : 'bg-label-danger') + '">' + obj.title + '</span>';
        }
      },
      {
        targets: 5,
        orderable: false,
        searchable: false,
        render: function (data, type, full) {
          const isActive    = full.status === 'active';
          const toggleLabel = isActive ? 'Turn Off' : 'Turn On';
          const toggleClass = isActive ? 'btn-text-warning' : 'btn-text-success';
          const toggleIcon  = isActive ? 'ri-forbid-line' : 'ri-play-circle-line';

          return (
            '<div class="d-flex gap-2 justify-content-center">' +
              '<button class="btn btn-sm ' + toggleClass + ' toggle-status-btn"' +
                ' data-id="' + full.id + '" data-status="' + full.status + '">' +
                '<i class="icon-base ri ' + toggleIcon + ' icon-16px me-1"></i>' + toggleLabel +
              '</button>' +
              '<button class="btn btn-sm btn-text-primary edit-btn"' +
                ' data-id="' + full.id + '"' +
                ' data-word="' + encodeURIComponent(full.word) + '"' +
                ' data-description="' + encodeURIComponent(full.description) + '">' +
                '<i class="icon-base ri ri-edit-line icon-16px me-1"></i>Edit' +
              '</button>' +
              '<button class="btn btn-sm btn-text-danger delete-btn" data-id="' + full.id + '">' +
                '<i class="icon-base ri ri-delete-bin-line icon-16px me-1"></i>Delete' +
              '</button>' +
            '</div>'
          );
        }
      },
      { targets: [0, 1, 3, 4], orderable: false }
    ],
    pageLength: 10,
    layout: {
      topStart: null,
      topEnd: null,
      bottomStart: {
        rowClass: 'row mx-3 justify-content-between',
        features: ['info']
      },
      bottomEnd: 'paging'
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
    responsive: false
  });

  // Loading indicator
  dt.on('preXhr.dt', function () { Block.pulse('.card-datatable'); });
  dt.on('xhr.dt',    function () { Block.remove('.card-datatable'); });

  // Filters
  let searchTimeout;
  document.getElementById('fraudSearch').addEventListener('input', function () {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(function () { dt.ajax.reload(null, true); }, 500);
  });
  document.getElementById('filterStatus').addEventListener('change', function () {
    dt.ajax.reload(null, true);
  });

  // Toggle status
  dt_table.addEventListener('click', function (e) {
    const btn = e.target.closest('.toggle-status-btn');
    if (!btn) { return; }

    const id     = btn.dataset.id;
    const status = btn.dataset.status;
    const action = status === 'active' ? 'turn off' : 'turn on';

    Swal.fire({
      title: 'Confirm',
      html: 'Sure you want to <strong>' + action + '</strong> this word?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes',
      cancelButtonText: 'Cancel',
      customClass: { confirmButton: 'btn btn-primary me-3', cancelButton: 'btn btn-label-secondary' },
      buttonsStyling: false
    }).then(function (result) {
      if (!result.isConfirmed) { return; }

      fetch('/FraudWord/ToggleStatus', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'id=' + encodeURIComponent(id)
      }).then(function (res) {
        if (res.ok) {
          dt.ajax.reload(null, false);
        } else {
          Swal.fire({ title: 'Error', text: 'Failed to update status.', icon: 'error', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
        }
      });
    });
  });

  // Edit
  const editModal       = new bootstrap.Modal(document.getElementById('editFraudWordModal'));
  const editWordIdInput = document.getElementById('editWordId');
  const editWordInput   = document.getElementById('editWord');
  const editDescInput   = document.getElementById('editDescription');

  dt_table.addEventListener('click', function (e) {
    const btn = e.target.closest('.edit-btn');
    if (!btn) { return; }

    editWordIdInput.value  = btn.dataset.id;
    editWordInput.value    = decodeURIComponent(btn.dataset.word);
    editDescInput.value    = decodeURIComponent(btn.dataset.description);
    editModal.show();
  });

  document.getElementById('saveEditBtn').addEventListener('click', function () {
    const id          = editWordIdInput.value;
    const word        = editWordInput.value.trim();
    const description = editDescInput.value.trim();

    if (!word) {
      editWordInput.classList.add('is-invalid');
      return;
    }
    editWordInput.classList.remove('is-invalid');

    fetch('/FraudWord/Edit', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: 'id=' + encodeURIComponent(id) +
            '&word=' + encodeURIComponent(word) +
            '&description=' + encodeURIComponent(description)
    }).then(function (res) {
      if (res.ok) {
        editModal.hide();
        dt.ajax.reload(null, false);
      } else {
        Swal.fire({ title: 'Error', text: 'Failed to save changes.', icon: 'error', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
      }
    });
  });

  // Delete
  dt_table.addEventListener('click', function (e) {
    const btn = e.target.closest('.delete-btn');
    if (!btn) { return; }

    const id = btn.dataset.id;

    Swal.fire({
      title: 'Delete Fraud Word?',
      text: 'This action cannot be undone.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Delete',
      cancelButtonText: 'Cancel',
      customClass: { confirmButton: 'btn btn-danger me-3', cancelButton: 'btn btn-label-secondary' },
      buttonsStyling: false
    }).then(function (result) {
      if (!result.isConfirmed) { return; }

      fetch('/FraudWord/Delete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'id=' + encodeURIComponent(id)
      }).then(function (res) {
        if (res.ok) {
          dt.ajax.reload(null, true);
          Swal.fire({ title: 'Deleted!', text: 'Fraud word removed.', icon: 'success', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
        } else {
          Swal.fire({ title: 'Error', text: 'Failed to delete.', icon: 'error', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
        }
      });
    });
  });

  // Layout tweaks
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
