/**
 * Request Chat
 * Phase 1 (page load)   — GET /RequestChat/Visibility    → show/hide the button
 * Phase 2 (canvas open) — GET /RequestChat/Conversation  → server-rendered HTML, connect SignalR
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

    var mode            = ChatMode.None;
    var chatKey         = '';
    var viewerId        = 0;
    var connection      = null;
    var contextLoaded   = false;
    var lastMessageDate = '';
    var waitingForEcho  = false;
    var echoTimer       = null;

    // ── Phase 1: visibility check (fast) ─────────────────────────────────────

    fetch('/RequestChat/Visibility?requestNumber=' + encodeURIComponent(requestNumber))
        .then(function (r) { return r.json(); })
        .then(function (data) {
            mode = data.mode || ChatMode.None;
            if (mode === ChatMode.None) { return; }
            floatingBtn.style.display = '';
        })
        .catch(function (e) { console.error('Chat visibility failed:', e); });

    // ── Phase 2: load conversation (on offcanvas open) ────────────────────────

    offcanvas.addEventListener('shown.bs.offcanvas', function () {
        window.chatOpen = true;
        if (contextLoaded) {
            if (mode === ChatMode.Active && chatKey) { connectSignalR(chatKey); }
            return;
        }
        loadConversation();
    });

    offcanvas.addEventListener('hidden.bs.offcanvas', function () {
        window.chatOpen = false;
        if (connection) {
            connection.stop();
            connection = null;
        }
    });

    // ── Cancel flow ──────────────────────────────────────────────────────────

    body.addEventListener('click', function (e) {
        if (!e.target.closest('#chatCancelBtn')) { return; }

        Swal.fire({
            title: 'End negotiation?',
            text: 'Sure you want to cancel the negotiation?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, end it',
            cancelButtonText: 'No',
            customClass: {
                confirmButton: 'btn btn-danger me-3',
                cancelButton: 'btn btn-label-secondary'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (!result.isConfirmed) { return; }

            Block.pulse('#chatCard');
            fetch('/RequestChat/Cancel', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'requestNumber=' + encodeURIComponent(requestNumber)
            })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (data.success) { window.location.reload(); }
                    else { Block.remove('#chatCard'); }
                })
                .catch(function () { Block.remove('#chatCard'); });
        });
    });

    // ── Initiate flow ─────────────────────────────────────────────────────────

    body.addEventListener('click', function (e) {
        var btn = e.target.closest('#chatInitiateBtn');
        if (!btn) { return; }

        btn.disabled = true;
        btn.innerHTML =
            '<span class="spinner-border spinner-border-sm me-1" role="status"></span>Starting…';

        fetch('/RequestChat/Initiate', {
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
        sendMessage(e.target);
    });

    body.addEventListener('keydown', function (e) {
        var input = e.target;
        if (!input.classList.contains('message-input')) { return; }
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            var form = input.closest('.form-send-message');
            if (form) { sendMessage(form); }
        }
    });

    body.addEventListener('paste', function (e) {
        if (!e.target.classList.contains('message-input')) { return; }
        e.preventDefault();
    });

    body.addEventListener('input', function (e) {
        var input = e.target;
        if (!input.classList.contains('message-input')) { return; }
        input.style.height = 'auto';
        input.style.height = input.scrollHeight + 'px';
        if (input.value.length > 1024) {
            input.style.setProperty('border', '1px solid var(--bs-danger)', 'important');
        } else {
            input.style.removeProperty('border');
        }
    });

    function sendMessage(form) {
        var input   = form.querySelector('.message-input');
        var content = (input ? input.value : '').trim();
        if (!content || content.length > 1024 || !connection) { return; }

        input.value = '';
        input.style.height = 'auto';

        waitingForEcho = true;
        echoTimer = setTimeout(function () {
            if (waitingForEcho) { Block.pulse('.chat-history-footer'); }
        }, 1000);

        connection.invoke('Send', chatKey, content)
            .catch(function (err) {
                clearTimeout(echoTimer);
                Block.remove('.chat-history-footer');
                waitingForEcho = false;
                console.error('Send error:', err);
            });
    }

    // ── SignalR ───────────────────────────────────────────────────────────────

    function connectSignalR(key) {
        if (connection) { return; }

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveMessage', function (msg) {
            if (msg.senderId === viewerId && waitingForEcho) {
                clearTimeout(echoTimer);
                Block.remove('.chat-history-footer');
                waitingForEcho = false;
            }
            appendMessage(msg.senderId, msg.content, msg.sentDate, msg.sentDay);
            scrollToBottom();
            if (msg.senderId !== viewerId) {
                connection.invoke('MarkRead', key).catch(function (err) { console.error('MarkRead error:', err); });
            }
        });

        connection.on('ChatCancelled', function () {
            window.location.reload();
        });

        connection.on('MessagesRead', function () {
            var icons = document.querySelectorAll('.msg-read-receipt');
            for (var i = 0; i < icons.length; i++) {
                icons[i].classList.add('text-success');
            }
        });

        connection.start()
            .then(function () { return connection.invoke('Join', key); })
            .catch(function (e) { console.error('SignalR error:', e); });

        scrollToBottom();
    }

    // ── Core ─────────────────────────────────────────────────────────────────

    function loadConversation() {
        Block.pulse('#chatCard');
        fetch('/RequestChat/Conversation?requestNumber=' + encodeURIComponent(requestNumber))
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
                    var separators = body.querySelectorAll('.chat-date-separator[data-date]');
                    lastMessageDate = separators.length ? separators[separators.length - 1].getAttribute('data-date') : '';
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

    function appendMessage(senderId, content, time, sentDay) {
        var list = document.getElementById('chatMessageList');
        if (!list) { return; }

        if (sentDay && sentDay !== lastMessageDate) {
            lastMessageDate = sentDay;
            var sep = document.createElement('li');
            sep.className = 'chat-date-separator text-center my-6';
            sep.setAttribute('data-date', sentDay);
            sep.innerHTML = '<span class="badge bg-label-secondary px-3 py-1">' + getDateLabel(sentDay) + '</span>';
            list.appendChild(sep);
        }

        var conv       = document.getElementById('chatConversation');
        var isMine     = senderId === viewerId;
        var initials   = isMine ? (conv && conv.dataset.viewerInitials || '?') : (conv && conv.dataset.otherInitials || '?');
        var pictureUrl = isMine ? (conv && conv.dataset.viewerPicture  || '')  : (conv && conv.dataset.otherPicture  || '');
        var avatarHtml = buildAvatarHtml(pictureUrl, initials);

        var li = document.createElement('li');
        li.className = 'chat-message' + (isMine ? ' chat-message-right' : '') + ' mb-3';

        if (isMine) {
            li.innerHTML =
                '<div class="d-flex overflow-hidden">' +
                  '<div class="chat-message-wrapper flex-grow-1">' +
                    '<div class="chat-message-text p-2">' +
                      '<p class="mb-0" style="white-space:pre-wrap;">' + esc(content) + '<span class="chat-bubble-tail"></span></p>' +
                      '<span class="chat-bubble-meta">' +
                        '<i class="msg-read-receipt icon-base ri ri-check-double-line icon-16px"></i>' +
                        '<small>' + esc(time) + '</small>' +
                      '</span>' +
                    '</div>' +
                  '</div>' +
                  '<div class="user-avatar flex-shrink-0 ms-4"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>' +
                '</div>';
        } else {
            li.innerHTML =
                '<div class="d-flex overflow-hidden">' +
                  '<div class="user-avatar flex-shrink-0 me-4"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>' +
                  '<div class="chat-message-wrapper flex-grow-1">' +
                    '<div class="chat-message-text p-2">' +
                      '<p class="mb-0" style="white-space:pre-wrap;">' + esc(content) + '<span class="chat-bubble-tail"></span></p>' +
                      '<span class="chat-bubble-meta text-body-secondary">' +
                        '<small>' + esc(time) + '</small>' +
                      '</span>' +
                    '</div>' +
                  '</div>' +
                '</div>';
        }

        list.appendChild(li);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    function getDateLabel(dateStr) {
        var today     = new Date();
        var yesterday = new Date(today);
        yesterday.setDate(today.getDate() - 1);
        var todayStr     = today.toISOString().slice(0, 10);
        var yesterdayStr = yesterday.toISOString().slice(0, 10);
        if (dateStr === todayStr)     { return 'Today'; }
        if (dateStr === yesterdayStr) { return 'Yesterday'; }
        var d = new Date(dateStr + 'T00:00:00');
        return d.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
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
