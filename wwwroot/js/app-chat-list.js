/**
 * Chats List — Bewegdeal
 * v1.0.1
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const dt_chat_table = document.querySelector('.datatables-chats');

    // Status → icon HTML (tooltip style, like Role column in user list)
    const statusMap = {
        ongoing: { icon: 'ri-wechat-line', color: 'info' },
        agreed: { icon: 'ri-shake-hands-line', color: 'success' },
        cancelled: { icon: 'ri-hand', color: 'danger' }
    };

    // Fraud → badge color (like Status column in user list)
    const fraudObj = {
        safe: { title: 'Safe', class: 'btn-text-success' },
        dubious: { title: 'Dubious', class: 'btn-text-warning' },
        resolved: { title: 'Resolved', class: 'btn-text-info' }
    };

    // Column index → sort field name
    // 0: fraud, 1: status, 2: company (n/a), 3: customer (n/a), 4: createDate, 5: requestId, 6: id
    const columnToField = { 0: 'fraud', 1: 'status', 4: 'createDate', 5: 'requestId', 6: 'id' };

    if (!dt_chat_table) { return; }

    const dt_chat = new DataTable(dt_chat_table, {
        serverSide: true,
        scrollX: true,
        ajax: {
            url: '/Chat/LoadChats',
            data: function (d) {
                const order = d.order && d.order[0];
                d.sortField = columnToField[order ? order.column : 6] || 'createDate';
                d.sortDirection = order ? order.dir : 'desc';

                delete d.order;
                delete d.columns;
                delete d.search;

                d.search = document.getElementById('chatsSearch').value;
                d.status = document.getElementById('filterStatus').value;
                d.fraud = document.getElementById('filterFraud').value;

                return d;
            }
        },
        columns: [
            { data: 'fraud' },  // 0 — fraud (sortable)
            { data: 'status' },  // 1 — status (sortable)
            { data: 'company' },  // 2 — company (not sortable)
            { data: 'customer' },  // 3 — customer (not sortable)
            { data: 'createDate' },  // 4 — date (sortable, default desc)
            { data: 'requestId' },  // 5 — request id (sortable)
            { data: 'id' },  // 6 — chat id (sortable)
        ],
        columnDefs: [
            {
                // Fraud — button
                targets: 0,
                width: '110px',
                render: function (data, type, full) {
                    const fraud = full['fraud'];
                    const obj = fraudObj[fraud] || { title: fraud, class: 'btn-text-secondary' };
                    const id = full['id'];

                    if (fraud !== 'dubious') {
                        return (
                            '<button type="button" class="btn ' + obj.class + '" style="pointer-events:none">' +
                            obj.title +
                            '</button>'
                        );
                    }

                    return (
                        '<button type="button" class="btn ' + obj.class + ' fraud-toggle-btn"' +
                        ' data-chat-id="' + id + '"' +
                        ' data-current-fraud="' + fraud + '">' +
                        '<span class="icon-base ri ri-exchange-line icon-16px me-1_5"></span>' +
                        obj.title +
                        '</button>'
                    );
                }
            },
            {
                // Status — icon + tooltip (like Role column)
                targets: 1,
                width: '70px',
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
                // Company — avatar + name (like User column)
                targets: 2,
                orderable: false,
                render: function (data, type, full) {
                    return renderUserCell(full['company'], full['companyEmail']);
                }
            },
            {
                // Customer — avatar + name (like User column)
                targets: 3,
                orderable: false,
                render: function (data, type, full) {
                    return renderUserCell(full['customer'], full['customerEmail']);
                }
            },
            {
                // Date
                targets: 4,
                width: '135px',
                createdCell: function (td) { td.style.minWidth = '135px'; },
                render: (data) => '<span class="text-muted small">' + (data || '—') + '</span>'
            },
            {
                // Request
                targets: 5,
                width: '110px',
                render: function (data, type, full) {
                    const number = full['requestNumber'];
                    return (
                        '<a style="max-width:100px" class="text-primary" href=\'/Request/View?number=' + encodeURIComponent(number) + '\'">' +
                        '<strong class="text-decoration-underline">#' + data + '</strong>' +
                        '</a>'
                    );
                }
            },
            {
                // Chat
                targets: 6,
                width: '110px',
                render: function (data, type, full) {
                    return (
                        '<a style="max-width:100px" href="javascript:void(0);" class="text-primary chat-view-btn" data-chat-key="' + full['key'] + '">' +
                        '<strong class="text-decoration-underline">#' + data + '</strong>' +
                        '</a>'
                    );
                }
            }
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

    dt_chat.on('preXhr.dt', function () { Block.pulse('.card-datatable'); });
    dt_chat.on('xhr.dt', function () { Block.remove('.card-datatable'); });

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

    // Status change — delegated click on the table wrapper
    const confirmTextMap = {
        dubious: 'Sure you want to change fraud to <span class="text-info fw-medium">Resolved</span>?'
    };

    // Fraud toggle — delegated click on the table wrapper
    dt_chat_table.addEventListener('click', function (e) {
        const btn = e.target.closest('.fraud-toggle-btn');
        if (!btn) { return; }

        const chatId = btn.dataset.chatId;
        const currentFraud = btn.dataset.currentFraud;
        const confirmHtml = confirmTextMap[currentFraud];
        const dtRow = dt_chat.row(btn.closest('tr'));

        Swal.fire({
            title: 'Confirm Action',
            html: confirmHtml,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, resolve it',
            cancelButtonText: 'Cancel',
            customClass: {
                confirmButton: 'btn btn-primary me-3',
                cancelButton: 'btn btn-label-secondary'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (!result.isConfirmed) { return; }

            fetch('/Chat/UpdateChatFraud', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'id=' + encodeURIComponent(chatId) + '&fraud=' + encodeURIComponent(currentFraud)
            }).then(function (res) {
                if (res.ok) {
                    res.json().then(function (body) {
                        const rowData = dtRow.data();
                        rowData.fraud = body.fraud;
                        dtRow.data(rowData).draw(false);
                    });
                    Swal.fire({
                        title: 'Done!',
                        text: 'Chat fraud status has been resolved.',
                        icon: 'success',
                        customClass: { confirmButton: 'btn btn-primary' },
                        buttonsStyling: false
                    });
                } else {
                    Swal.fire({ title: 'Error', text: 'Failed to update fraud status.', icon: 'error', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
                }
            });
        });
    });

    // Chat view — open offcanvas with conversation history
    dt_chat_table.addEventListener('click', function (e) {
        const btn = e.target.closest('.chat-view-btn');
        if (!btn) { return; }

        const key = btn.dataset.chatKey;
        const offcanvasEl = document.getElementById('adminChatOffcanvas');
        const body = document.getElementById('adminChatOffcanvasBody');

        body.innerHTML = '';
        bootstrap.Offcanvas.getOrCreateInstance(offcanvasEl).show();
        Block.pulse('#adminChatOffcanvasBody');

        fetch('/Chat/Conversation?key=' + encodeURIComponent(key))
            .then(function (res) { return res.text(); })
            .then(function (html) {
                Block.remove('#adminChatOffcanvasBody');
                body.innerHTML = html;
                const histBody = body.querySelector('.chat-history-body');
                if (histBody) { new PerfectScrollbar(histBody); }
            })
            .catch(function () { Block.remove('#adminChatOffcanvasBody'); });
    });

    function renderUserCell(avatar, email) {
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
            (email ? '<small class="text-muted">' + email + '</small>' : '') +
            '</div>' +
            '</div>'
        );
    }
});
