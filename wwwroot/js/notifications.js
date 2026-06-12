/**
 * Global notification listener
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
        showToast(data.senderName, data.preview, data.requestNumber);
        if (document.hidden) {
            showBrowserNotification(data.senderName, data.preview, data.requestNumber);
        }
    });

    connection.start()
        .then(function () { return connection.invoke('JoinNotifications'); })
        .catch(function (e) { console.error('Notification hub error:', e); });

    // ── Bootstrap toast ───────────────────────────────────────────────────────

    function showToast(senderName, preview, requestNumber) {
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
                '<small class="text-body-secondary">just now</small>' +
                '<button type="button" class="btn-close ms-2" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>' +
            '<div class="toast-body">' +
                '<p class="mb-2" style="overflow:hidden;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;">' + esc(preview) + '</p>' +
                '<a href="/Request/View?number=' + encodeURIComponent(requestNumber) + '" class="btn btn-sm btn-primary">View request</a>' +
            '</div>';

        container.appendChild(el);
        var toast = new bootstrap.Toast(el, { delay: 8000 });
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

    function esc(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }
})();
