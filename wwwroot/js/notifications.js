/**
 * Global notification listener
 * v1.0.1
 * Connects to /hubs/chat, joins the user's personal group, and handles incoming
 * NewMessageNotification events with a Bootstrap toast + Browser Notification.
 */
'use strict';

(function () {
    var userId = (window.notificationConfig && window.notificationConfig.userId) || 0;
    if (!userId) { return; }

    // ── Browser notification permission (ask once) ────────────────────────────

    if ('Notification' in window && Notification.permission === 'default') {
        Notification.requestPermission();
    }

    // ── SignalR ───────────────────────────────────────────────────────────────

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/chat')
        .withAutomaticReconnect()
        .build();

    connection.on('NewMessageNotification', function (data) {
        if (window.chatOpen) { return; }
        showToast(data.senderName, data.preview, data.requestNumber, data.date);
        if (document.hidden) {
            showBrowserNotification(data.senderName, data.preview, data.requestNumber);
        }
    });

    connection.start()
        .then(function () { return connection.invoke('Notify'); })
        .catch(function (e) { });

    // ── Bootstrap toast ───────────────────────────────────────────────────────

    function showToast(senderName, preview, requestNumber, date) {
        var container = document.getElementById('notifToastContainer');
        if (!container) { return; }

        var el = document.createElement('div');
        el.className = 'bs-toast toast animate__animated animate__bounceInUp';
        el.setAttribute('role', 'alert');
        el.setAttribute('aria-live', 'assertive');
        el.setAttribute('aria-atomic', 'true');
        el.innerHTML =
            '<div class="toast-header">' +
            '<i class="icon-base ri ri-wechat-line icon-sm text-primary me-2"></i>' +
            '<div class="me-auto fw-medium">' + esc(senderName) + '</div>' +
            '<small class="text-body-secondary">' + esc(getDateLabel(date)) + '</small>' +
            '<button type="button" class="btn-close ms-2" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>' +
            '<div class="toast-body d-flex align-items-center gap-2">' +
            '<a href="/Request/View?number=' + encodeURIComponent(requestNumber) + '&chat=open" class="btn btn-text-primary btn-icon btn-sm rounded-pill flex-shrink-0"><i class="ri ri-search-eye-line icon-md"></i></a>' +
            '<a href="/Request/View?number=' + encodeURIComponent(requestNumber) + '&chat=open" class="text-body text-decoration-none mb-0 flex-grow-1" style="overflow:hidden;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;">' + esc(preview) + '</a>' +
            '</div>';

        container.appendChild(el);
        var toast = new bootstrap.Toast(el, { delay: 5000 });
        toast.show();
        el.addEventListener('hidden.bs.toast', function () { el.remove(); });
    }

    // ── Browser notification ──────────────────────────────────────────────────

    function showBrowserNotification(senderName, preview, requestNumber) {
        if (!('Notification' in window) || Notification.permission !== 'granted') { return; }
        var n = new Notification('New message from ' + senderName, {
            body: preview,
            icon: '/img/favicon/favicon.ico'
        });
        n.onclick = function () {
            window.focus();
            window.location.href = '/Request/View?number=' + encodeURIComponent(requestNumber);
            n.close();
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    function getDateLabel(dateStr) {
        if (!dateStr) { return ''; }
        var d = new Date(dateStr);
        var now = new Date();
        if (now - d < 2 * 60 * 1000) { return 'Just now'; }
        var toStr = function (dt) { return dt.toISOString().slice(0, 10); };
        var todayStr = toStr(now);
        var yesterday = new Date(now);
        yesterday.setDate(now.getDate() - 1);
        var dStr = toStr(d);
        if (dStr === todayStr) { return 'Today'; }
        if (dStr === toStr(yesterday)) { return 'Yesterday'; }
        return d.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
    }

    function esc(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }
})();
