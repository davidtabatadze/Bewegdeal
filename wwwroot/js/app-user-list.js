/**
 * Users List — Bewegdeal
 * v1.0.3
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const dt_user_table = document.querySelector('.datatables-users');

    // Status → icon + color + label
    const us = window.userStatusLabels || {};
    const statusMap = {
        active: { title: us.active, icon: 'ri-user-follow-line', color: 'success' },
        pending: { title: us.pending, icon: 'ri-user-forbid-line', color: 'warning' },
        blocked: { title: us.blocked, icon: 'ri-admin-line', color: 'danger' },
        unverified: { title: us.unverified, icon: 'ri-user-settings-line', color: 'secondary' }
    };

    // Interest → icon + color + label
    const ui = window.userInterestLabels || {};
    const interestMap = {
        moving: { icon: 'ri-truck-line', color: 'bg-label-success', title: ui.moving },
        removal: { icon: 'ri-recycle-line', color: 'bg-label-danger', title: ui.removal },
        pickup: { icon: 'ri-shopping-bag-4-line', color: 'bg-label-warning', title: ui.pickup },
        transport: { icon: 'ri-car-line', color: 'bg-label-info', title: ui.transport }
    };

    // Role → icon HTML
    const roleMap = {
        customer: { icon: 'ri-user-2-line', color: 'success' },
        company: { icon: 'ri-building-line', color: 'info' },
        administrator: { icon: 'ri-computer-line', color: 'danger' }
    };

    // Column index → sort field name sent to the server (only orderable columns listed)
    const columnToField = { 0: 'status', 1: 'role', 4: 'createDate' };

    if (!dt_user_table) { return; }

    const dt_user = new DataTable(dt_user_table, {
        serverSide: true,
        scrollX: true,
        ajax: {
            url: '/User/LoadUsers',
            data: function (d) {
                // Map DT's nested order to the flat sortField/sortDirection the filter expects
                const order = d.order && d.order[0];
                d.sortField = columnToField[order ? order.column : 5] || 'createDate';
                d.sortDirection = order ? order.dir : 'desc';

                // Remove DT's auto-generated nested params — controller doesn't use them
                delete d.order;
                delete d.columns;
                delete d.search;   // replaced below with our own plain-string value

                // Custom filters
                d.search = document.getElementById('usersSearch').value;
                d.role = document.getElementById('filterRole').value;
                d.status = document.getElementById('filterStatus').value;

                return d;
            }
        },
        columns: [
            { data: 'status' },   // 0 — status (sortable)
            { data: 'role' },   // 1 — role
            { data: 'avatar' },   // 2 — user cell
            { data: 'mobile' },   // 3 — contact
            { data: 'createDate' },   // 4 — date (sortable)
            { data: 'interests' },   // 5 — interests
            { data: null },          // 6 — actions
        ],
        columnDefs: [
            {
                // User cell: avatar initials + name + email
                targets: 2,
                orderable: false,
                responsivePriority: 4,
                render: function (data, type, full) {
                    const name = full['name'];
                    const email = full['email'];
                    const avatar = full['avatar'] || {};

                    const avatarInner = avatar.url
                        ? '<img src="' + avatar.url + '"class="rounded-circle" style="width:100%;height:100%;object-fit:cover;" />'
                        : '<span class="avatar-initial rounded-circle bg-label-primary">' + (avatar.initials || '') + '</span>';
                    const avatarRating = avatar.rating && avatar.rating > 0 ?
                        '<span class="position-absolute top-10 start-100 translate-middle badge badge-center border rounded-pill bg-primary fw-bold">' + avatar.rating + '</span>' :
                        '';

                    return (
                        '<div class="d-flex justify-content-start align-items-center user-name">' +
                        '<div class="avatar-wrapper">' +
                        '<div class="avatar avatar-m me-2">' + avatarInner + avatarRating + '</div>' +
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
                // Contact
                targets: 3,
                orderable: false,
                render: (data, type, full) => {
                    const mobile = full['mobile'] || '—';
                    const address = full['address'] || '—';
                    return (
                        '<div class="d-flex flex-column">' +
                        '<span class="fw-medium">' + mobile + '</span>' +
                        '<small class="text-muted">' + address + '</small>' +
                        '</div>'
                    );
                }
            },
            {
                // Role
                targets: 1,
                width: '60px',
                orderable: true,
                render: function (data, type, full) {
                    const role = full['role'];
                    const label = role ? (role.charAt(0).toUpperCase() + role.slice(1)) : role;
                    const map = roleMap[role];
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
                // Interests
                targets: 5,
                width: '120px',
                orderable: false,
                searchable: false,
                render: function (data, type, full) {
                    const interests = full['interests'];
                    if (!interests || interests.length === 0) { return ''; }
                    const items = interests.map(function (key) {
                        const map = interestMap[key];
                        if (!map) { return ''; }
                        return (
                            '<li class="avatar avatar-m pull-up" data-bs-toggle="tooltip" data-bs-placement="top" title="' + map.title + '">' +
                            '<div class="avatar-initial rounded-circle ' + map.color + '">' +
                            '<i class="icon-base ri ' + map.icon + ' icon-m"></i>' +
                            '</div>' +
                            '</li>'
                        );
                    }).join('');
                    return '<ul class="list-unstyled m-0 avatar-group d-flex align-items-center">' + items + '</ul>';
                }
            },
            {
                // Actions
                targets: 6,
                width: '90px',
                orderable: false,
                searchable: false,
                render: function (data, type, full) {
                    const status = full['status'];
                    const id = full['id'];
                    const actionsConfig = {
                        active: [{ action: 'block', icon: 'ri-admin-line', color: 'danger', title: 'Sperren' }],
                        pending: [{ action: 'activate', icon: 'ri-user-follow-line', color: 'success', title: 'Aktivieren' },
                        { action: 'delete', icon: 'ri-user-unfollow-line', color: 'danger', title: 'Löschen' }],
                        blocked: [{ action: 'activate', icon: 'ri-user-follow-line', color: 'success', title: 'Aktivieren' }],
                        unverified: [{ action: 'delete', icon: 'ri-user-unfollow-line', color: 'danger', title: 'Löschen' }]
                    };
                    const actions = actionsConfig[status] || [];
                    return actions.map(function (a) {
                        return (
                            '<button type="button" class="btn btn-icon btn-label-' + a.color + ' user-action-btn me-1"' +
                            ' data-user-id="' + id + '"' +
                            ' data-action="' + a.action + '"' +
                            ' data-current-status="' + status + '"' +
                            ' data-bs-toggle="tooltip" data-bs-placement="top" title="' + a.title + '">' +
                            '<span class="icon-base ri ' + a.icon + ' icon-22px text-' + a.color + '"></span>' +
                            '</button>'
                        );
                    }).join('');
                }
            },
            {
                // Date
                targets: 4,
                width: '130px',
                orderable: true,
                createdCell: function (td) {
                    td.style.minWidth = '135px';
                },
                render: (data) => '<span class="text-muted small">' + (data || '—') + '</span>'
            },
            {
                // Status icon
                targets: 0,
                width: '60px',
                orderable: true,
                render: function (data, type, full) {
                    const status = full['status'];
                    const map = statusMap[status] || { title: status, icon: 'ri-question-line', color: 'secondary' };
                    return (
                        '<ul class="list-unstyled m-0 avatar-group d-flex align-items-center">' +
                        '<li class="avatar avatar-m" data-bs-toggle="tooltip" data-bs-placement="top" title="' + map.title + '">' +
                        '<div class="avatar-initial rounded-circle bg-label-' + map.color + '">' +
                        '<i class="icon-base ri ' + map.icon + ' icon-m"></i>' +
                        '</div>' +
                        '</li>' +
                        '</ul>'
                    );
                }
            },
        ],
        pageLength: 10,
        order: [[4, 'desc']],
        drawCallback: function () {
            document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
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
        responsive: false,
    });

    // Loading indicator — also covers the initial load
    Block.pulse('.card-datatable');

    dt_user.on('preXhr.dt', function () {
        Block.pulse('.card-datatable');
    });

    dt_user.on('xhr.dt', function () {
        Block.remove('.card-datatable');
    });

    // Filters
    let searchTimeout;
    document.getElementById('usersSearch').addEventListener('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () { dt_user.ajax.reload(null, true); }, 500);
    });

    document.getElementById('filterRole').addEventListener('change', function () {
        dt_user.ajax.reload(null, true);
    });

    document.getElementById('filterStatus').addEventListener('change', function () {
        dt_user.ajax.reload(null, true);
    });

    // Action button — delegated click on the table wrapper
    const confirmTextMap = {
        block: 'Möchten Sie den Benutzer wirklich <span class="text-danger fw-bold">Sperren</span>?',
        activate: 'Möchten Sie den Benutzer wirklich <span class="text-success fw-bold">Aktivieren</span>?',
        delete: 'Möchten Sie den Benutzer wirklich <span class="text-danger fw-bold">Löschen</span>?'
    };

    dt_user_table.addEventListener('click', function (e) {
        const btn = e.target.closest('.user-action-btn');
        if (!btn) { return; }

        const userId = btn.dataset.userId;
        const action = btn.dataset.action;
        const currentStatus = btn.dataset.currentStatus;
        const confirmHtml = confirmTextMap[action];
        const dtRow = dt_user.row(btn.closest('tr'));

        if (!confirmHtml) { return; }

        Swal.fire({
            title: 'Aktion bestätigen',
            html: confirmHtml,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Ja, bestätigen',
            cancelButtonText: 'Abbrechen',
            customClass: {
                confirmButton: 'btn btn-primary me-3',
                cancelButton: 'btn btn-label-secondary'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (!result.isConfirmed) { return; }

            if (action === 'delete') {
                fetch('/User/DeleteUser', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: 'id=' + encodeURIComponent(userId)
                }).then(function (res) {
                    if (res.ok) {
                        dtRow.remove().draw(false);
                        Swal.fire({
                            title: 'Erledigt!',
                            text: 'Benutzer wurde gelöscht.',
                            icon: 'success',
                            customClass: { confirmButton: 'btn btn-primary' },
                            buttonsStyling: false
                        });
                    } else if (res.status === 400) {
                        Swal.fire({ title: 'Nicht erlaubt', text: 'Dieser Benutzer kann nicht gelöscht werden.', icon: 'warning', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
                    } else {
                        Swal.fire({ title: 'Fehler', text: 'Benutzer konnte nicht gelöscht werden.', icon: 'error', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
                    }
                });
                return;
            }

            fetch('/User/UpdateUserStatus', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'id=' + encodeURIComponent(userId) + '&status=' + encodeURIComponent(currentStatus)
            }).then(function (res) {
                if (res.ok) {
                    res.json().then(function (body) {
                        const rowData = dtRow.data();
                        rowData.status = body.status;
                        dtRow.data(rowData).draw(false);
                    });
                    Swal.fire({
                        title: 'Erledigt!',
                        text: 'Benutzerstatus wurde aktualisiert.',
                        icon: 'success',
                        customClass: { confirmButton: 'btn btn-primary' },
                        buttonsStyling: false
                    });
                } else if (res.status === 400) {
                    Swal.fire({ title: 'Nicht erlaubt', text: 'Sie können Ihren eigenen Status nicht ändern.', icon: 'warning', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
                } else {
                    Swal.fire({ title: 'Fehler', text: 'Status konnte nicht aktualisiert werden.', icon: 'error', customClass: { confirmButton: 'btn btn-primary' }, buttonsStyling: false });
                }
            });
        });
    });

    // Layout tweaks (same as template)
    setTimeout(() => {
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
        ].forEach(({ selector, classToRemove, classToAdd }) => {
            document.querySelectorAll(selector).forEach(el => {
                if (classToRemove) { classToRemove.split(' ').forEach(c => el.classList.remove(c)); }
                if (classToAdd) { classToAdd.split(' ').forEach(c => el.classList.add(c)); }
            });
        });
    }, 100);

});
