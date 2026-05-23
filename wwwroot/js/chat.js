/**
 * Request Chat
 * Phase 1 (page load)   — GET /Chat/Visibility  → show/hide the button
 * Phase 2 (canvas open) — GET /Chat/Context     → load party info + messages, render UI
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

    // ── Phase 2: full context (on offcanvas open) ─────────────────────────────

    offcanvas.addEventListener('show.bs.offcanvas', function () {
        if (contextLoaded) { return; }
        showSpinner();
    });

    offcanvas.addEventListener('shown.bs.offcanvas', function () {
        if (contextLoaded) {
            if (mode === ChatMode.Active && chatKey) { connectSignalR(chatKey); }
            return;
        }

        fetch('/Chat/Context?requestNumber=' + encodeURIComponent(requestNumber))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                contextLoaded = true;
                cfg.viewerId          = data.viewerId;
                cfg.viewerInitials    = data.viewerInitials    || '?';
                cfg.viewerPictureUrl  = data.viewerPictureUrl  || '';
                cfg.otherPartyName    = data.otherPartyName    || '';
                cfg.otherPartyInitials   = data.otherPartyInitials   || '?';
                cfg.otherPartyPictureUrl = data.otherPartyPictureUrl || '';
                viewerId = data.viewerId || 0;

                if (mode === ChatMode.Active) {
                    chatKey = data.chatKey || '';
                    renderActiveChatUI(chatKey, data.otherPartyName,
                        data.otherPartyInitials, data.otherPartyPictureUrl,
                        data.messages || []);
                    connectSignalR(chatKey);
                } else if (mode === ChatMode.Initiate) {
                    renderInitiateUI(data.otherPartyName,
                        data.otherPartyInitials, data.otherPartyPictureUrl);
                }
            })
            .catch(function (e) {
                console.error('Chat context failed:', e);
                showError();
            });
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
                chatKey = data.chatKey;
                mode    = ChatMode.Active;
                cfg.otherPartyInitials   = data.otherPartyInitials   || cfg.otherPartyInitials;
                cfg.otherPartyPictureUrl = data.otherPartyPictureUrl || cfg.otherPartyPictureUrl;

                renderActiveChatUI(chatKey, data.otherPartyName,
                    data.otherPartyInitials, data.otherPartyPictureUrl, []);
                connectSignalR(chatKey);
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

        connection.invoke('SendMessage', chatKey, content)
            .then(function () { if (input) { input.value = ''; } })
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
        });

        connection.start()
            .then(function () { return connection.invoke('JoinChat', key); })
            .catch(function (e) { console.error('SignalR error:', e); });

        scrollToBottom();
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    function showSpinner() {
        body.innerHTML =
            '<div class="d-flex align-items-center justify-content-center flex-grow-1">' +
              '<div class="spinner-border text-primary" role="status">' +
                '<span class="visually-hidden">Loading…</span>' +
              '</div>' +
            '</div>';
    }

    function showError() {
        body.innerHTML =
            '<div class="d-flex align-items-center justify-content-center flex-grow-1 text-muted">' +
              '<span>Failed to load chat. Please try again.</span>' +
            '</div>';
    }

    function renderInitiateUI(name, initials, pictureUrl) {
        body.innerHTML =
            '<div class="d-flex flex-column align-items-center justify-content-center text-center flex-grow-1 p-6">' +
              '<div class="avatar avatar-lg mb-4">' + buildAvatarHtml(pictureUrl, initials, name) + '</div>' +
              '<h6 class="mb-1">' + esc(name) + '</h6>' +
              '<p class="text-muted small mb-5">Start a conversation about this request</p>' +
              '<button id="chatInitiateBtn" class="btn btn-primary">' +
                '<i class="icon-base ri ri-wechat-line me-1"></i>Start Conversation' +
              '</button>' +
            '</div>';
    }

    function renderActiveChatUI(key, name, initials, pictureUrl, messages) {
        body.innerHTML =
            '<div class="chat-history-wrapper d-flex flex-column h-100" data-chat-key="' + esc(key) + '">' +
              '<div class="chat-history-header border-bottom px-4 py-3 flex-shrink-0">' +
                '<div class="d-flex align-items-center gap-3">' +
                  '<div class="avatar avatar-sm flex-shrink-0">' + buildAvatarHtml(pictureUrl, initials, name) + '</div>' +
                  '<h6 class="mb-0">' + esc(name) + '</h6>' +
                '</div>' +
              '</div>' +
              '<div class="chat-history-body flex-grow-1 overflow-auto p-4">' +
                '<ul class="list-unstyled chat-history mb-0" id="chatMessageList"></ul>' +
              '</div>' +
              '<div class="chat-history-footer border-top px-4 py-3 flex-shrink-0">' +
                '<form class="form-send-message d-flex gap-2 align-items-center">' +
                  '<input type="text" class="form-control message-input border-0 shadow-none"' +
                         ' placeholder="Type your message…" maxlength="2048" autocomplete="off" />' +
                  '<button type="submit" class="btn btn-primary btn-icon flex-shrink-0">' +
                    '<i class="icon-base ri ri-send-plane-line"></i>' +
                  '</button>' +
                '</form>' +
              '</div>' +
            '</div>';

        messages.forEach(function (msg) { appendMessage(msg.senderId, msg.content, msg.sentDate); });
    }

    function appendMessage(senderId, content, time) {
        var list = document.getElementById('chatMessageList');
        if (!list) { return; }

        var isMine     = senderId === viewerId;
        var initials   = isMine ? (cfg.viewerInitials    || '?') : (cfg.otherPartyInitials    || '?');
        var pictureUrl = isMine ? (cfg.viewerPictureUrl  || '')  : (cfg.otherPartyPictureUrl  || '');
        var avatarHtml = buildAvatarHtml(pictureUrl, initials);

        var li = document.createElement('li');
        li.className = 'chat-message' + (isMine ? ' chat-message-right' : '') + ' mb-4';

        if (isMine) {
            li.innerHTML =
                '<div class="d-flex overflow-hidden">' +
                  '<div class="chat-message-wrapper flex-grow-1">' +
                    '<div class="chat-message-text"><p class="mb-0" style="white-space:pre-wrap;">' + esc(content) + '</p></div>' +
                    '<div class="text-end text-body-secondary mt-1"><small>' + esc(time) + '</small></div>' +
                  '</div>' +
                  '<div class="user-avatar flex-shrink-0 ms-3"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>' +
                '</div>';
        } else {
            li.innerHTML =
                '<div class="d-flex overflow-hidden">' +
                  '<div class="user-avatar flex-shrink-0 me-3"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>' +
                  '<div class="chat-message-wrapper flex-grow-1">' +
                    '<div class="chat-message-text"><p class="mb-0" style="white-space:pre-wrap;">' + esc(content) + '</p></div>' +
                    '<div class="text-body-secondary mt-1"><small>' + esc(time) + '</small></div>' +
                  '</div>' +
                '</div>';
        }

        list.appendChild(li);
    }

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
