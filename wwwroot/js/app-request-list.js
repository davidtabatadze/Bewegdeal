/**
 * Requests List — Bewegdeal
 * v1.0.3
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
    const dt_table = document.querySelector('.datatables-requests');

    // Status → icon HTML + label
    const sl = window.requestStatusLabels || {};
    const statusMap = {
        pending: { icon: 'ri-timer-flash-line', color: 'warning', label: sl.pending },
        cancelled: { icon: 'ri-hand', color: 'danger', label: sl.cancelled },
        negotiation: { icon: 'ri-wechat-line', color: 'info', label: sl.negotiation },
        agreed: { icon: 'ri-shake-hands-line', color: 'success', label: sl.agreed },
        resolved: { icon: 'ri-check-double-line', color: 'success', label: sl.resolved },
        declined: { icon: 'ri-rest-time-line', color: 'dark', label: sl.declined },
    };

    // Service → icon HTML + label + text color
    const sv = window.requestServiceLabels || {};
    const serviceMap2 = {
        moving: { icon: 'ri-truck-line', color: 'bg-label-success', title: sv.moving },
        removal: { icon: 'ri-recycle-line', color: 'bg-label-danger', title: sv.removal },
        pickup: { icon: 'ri-shopping-bag-4-line', color: 'bg-label-warning', title: sv.pickup },
        transport: { icon: 'ri-car-line', color: 'bg-label-info', title: sv.transport }
    };
    const serviceMap = {
        moving: { icon: '<i class="icon-base ri ri-truck-line        icon-22px text-success"></i>', label: sv.moving, color: 'text-success' },
        removal: { icon: '<i class="icon-base ri ri-recycle-line      icon-22px text-danger"></i>', label: sv.removal, color: 'text-danger' },
        pickup: { icon: '<i class="icon-base ri ri-shopping-bag-4-line      icon-22px text-warning"></i>', label: sv.pickup, color: 'text-warning' },
        transport: { icon: '<i class="icon-base ri ri-car-line          icon-22px text-info"></i>', label: sv.transport, color: 'text-info' }
    };

    // Column index → sort field sent to the server (only sortable columns listed)
    const columnToField = {
        1: 'status',
        2: 'service',
        3: 'createDate'
    };

    if (!dt_table) { return; }

    // ── State persistence ────────────────────────────────────────────────────────
    const STATE_KEY = 'requestListState';
    const RETURN_KEY = 'requestListReturn';

    const isReturn = !!sessionStorage.getItem(RETURN_KEY);
    sessionStorage.removeItem(RETURN_KEY);

    let savedState = null;
    if (isReturn) {
        try { savedState = JSON.parse(sessionStorage.getItem(STATE_KEY)); } catch (e) { }
    } else {
        sessionStorage.removeItem(STATE_KEY);
    }

    // Restore filter inputs before the first DataTable draw
    if (savedState) {
        document.getElementById('requestsSearch').value = savedState.search || '';
        $('#filterStatus').selectpicker('val', savedState.status || '');
        $('#filterService').selectpicker('val', savedState.service || '');
        const filterRequest = document.getElementById('filterRequest');
        if (filterRequest) { $('#filterRequest').selectpicker('val', savedState.viewerFocus || ''); }
    }
    // ─────────────────────────────────────────────────────────────────────────────

    const dt = new DataTable(dt_table, {
        serverSide: true,
        scrollX: true,
        displayStart: savedState ? (savedState.start || 0) : 0,
        ajax: {
            url: '/Request/LoadRequests',
            data: function (d) {
                const order = d.order && d.order[0];
                d.sortField = columnToField[order ? order.column : 3] || 'createDate';
                d.sortDirection = order ? order.dir : 'desc';

                delete d.order;
                delete d.columns;
                delete d.search;

                d.search = document.getElementById('requestsSearch').value;
                d.status = document.getElementById('filterStatus').value;
                d.service = document.getElementById('filterService').value;
                const filterRequest = document.getElementById('filterRequest');
                d.viewerFocus = filterRequest ? filterRequest.value : '';

                // Persist state on every draw
                sessionStorage.setItem(STATE_KEY, JSON.stringify({
                    search: d.search,
                    status: d.status,
                    service: d.service,
                    viewerFocus: d.viewerFocus,
                    start: d.start
                }));

                return d;
            }
        },
        columns: [
            // { data: 'id' },   // 0 — number / view button
            { data: 'status' },   // 1 — status badge
            { data: 'title' },   // 2 — request cell (image + title + service)
            { data: 'service' },   // 3 — service icon
            { data: 'createDate', width: '120px' },   // 4 — date
            { data: 'cost' },   // 5 — details
        ],
        columnDefs: [
            // {
            //     // Number — view button
            //     targets: 0,
            //     width: '60px',
            //     render: function (data, type, full) {
            //         return (
            //             '<button type="button" class="btn btn-text-secondary view-request-btn"' +
            //             ' data-number="' + full['number'] + '">' +
            //             '<span class="icon-base ri ri-search-eye-line icon-16px me-1_5"></span>' +
            //             '#' + full['id'] +
            //             '</button>'
            //         );
            //     }
            // },
            {
                // Status icon with tooltip
                targets: 1,
                width: '60px',
                render: function (data, type, full) {
                    const map = statusMap[full['status']];
                    return (
                        '<ul class="list-unstyled m-0 avatar-group d-flex align-items-center">' +
                        '<li class="avatar avatar-m" data-bs-toggle="tooltip" data-bs-placement="top" title="' + map.label + '">' +
                        '<div class="avatar-initial rounded-circle bg-label-' + map.color + '">' +
                        '<i class="icon-base ri ' + map.icon + ' icon-m"></i>' +
                        '</div>' +
                        '</li>' +
                        '</ul>'
                    );
                }
            },
            {
                // Request cell — thumbnail + title + service label
                targets: 0,
                orderable: false,
                createdCell: function (td, cellData, rowData) {
                    td.style.minWidth = '300px';
                    td.style.cursor = 'pointer';
                    td.dataset.number = rowData['number'];
                },
                render: function (data, type, full) {
                    const title = full['title'];
                    const service = full['service'];
                    const imageUrl = full['imageUrl'];
                    const serviceObj = serviceMap[service] || { label: service };

                    const img = imageUrl
                        ? '<img src="' + imageUrl + '" class="rounded pull-up view-request-btn" style="width:64px;height:64px;object-fit:cover;flex-shrink:0;cursor:pointer;" data-number="' + full['number'] + '">'
                        : '<div class="rounded bg-label-secondary d-flex align-items-center justify-content-center" style="width:40px;height:40px;flex-shrink:0;"><i class="icon-base ri ri-image-line"></i></div>';

                    return (
                        '<div class="d-flex align-items-center gap-3">' +
                        img +
                        '<div class="d-flex flex-column">' +
                        '<span class="view-request-btn text-heading fw-medium text-strong" style="cursor:pointer;" data-number="' + full['number'] + '">' + title + '</span>' +
                        '<small class="text-muted mt-2">#' + full['id'] + '</small>' +
                        '</div>' +
                        '</div>'
                    );
                }
            },
            {
                // Service icon with tooltip
                targets: 2,
                width: '60px',
                render: function (data, type, full) {
                    const map = serviceMap2[full['service']];
                    return (
                        '<ul class="list-unstyled m-0 avatar-group d-flex align-items-center">' +
                        '<li class="avatar avatar-m" data-bs-toggle="tooltip" data-bs-placement="top" title="' + map.title + '">' +
                        '<div class="avatar-initial rounded-circle ' + map.color + '">' +
                        '<i class="icon-base ri ' + map.icon + ' icon-m"></i>' +
                        '</div>' +
                        '</li>' +
                        '</ul>'
                    );
                }
            },
            {
                // Create date
                targets: 3,
                width: '120px',
                createdCell: function (td) {
                    td.style.minWidth = '135px';
                },
                render: function (data, type, full) {
                    return full['createDate'];
                }
            },
            {
                // Details — cost + timing
                targets: 4,
                orderable: false,
                createdCell: function (td) {
                    td.style.minWidth = '200px';
                },
                render: function (data, type, full) {
                    var asap = full['asap'];
                    var cost = full['cost'];
                    var date = full['date'];
                    var time = full['time'];
                    const avatar = full['requester'] || {};

                    const proposal = full['proposal'] || {};
                    const proposalColor = proposal.status == 'pending' ? 'warning' :
                        proposal.status == 'accepted' ? 'success' : '';
                    const proposalIcon = !proposal.status ? '' :
                        '<i class="icon-base ri ri-shake-hands-line icon-18px text-' + proposalColor + ' me-1"></i>'
                    if (proposal.status) {
                        asap = false;
                        cost = proposal.cost;
                        date = proposal.date;
                        time = proposal.time;
                    }

                    let timing;
                    if (asap) {
                        timing = 'So schnell wie möglich';
                    } else {
                        timing = date || '';
                        if (time) { timing += ' - ' + time; }
                    }

                    const avatarInner = avatar.url
                        ? '<img src="' + avatar.url + '"class="rounded-circle" style="width:100%;height:100%;object-fit:cover;" />'
                        : '<span class="avatar-initial rounded-circle bg-label-primary">' + (avatar.initials || '') + '</span>';

                    return (
                        '<div class="d-flex justify-content-start align-items-center user-name">' +
                        '<div class="avatar-wrapper">' +
                        '<div class="avatar avatar-m me-2" data-bs-toggle="tooltip" data-bs-placement="top" title="' + avatar.name + '">' + avatarInner + '</div>' +
                        '</div>' +
                        '<div class="d-flex flex-column">' +
                        '<span class="fw-medium">' + proposalIcon + '€' + parseFloat(cost).toFixed(2) + '</span>' +
                        '<small class="text-muted">' + timing + '</small>' +
                        '</div>' +
                        '</div>'
                    );
                }
            }
        ],
        pageLength: 10,
        order: [[3, 'desc']],
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
            // german
            info: 'Zeige _START_ bis _END_ von _TOTAL_ Einträgen',
            infoEmpty: 'Keine Einträge vorhanden',
            infoFiltered: '(gefiltert von _MAX_ Einträgen)',
            zeroRecords: 'Keine passenden Einträge gefunden',
            emptyTable: 'Keine Daten vorhanden',
            loadingRecords: 'Wird geladen...',
            processing: 'Bitte warten...',
            // german
            paginate: {
                next: '<i class="icon-base ri ri-arrow-right-s-line scaleX-n1-rtl icon-22px"></i>',
                previous: '<i class="icon-base ri ri-arrow-left-s-line  scaleX-n1-rtl icon-22px"></i>',
                first: '<i class="icon-base ri ri-skip-back-mini-line    scaleX-n1-rtl icon-22px"></i>',
                last: '<i class="icon-base ri ri-skip-forward-mini-line scaleX-n1-rtl icon-22px"></i>'
            }
        },
        responsive: false
    });

    // Loading indicator — also covers the initial load
    Block.pulse('.card-datatable');

    dt.on('preXhr.dt', function () {
        Block.pulse('.card-datatable');
    });

    dt.on('xhr.dt', function () {
        Block.remove('.card-datatable');
    });

    // Filters
    let searchTimeout;
    document.getElementById('requestsSearch').addEventListener('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () { dt.ajax.reload(null, true); }, 500);
    });

    document.getElementById('filterStatus').addEventListener('change', function () {
        dt.ajax.reload(null, true);
    });

    document.getElementById('filterService').addEventListener('change', function () {
        dt.ajax.reload(null, true);
    });

    const filterRequestEl = document.getElementById('filterRequest');
    if (filterRequestEl) {
        filterRequestEl.addEventListener('change', function () {
            dt.ajax.reload(null, true);
        });
    }

    // View request — delegated click on the table wrapper
    dt_table.addEventListener('click', function (e) {
        const btn = e.target.closest('.view-request-btn') || e.target.closest('td[data-number]');
        if (!btn) { return; }
        sessionStorage.setItem(RETURN_KEY, '1');
        window.location.href = '/Request/View?number=' + btn.dataset.number;
    });

    // Middle-click — open in new tab
    dt_table.addEventListener('auxclick', function (e) {
        if (e.button !== 1) { return; }
        const btn = e.target.closest('.view-request-btn') || e.target.closest('td[data-number]');
        if (!btn) { return; }
        e.preventDefault();
        window.open('/Request/View?number=' + btn.dataset.number, '_blank');
    });

    // Layout tweaks
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

});
