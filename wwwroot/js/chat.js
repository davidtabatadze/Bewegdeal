/**
 * Request Chat
 * Phase 1 (page load)   — GET /Chat/Visibility    → show/hide the button
 * Phase 2 (canvas open) — GET /Chat/Conversation  → server-rendered HTML, connect SignalR
 */
'use strict';

(function () {
    var cfg      = window.chatConfig || {};
    var ChatMode = window.ChatMode   || { None: 'none', Initiate: 'initiate', Active: 'active' };
    var requestNumber = cfg.requestNumber || '';
    if (!requestNumber) { return; }

    var floatingBtn = document.getElementById('chatFloatingBtn');
    var offcanvas   = document.getElementById('requestChatOffcanvas');
    var body        = document.getElementById('chatOffcanvasBody');
    if (!floatingBtn || !offcanvas || !body) { return; }

    var mode       = ChatMode.None;
    var chatKey    = '';
    var viewerId   = 0;
    var connection = null;
    var contextLoaded = false;

    // ── Phase 1: visibility check (fast) ─────────────────────────────────────

    fetch('/Chat/Visibility?requestNumber=' + encodeURIComponent(requestNumber))
        .then(function (r) { return r.json(); })
        .then(function (data) {
            mode = data.mode || ChatMode.None;
            if (mode === ChatMode.None) { return; }
            floatingBtn.style.display = '';
        })
        .catch(function (e) { console.error('Chat visibility failed:', e); });

    // ── Phase 2: load conversation (on offcanvas open) ────────────────────────

    offcanvas.addEventListener('shown.bs.offcanvas', function () {
        if (contextLoaded) {
            if (mode === ChatMode.Active && chatKey) { connectSignalR(chatKey); }
            return;
        }
        loadConversation();
    });

    offcanvas.addEventListener('hidden.bs.offcanvas', function () {
        if (connection) {
            connection.stop();
            connection = null;
        }
    });

    // ── Initiate flow ─────────────────────────────────────────────────────────

    body.addEventListener('click', function (e) {
        var btn = e.target.closest('#chatInitiateBtn');
        if (!btn) { return; }

        btn.disabled = true;
        btn.innerHTML =
            '<span class="spinner-border spinner-border-sm me-1" role="status"></span>Starting…';

        fetch('/Chat/Initiate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'requestNumber=' + encodeURIComponent(requestNumber)
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.success) {
                    btn.disabled = false;
                    btn.innerHTML =
                        '<i class="icon-base ri ri-wechat-line me-1"></i>Start Conversation';
                    return;
                }
                contextLoaded = false;
                loadConversation();
            })
            .catch(function () {
                btn.disabled = false;
                btn.innerHTML =
                    '<i class="icon-base ri ri-wechat-line me-1"></i>Start Conversation';
            });
    });

    // ── Send form ─────────────────────────────────────────────────────────────

    body.addEventListener('submit', function (e) {
        if (!e.target.classList.contains('form-send-message')) { return; }
        e.preventDefault();

        var input   = e.target.querySelector('.message-input');
        var content = (input ? input.value : '').trim();
        if (!content || !connection) { return; }

        input.value = '';

        connection.invoke('SendMessage', chatKey, content)
            .catch(function (err) { console.error('Send error:', err); });
    });

    // ── SignalR ───────────────────────────────────────────────────────────────

    function connectSignalR(key) {
        if (connection) { return; }

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveMessage', function (msg) {
            appendMessage(msg.senderId, msg.content, msg.sentDate);
            scrollToBottom();
            if (msg.senderId !== viewerId) {
                connection.invoke('MarkRead', key).catch(function (err) { console.error('MarkRead error:', err); });
            }
        });

        connection.on('MessagesRead', function () {
            var icons = document.querySelectorAll('.msg-read-receipt');
            for (var i = 0; i < icons.length; i++) {
                icons[i].classList.add('text-success');
            }
        });

        connection.start()
            .then(function () { return connection.invoke('JoinChat', key); })
            .catch(function (e) { console.error('SignalR error:', e); });

        scrollToBottom();
    }

    // ── Core ─────────────────────────────────────────────────────────────────

    function loadConversation() {
        Block.pulse('#chatCard');
        fetch('/Chat/Conversation?requestNumber=' + encodeURIComponent(requestNumber))
            .then(function (r) { return r.text(); })
            .then(function (html) {
                Block.remove('#chatCard');
                body.innerHTML = html;
                contextLoaded  = true;

                var conv = document.getElementById('chatConversation');
                if (!conv) { return; }

                mode     = conv.dataset.mode    || ChatMode.None;
                chatKey  = conv.dataset.chatKey || '';
                viewerId = parseInt(conv.dataset.viewerId || '0', 10);

                if (mode === ChatMode.Active) {
                    var historyBody = body.querySelector('.chat-history-body');
                    if (historyBody) { new PerfectScrollbar(historyBody); }
                    connectSignalR(chatKey);
                    scrollToBottom();
                }
            })
            .catch(function (e) {
                console.error('Chat conversation failed:', e);
                Block.remove('#chatCard');
                showError();
            });
    }

    // ── Real-time message append ──────────────────────────────────────────────

    function appendMessage(senderId, content, time) {
        var list = document.getElementById('chatMessageList');
        if (!list) { return; }

        var conv       = document.getElementById('chatConversation');
        var isMine     = senderId === viewerId;
        var initials   = isMine ? (conv && conv.dataset.viewerInitials || '?') : (conv && conv.dataset.otherInitials || '?');
        var pictureUrl = isMine ? (conv && conv.dataset.viewerPicture  || '')  : (conv && conv.dataset.otherPicture  || '');
        var avatarHtml = buildAvatarHtml(pictureUrl, initials);

        var li = document.createElement('li');
        li.className = 'chat-message' + (isMine ? ' chat-message-right' : '') + ' mb-2';

        if (isMine) {
            li.innerHTML =
                '<div class="d-flex overflow-hidden">' +
                  '<div class="chat-message-wrapper flex-grow-1">' +
                    '<div class="chat-message-text"><p class="mb-0">' + esc(content) + '</p></div>' +
                    '<div class="text-end text-body-secondary mt-1">' +
                      '<i class="msg-read-receipt icon-base ri ri-check-double-line icon-16px me-1"></i>' +
                      '<small>' + esc(time) + '</small>' +
                    '</div>' +
                  '</div>' +
                  '<div class="user-avatar flex-shrink-0 ms-4"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>' +
                '</div>';
        } else {
            li.innerHTML =
                '<div class="d-flex overflow-hidden">' +
                  '<div class="user-avatar flex-shrink-0 me-4"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>' +
                  '<div class="chat-message-wrapper flex-grow-1">' +
                    '<div class="chat-message-text"><p class="mb-0">' + esc(content) + '</p></div>' +
                    '<div class="text-body-secondary mt-1"><small>' + esc(time) + '</small></div>' +
                  '</div>' +
                '</div>';
        }

        list.appendChild(li);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    function buildAvatarHtml(pictureUrl, initials, altText) {
        if (pictureUrl) {
            return '<img src="' + esc(pictureUrl) + '" class="rounded-circle"' +
                   ' style="object-fit:cover;width:100%;height:100%;"' +
                   (altText ? ' alt="' + esc(altText) + '"' : '') + ' />';
        }
        return '<span class="avatar-initial rounded-circle bg-label-primary w-100 h-100"' +
               ' style="display:flex;align-items:center;justify-content:center;font-size:0.75rem;">' +
               esc(initials || '?') + '</span>';
    }

    function showError() {
        body.innerHTML =
            '<div class="col d-flex align-items-center justify-content-center text-muted">' +
              '<span>Failed to load chat. Please try again.</span>' +
            '</div>';
    }

    function scrollToBottom() {
        var chatBody = document.querySelector('.chat-history-body');
        if (chatBody) { chatBody.scrollTop = chatBody.scrollHeight; }
    }

    function esc(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }
})();
