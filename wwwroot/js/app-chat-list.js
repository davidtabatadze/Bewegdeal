/**
 * Chats List — Bewegdeal
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const dt_chat_table = document.querySelector('.datatables-chats');

    // Status → icon HTML (tooltip style, like Role column in user list)
    const statusBadgeObj = {
        ongoing:   '<i class="icon-base ri ri-wechat-line       icon-22px text-info    me-2"></i>',
        agreed:    '<i class="icon-base ri ri-shake-hands-line  icon-22px text-success me-2"></i>',
        cancelled: '<i class="icon-base ri ri-hand              icon-22px text-danger  me-2"></i>'
    };

    // Fraud → badge color (like Status column in user list)
    const fraudObj = {
        safe:     { title: 'Safe',     class: 'bg-label-success' },
        dubious:  { title: 'Dubious',  class: 'bg-label-warning' },
        resolved: { title: 'Resolved', class: 'bg-label-info'    }
    };

    // Column index → sort field name
    // 0: requestId, 1: id, 2: status, 3: fraud, 4: customer (n/a), 5: company (n/a), 6: createDate
    const columnToField = { 0: 'requestId', 1: 'id', 2: 'status', 3: 'fraud', 6: 'createDate' };

    if (!dt_chat_table) { return; }

    const dt_chat = new DataTable(dt_chat_table, {
        serverSide: true,
        scrollX: true,
        ajax: {
            url: '/Chat/LoadChats',
            data: function (d) {
                const order = d.order && d.order[0];
                d.sortField     = columnToField[order ? order.column : 6] || 'createDate';
                d.sortDirection = order ? order.dir : 'desc';

                delete d.order;
                delete d.columns;
                delete d.search;

                d.search = document.getElementById('chatsSearch').value;
                d.status = document.getElementById('filterStatus').value;
                d.fraud  = document.getElementById('filterFraud').value;

                return d;
            }
        },
        columns: [
            { data: 'requestId'  },  // 0 — request id (sortable)
            { data: 'id'         },  // 1 — chat id (sortable)
            { data: 'status'     },  // 2 — status (sortable)
            { data: 'fraud'      },  // 3 — fraud (sortable)
            { data: 'customer'   },  // 4 — customer (not sortable)
            { data: 'company'    },  // 5 — company (not sortable)
            { data: 'createDate' },  // 6 — date (sortable, default desc)
        ],
        columnDefs: [
            {
                // Request ID
                targets: 0,
                width: '110px',
                render: (data) => '<span class="fw-medium">#' + data + '</span>'
            },
            {
                // Chat ID
                targets: 1,
                width: '90px',
                render: (data) => '<span class="fw-medium">#' + data + '</span>'
            },
            {
                // Status — icon + tooltip (like Role column)
                targets: 2,
                width: '70px',
                render: function (data, type, full) {
                    const status = full['status'];
                    const icon   = statusBadgeObj[status] || '';
                    const label  = status ? (status.charAt(0).toUpperCase() + status.slice(1)) : status;
                    return "<span data-bs-toggle='tooltip' data-bs-placement='top' title='" + label + "'>" + icon + '</span>';
                }
            },
            {
                // Fraud — colored badge
                targets: 3,
                width: '110px',
                render: function (data, type, full) {
                    const fraud = full['fraud'];
                    const obj   = fraudObj[fraud] || { title: fraud, class: 'bg-label-secondary' };
                    return '<span class="badge ' + obj.class + '">' + obj.title + '</span>';
                }
            },
            {
                // Customer — avatar + name (like User column)
                targets: 4,
                orderable: false,
                render: function (data, type, full) {
                    return renderUserCell(full['customer']);
                }
            },
            {
                // Company — avatar + name (like User column)
                targets: 5,
                orderable: false,
                render: function (data, type, full) {
                    return renderUserCell(full['company']);
                }
            },
            {
                // Date
                targets: 6,
                width: '135px',
                createdCell: function (td) { td.style.minWidth = '135px'; },
                render: (data) => '<span class="text-muted small">' + (data || '—') + '</span>'
            }
        ],
        pageLength: 10,
        order: [[6, 'desc']],
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
                next:     '<i class="icon-base ri ri-arrow-right-s-line scaleX-n1-rtl icon-22px"></i>',
                previous: '<i class="icon-base ri ri-arrow-left-s-line  scaleX-n1-rtl icon-22px"></i>',
                first:    '<i class="icon-base ri ri-skip-back-mini-line    scaleX-n1-rtl icon-22px"></i>',
                last:     '<i class="icon-base ri ri-skip-forward-mini-line scaleX-n1-rtl icon-22px"></i>'
            }
        },
        responsive: false
    });

    // Loading indicator
    Block.pulse('.card-datatable');

    dt_chat.on('preXhr.dt', function () { Block.pulse('.card-datatable'); });
    dt_chat.on('xhr.dt',    function () { Block.remove('.card-datatable'); });

    // Filters
    let searchTimeout;
    document.getElementById('chatsSearch').addEventListener('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () { dt_chat.ajax.reload(null, true); }, 500);
    });

    document.getElementById('filterStatus').addEventListener('change', function () {
        dt_chat.ajax.reload(null, true);
    });

    document.getElementById('filterFraud').addEventListener('change', function () {
        dt_chat.ajax.reload(null, true);
    });

    // Layout tweaks (same as template)
    setTimeout(function () {
        [
            { selector: '.dt-buttons .btn',        classToRemove: 'btn-secondary' },
            { selector: '.dt-length .form-select', classToAdd: 'ms-0' },
            { selector: '.dt-length',              classToAdd: 'mb-md-4 mb-0' },
            { selector: '.dt-layout-end',          classToRemove: 'justify-content-between', classToAdd: 'd-flex gap-md-4 justify-content-md-between justify-content-center gap-md-2 flex-wrap mt-0' },
            { selector: '.dt-layout-start',        classToAdd: 'mt-md-0 mt-5' },
            { selector: '.dt-layout-start .dt-buttons', classToAdd: 'd-md-flex d-block gap-4 justify-content-center' },
            { selector: '.dt-layout-end .dt-buttons',   classToAdd: 'd-md-flex d-block gap-4 mb-md-0 mb-5 justify-content-center' },
            { selector: '.dt-layout-table',        classToRemove: 'row mt-2' },
            { selector: '.dt-layout-full',         classToRemove: 'col-md col-12' },
            { selector: '.dt-layout-full .table',  classToAdd: 'table-responsive' }
        ].forEach(function ({ selector, classToRemove, classToAdd }) {
            document.querySelectorAll(selector).forEach(function (el) {
                if (classToRemove) { classToRemove.split(' ').forEach(function (c) { el.classList.remove(c); }); }
                if (classToAdd)    { classToAdd.split(' ').forEach(function (c) { el.classList.add(c); }); }
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
