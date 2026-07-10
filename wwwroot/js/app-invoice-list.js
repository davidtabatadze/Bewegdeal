/**
 * Invoices List — Bewegdeal
 * v1.0.8
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const dt_invoice_table = document.querySelector('.datatables-invoices');

    // Status → icon HTML
    const statusMap = {
        pending: { icon: 'ri-timer-flash-line', color: 'warning' },
        paid: { icon: 'ri-wallet-line', color: 'success' },
        cancelled: { icon: 'ri-hand', color: 'danger' }
    };

    // Column index → sort field name
    // 0: status, 1: user (n/a), 2: serviceCost, 3: totalCost, 4: createDate, 5: requestId, 6: id
    const columnToField = { 0: 'status', 2: 'serviceCost', 3: 'totalCost', 4: 'createDate', 5: 'requestId', 6: 'id' };

    if (!dt_invoice_table) { return; }

    const isAdmin = dt_invoice_table.querySelectorAll('thead th').length === 8;

    const dt_invoice = new DataTable(dt_invoice_table, {
        serverSide: true,
        scrollX: true,
        ajax: {
            url: '/Invoice/LoadInvoices',
            data: function (d) {
                const order = d.order && d.order[0];
                d.sortField = columnToField[order ? order.column : 0] || 'status';
                d.sortDirection = order ? order.dir : 'desc';

                delete d.order;
                delete d.columns;
                delete d.search;

                d.search = document.getElementById('invoicesSearch').value;
                d.status = document.getElementById('filterStatus').value;
                d.amountFrom = document.getElementById('amountFrom').value || null;
                d.amountTo = document.getElementById('amountTo').value || null;

                return d;
            }
        },
        columns: [
            { data: 'status' },       // 0 — status (sortable, default desc)
            { data: 'user' },         // 1 — user (not sortable)
            { data: 'serviceCost' },  // 2 — cost (sortable)
            { data: 'totalCost' },    // 3 — fee (sortable)
            { data: 'createDate' },   // 4 — date (sortable)
            { data: 'requestId' },    // 5 — request (sortable)
            { data: 'id' },           // 6 — invoice (sortable)
            ...(isAdmin ? [{ data: 'status' }] : [])  // 7 — actions (admin only)
        ],
        columnDefs: [
            {
                // Status — icon + tooltip
                targets: 0,
                width: '60px',
                render: function (data, type, full) {
                    const status = full['status'];
                    const label = status ? (status.charAt(0).toUpperCase() + status.slice(1)) : status;
                    const map = statusMap[status];
                    return (
                        '<ul class="list-unstyled m-0 avatar-group d-flex align-items-center">' +
                        '<li class="avatar avatar-m" data-bs-toggle="tooltip" data-bs-placement="top" title="' + label + '">' +
                        '<div class="avatar-initial rounded-circle bg-label-' + map.color + '">' +
                        '<i class="icon-base ri ' + map.icon + ' icon-m"></i>' +
                        '</div>' +
                        '</li>' +
                        '</ul>'
                    );
                }
            },
            {
                // User — avatar + name
                targets: 1,
                orderable: false,
                render: function (data, type, full) {
                    return renderUserCell(full['user']);
                }
            },
            {
                // Cost — serviceCost EUR
                targets: 2,
                width: '120px',
                render: function (data) {
                    return '<span class="text-heading fw-medium">' + (data != null ? '€' + data : '—') + '</span>';
                }
            },
            {
                // Fee — totalCost EUR
                targets: 3,
                width: '120px',
                render: function (data) {
                    return '<span class="text-heading fw-medium">' + (data != null ? '€' + data : '—') + '</span>';
                }
            },
            {
                // Date — invoice create date
                targets: 4,
                width: '135px',
                createdCell: function (td) { td.style.minWidth = '135px'; },
                render: function (data, type, full) {
                    const create = !data ? '-' :
                        new Date(data).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
                    const due = !full['dueDate'] ? '-' :
                        new Date(full['dueDate']).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

                    return (
                        '<div class="d-flex flex-column">' +
                        '<span class="fw-medium">' + create + '</span>' +
                        '<small class="text-danger">' + due + '</small>' +
                        '</div>'
                    );
                }
            },
            {
                // Request
                targets: 5,
                width: '110px',
                render: function (data, type, full) {
                    const id = full['requestId'];
                    const number = full['requestNumber'];
                    return (
                        '<a style="max-width:100px" class="text-primary" href=\'/Request/View?number=' + encodeURIComponent(number) + '\'">' +
                        '<strong class="text-decoration-underline">#' + id + '</strong>' +
                        '</a>'
                    );
                }
            },
            {
                // Invoice
                targets: 6,
                width: '110px',
                render: function (data, type, full) {
                    const id = full['id'];
                    const number = full['number'];
                    return (
                        '<a style="max-width:100px" class="text-primary" target="_blank" href=\'/Invoice/Print?number=' + number + '\'>' +
                        '<strong class="text-decoration-underline">#' + id + '</strong>' +
                        '</a>'
                    );
                }
            },
            ...(isAdmin ? [{
                // Actions
                targets: 7,
                width: '90px',
                orderable: false,
                searchable: false,
                render: function (data, type, full) {
                    const id = full['id'];
                    const paid = data === 'paid' ? '' : (
                        '<button type="button" class="btn btn-icon btn-label-success invoice-status-btn me-1"' +
                        ' data-invoice-id="' + id + '" data-new-status="paid"' +
                        ' data-bs-toggle="tooltip" data-bs-placement="top" title="Paid">' +
                        '<span class="icon-base ri ri-wallet-line icon-22px text-success"></span>' +
                        '</button>'
                    );
                    const cancelled = data === 'cancelled' ? '' : (
                        '<button type="button" class="btn btn-icon btn-label-danger invoice-status-btn"' +
                        ' data-invoice-id="' + id + '" data-new-status="cancelled"' +
                        ' data-bs-toggle="tooltip" data-bs-placement="top" title="Cancel">' +
                        '<span class="icon-base ri ri-hand icon-22px text-danger"></span>' +
                        '</button>'
                    );
                    return '<div class="d-flex align-items-center">' + paid + cancelled + '</div>';
                }
            }] : [])
        ],
        pageLength: 10,
        order: [[4, 'desc']],
        drawCallback: function () {
            document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
                if (!bootstrap.Tooltip.getInstance(el)) {
                    new bootstrap.Tooltip(el);
                }
            });
        },
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
                next: '<i class="icon-base ri ri-arrow-right-s-line scaleX-n1-rtl icon-22px"></i>',
                previous: '<i class="icon-base ri ri-arrow-left-s-line  scaleX-n1-rtl icon-22px"></i>',
                first: '<i class="icon-base ri ri-skip-back-mini-line    scaleX-n1-rtl icon-22px"></i>',
                last: '<i class="icon-base ri ri-skip-forward-mini-line scaleX-n1-rtl icon-22px"></i>'
            }
        },
        responsive: false
    });

    // Loading indicator
    Block.pulse('.card-datatable');

    dt_invoice.on('preXhr.dt', function () { Block.pulse('.card-datatable'); });
    dt_invoice.on('xhr.dt', function () { Block.remove('.card-datatable'); });

    // Filters
    let searchTimeout;
    document.getElementById('invoicesSearch').addEventListener('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () { dt_invoice.ajax.reload(null, true); }, 500);
    });

    document.getElementById('filterStatus').addEventListener('change', function () {
        dt_invoice.ajax.reload(null, true);
    });

    let amountTimeout;
    document.getElementById('amountFrom').addEventListener('input', function () {
        clearTimeout(amountTimeout);
        amountTimeout = setTimeout(function () { dt_invoice.ajax.reload(null, true); }, 500);
    });

    document.getElementById('amountTo').addEventListener('input', function () {
        clearTimeout(amountTimeout);
        amountTimeout = setTimeout(function () { dt_invoice.ajax.reload(null, true); }, 500);
    });

    // Status change — confirm → POST → row update (admin only)
    if (isAdmin) {
        const confirmTextMap = {
            paid: 'Sure you want to mark the invoice as <span class="text-success fw-bold">Paid</span>?',
            cancelled: 'Sure you want to <span class="text-danger fw-bold">Cancel</span> the invoice?'
        };

        dt_invoice_table.addEventListener('click', function (e) {
            const btn = e.target.closest('.invoice-status-btn');
            if (!btn) { return; }

            const invoiceId = btn.dataset.invoiceId;
            const newStatus = btn.dataset.newStatus;
            const confirmHtml = confirmTextMap[newStatus];
            const dtRow = dt_invoice.row(btn.closest('tr'));

            if (!confirmHtml) { return; }

            Swal.fire({
                title: 'Confirm Action',
                html: confirmHtml,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Yes, confirm',
                cancelButtonText: 'Cancel',
                customClass: {
                    confirmButton: 'btn btn-primary me-3',
                    cancelButton: 'btn btn-label-secondary'
                },
                buttonsStyling: false
            }).then(function (result) {
                if (!result.isConfirmed) { return; }

                Block.pulse('.card-datatable');

                fetch('/Invoice/UpdateInvoiceStatus', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: 'id=' + encodeURIComponent(invoiceId) + '&status=' + encodeURIComponent(newStatus)
                }).then(function (res) {
                    if (res.ok) {
                        res.json().then(function (body) {
                            const rowData = dtRow.data();
                            rowData.status = body.status;
                            dtRow.data(rowData).draw(false);
                        });
                        Swal.fire({
                            title: 'Done!',
                            text: 'Invoice status has been updated.',
                            icon: 'success',
                            customClass: { confirmButton: 'btn btn-primary' },
                            buttonsStyling: false
                        });
                    } else {
                        Block.remove('.card-datatable');
                        Swal.fire({ title: 'Error', text: 'Failed to update invoice status.', icon: 'error', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
                    }
                });
            });
        });
    }

    // Layout tweaks (same as template)
    setTimeout(function () {
        [
            { selector: '.dt-buttons .btn', classToRemove: 'btn-secondary' },
            { selector: '.dt-length .form-select', classToAdd: 'ms-0' },
            { selector: '.dt-length', classToAdd: 'mb-md-4 mb-0' },
            { selector: '.dt-layout-end', classToRemove: 'justify-content-between', classToAdd: 'd-flex gap-md-4 justify-content-md-between justify-content-center gap-md-2 flex-wrap mt-0' },
            { selector: '.dt-layout-start', classToAdd: 'mt-md-0 mt-5' },
            { selector: '.dt-layout-start .dt-buttons', classToAdd: 'd-md-flex d-block gap-4 justify-content-center' },
            { selector: '.dt-layout-end .dt-buttons', classToAdd: 'd-md-flex d-block gap-4 mb-md-0 mb-5 justify-content-center' },
            { selector: '.dt-layout-table', classToRemove: 'row mt-2' },
            { selector: '.dt-layout-full', classToRemove: 'col-md col-12' },
            { selector: '.dt-layout-full .table', classToAdd: 'table-responsive' }
        ].forEach(function ({ selector, classToRemove, classToAdd }) {
            document.querySelectorAll(selector).forEach(function (el) {
                if (classToRemove) { classToRemove.split(' ').forEach(function (c) { el.classList.remove(c); }); }
                if (classToAdd) { classToAdd.split(' ').forEach(function (c) { el.classList.add(c); }); }
            });
        });
    }, 100);

    function renderUserCell(avatar) {
        if (!avatar) { return '—'; }
        const avatarInner = avatar.url
            ? '<img src="' + avatar.url + '" class="rounded-circle" style="width:100%;height:100%;object-fit:cover;" />'
            : '<span class="avatar-initial rounded-circle bg-label-primary">' + (avatar.initials || '') + '</span>';
        return (
            '<div class="d-flex justify-content-start align-items-center user-name">' +
            '<div class="avatar-wrapper">' +
            '<div class="avatar avatar-m me-2">' + avatarInner + '</div>' +
            '</div>' +
            '<div class="d-flex flex-column">' +
            '<span class="text-heading fw-medium text-truncate">' + (avatar.name || '—') + '</span>' +
            '</div>' +
            '</div>'
        );
    }
});
