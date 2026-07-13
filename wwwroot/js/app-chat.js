/**
 * Request Chat
 * v1.1.0
 * Phase 1 (page load)   — GET /RequestChat/Visibility    → show/hide the button
 * Phase 2 (canvas open) — GET /RequestChat/Conversation  → server-rendered HTML, connect SignalR
 */
'use strict';

(function () {
    var cfg = window.chatConfig || {};
    var ChatMode = window.ChatMode || { None: 'none', Initiate: 'initiate', Ongoing: 'ongoing' };
    var requestNumber = cfg.requestNumber || '';
    if (!requestNumber) { return; }

    if (new URLSearchParams(window.location.search).get('chat') === 'open') {
        var _oc = document.getElementById('requestChatOffcanvas');
        if (_oc) {
            bootstrap.Offcanvas.getOrCreateInstance(_oc).show();
            var _url = new URL(window.location.href);
            _url.searchParams.delete('chat');
            history.replaceState(null, '', _url.pathname + (_url.search || ''));
        }
    }

    var floatingBtn = document.getElementById('chatFloatingBtn');
    var offcanvas = document.getElementById('requestChatOffcanvas');
    var body = document.getElementById('chatOffcanvasBody');
    if (!floatingBtn || !offcanvas || !body) { return; }

    var mode = ChatMode.None;
    var chatKey = '';
    var viewerId = 0;
    var connection = null;
    var lastMessageDate = '';
    var waitingForEcho = false;
    var echoTimer = null;
    var savedFooterHtml = '';

    // ── Phase 1: visibility check (fast) ─────────────────────────────────────

    fetch('/RequestChat/Visibility?requestNumber=' + encodeURIComponent(requestNumber))
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (!data) { return; }
            floatingBtn.style.display = '';
        })
        .catch(function (e) { console.error('Chat visibility failed:', e); });

    // ── Phase 2: load conversation (on offcanvas open) ────────────────────────

    offcanvas.addEventListener('shown.bs.offcanvas', function () {
        window.chatOpen = true;
        loadConversation();
    });

    offcanvas.addEventListener('hidden.bs.offcanvas', function () {
        window.chatOpen = false;
        if (connection) {
            connection.stop();
            connection = null;
        }
        Block.pulse('#request-view');
        window.location.reload();
    });

    // ── Proposal flow ─────────────────────────────────────────────────────────

    body.addEventListener('click', function (e) {
        if (!e.target.closest('#chatProposalBtn')) { return; }
        if (window.ChatProposal) { window.ChatProposal.open(requestNumber); }
    });

    // ── Proposal accept / reject ──────────────────────────────────────────────

    body.addEventListener('click', function (e) {
        var acceptBtn = e.target.closest('.proposal-accept-btn');
        var rejectBtn = e.target.closest('.proposal-reject-btn');
        if (!acceptBtn && !rejectBtn) { return; }

        var accepted = !!acceptBtn;
        var proposalId = (acceptBtn || rejectBtn).dataset.proposalId;

        if (!window.ChatProposalReact) { return; }
        window.ChatProposalReact.open(proposalId, accepted);
    });

    // ── Cancel Proposal flow ──────────────────────────────────────────────────

    body.addEventListener('click', function (e) {
        var btn = e.target.closest('#chatCancelProposalBtn');
        if (!btn) { return; }
        e.preventDefault();

        Swal.fire({
            title: 'Angebot zurückziehen?',
            text: 'Sind Sie sicher, dass Sie Ihr Angebot zurückziehen möchten?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Ja, zurückziehen',
            cancelButtonText: 'Nein',
            customClass: {
                confirmButton: 'btn btn-danger me-3',
                cancelButton: 'btn btn-label-secondary'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (!result.isConfirmed) { return; }

            Block.pulse('#chatCard');
            fetch('/RequestChat/ProposalCancel', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'requestNumber=' + encodeURIComponent(requestNumber)
            })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (!data.success) { Block.remove('#chatCard'); }
                    // ProposalUpdated SignalR event handles the UI update
                })
                .catch(function () { Block.remove('#chatCard'); });
        });
    });

    // ── Cancel flow ──────────────────────────────────────────────────────────

    body.addEventListener('click', function (e) {
        if (!e.target.closest('#chatCancelBtn')) { return; }

        Swal.fire({
            title: 'Verhandlung beenden?',
            text: 'Möchten Sie die Verhandlung wirklich abbrechen?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Ja, beenden',
            cancelButtonText: 'Nein',
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
        Block.pulse('#chatConversation');

        fetch('/RequestChat/Initiate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'requestNumber=' + encodeURIComponent(requestNumber)
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.success) {
                    loadConversation();
                    return;
                }
                loadConversation();
            })
            .catch(function () {
                Block.remove('#chatConversation');
                btn.disabled = false;
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
        var input = form.querySelector('.message-input');
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
            if (proposalPattern.exec(msg.content)) {
                var footer = body.querySelector('.chat-history-footer');
                if (footer) {
                    var cancelBtn = (msg.senderId === viewerId)
                        ? '<a href="#" id="chatCancelProposalBtn" class="text-danger fw-bold ms-2" style="cursor:pointer;"' +
                        ' data-bs-toggle="tooltip" data-bs-placement="top" title="Angebot stornieren">' +
                            '<i class="icon-base ri ri-hand icon-md text-danger"></i></a>'
                        : '';
                    footer.outerHTML =
                        '<div class="chat-history-footer shadow-xs mt-0 p-0">' +
                        '<div class="alert alert-warning m-0" role="alert">' +
                        '<div class="d-flex align-items-center">' +
                        '<i class="icon-base ri ri-error-warning-line me-2 icon-22px"></i>' +
                        '<strong class="pe-1">Ausstehende Antwort auf Angebot</strong>' +
                        cancelBtn +
                        '</div></div></div>';
                    var cancelEl = body.querySelector('#chatCancelProposalBtn');
                    if (cancelEl) { new bootstrap.Tooltip(cancelEl); }
                }
            }
            if (msg.senderId !== viewerId) {
                connection.invoke('MarkRead', key).catch(function (err) { console.error('MarkRead error:', err); });
            }
        });

        connection.on('ProposalUpdated', function (data) {
            var card = document.querySelector('[data-proposal-card-id="' + data.proposalId + '"]');
            if (!card) { return; }

            if (data.proposalStatus === 'canceled') {
                card.classList.remove('border-warning');
                card.classList.add('border-secondary');
                var icon = card.querySelector('.ri-shake-hands-line');
                if (icon) {
                    icon.classList.remove('text-warning');
                    icon.classList.add('text-secondary');
                }
                var hr = card.querySelector('hr');
                if (hr) {
                    hr.className = 'border border-secondary mt-1 mb-3';
                    while (hr.nextSibling) { card.removeChild(hr.nextSibling); }
                    var deleted = document.createElement('div');
                    deleted.className = 'd-flex align-items-center justify-content-center gap-1 mb-1 text-secondary';
                    deleted.innerHTML = '<i class="icon-base ri ri-prohibited-line icon-18px"></i>' +
                        '<span class="fst-italic">Die Nachricht wurde gelöscht.</span>';
                    card.appendChild(deleted);
                }
            } else {
                var color = data.proposalStatus === 'accepted' ? 'success' : 'danger';
                card.classList.remove('border-warning', 'border-success', 'border-danger');
                card.classList.add('border-' + color);
                var icon = card.querySelector('.ri-shake-hands-line');
                if (icon) {
                    icon.classList.remove('text-warning', 'text-success', 'text-danger');
                    icon.classList.add('text-' + color);
                }
                var hr = card.querySelector('hr');
                if (hr) { hr.className = 'border border-' + color + ' mt-1 mb-3'; }
                var actions = card.querySelector('.proposal-actions');
                if (actions) { actions.remove(); }
            }

            if (data.proposalStatus === 'accepted' || data.proposalStatus === 'rejected' || data.proposalStatus === 'canceled') {
                Block.remove('#chatCard');
                if (savedFooterHtml) {
                    var footer = body.querySelector('.chat-history-footer');
                    if (footer) { footer.outerHTML = savedFooterHtml; }
                    if (data.proposalStatus === 'accepted') {
                        var cancelBtn = body.querySelector('#chatCancelBtn');
                        if (cancelBtn) { cancelBtn.remove(); }
                        var proposalBtn = body.querySelector('#chatProposalBtn');
                        if (proposalBtn) { proposalBtn.remove(); }
                    }
                } else {
                    loadConversation();
                }
            }
        });

        connection.on('ChatCancelled', function () {
            window.location.reload();
        });

        connection.on('MessagesRead', function () {
            var icons = document.querySelectorAll('.msg-read-receipt');
            for (var i = 0; i < icons.length; i++) {
                icons[i].classList.add('text-warning');
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

                body.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
                    new bootstrap.Tooltip(el);
                });

                var conv = document.getElementById('chatConversation');
                if (!conv) { return; }

                mode = conv.dataset.mode || ChatMode.None;
                chatKey = conv.dataset.chatKey || '';
                viewerId = parseInt(conv.dataset.viewerId || '0', 10);

                if (mode === ChatMode.Ongoing) {
                    var historyBody = body.querySelector('.chat-history-body');
                    if (historyBody) { new PerfectScrollbar(historyBody); }
                    var separators = body.querySelectorAll('.chat-date-separator[data-date]');
                    lastMessageDate = separators.length ? separators[separators.length - 1].getAttribute('data-date') : '';
                    var footer = body.querySelector('.chat-history-footer');
                    savedFooterHtml = (footer && footer.querySelector('.form-send-message')) ? footer.outerHTML : '';
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

    var proposalPattern = /^#bewegdeal-proposal-(\d+)$/;

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

        var conv = document.getElementById('chatConversation');
        var isMine = senderId === viewerId;
        var initials = isMine ? (conv && conv.dataset.viewerInitials || '?') : (conv && conv.dataset.otherInitials || '?');
        var pictureUrl = isMine ? (conv && conv.dataset.viewerPicture || '') : (conv && conv.dataset.otherPicture || '');
        var avatarHtml = buildAvatarHtml(pictureUrl, initials);

        var proposalMatch = proposalPattern.exec(content);
        if (proposalMatch) {
            var proposalId = proposalMatch[1];
            var li = document.createElement('li');
            li.className = 'chat-message' + (isMine ? ' chat-message-right' : '') + ' mb-3';
            var avatarSide = isMine
                ? '<div class="user-avatar flex-shrink-0 ms-2"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>'
                : '<div class="user-avatar flex-shrink-0 me-2"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>';
            var cardSlot = '<div class="chat-message-wrapper flex-grow-1" id="proposal-slot-' + proposalId + '"></div>';
            li.innerHTML = isMine
                ? '<div class="d-flex overflow-hidden">' + cardSlot + avatarSide + '</div>'
                : '<div class="d-flex overflow-hidden">' + avatarSide + cardSlot + '</div>';
            list.appendChild(li);
            scrollToBottom();
            fetch('/RequestChat/ProposalCard?proposalId=' + encodeURIComponent(proposalId))
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    var slot = document.getElementById('proposal-slot-' + proposalId);
                    if (slot) { slot.innerHTML = html; }
                    scrollToBottom();
                })
                .catch(function (e) { });
            return;
        }

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
                '<div class="user-avatar flex-shrink-0 ms-2"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>' +
                '</div>';
        } else {
            li.innerHTML =
                '<div class="d-flex overflow-hidden">' +
                '<div class="user-avatar flex-shrink-0 me-2"><div class="avatar avatar-sm">' + avatarHtml + '</div></div>' +
                '<div class="chat-message-wrapper flex-grow-1">' +
                '<div class="chat-message-text p-2">' +
                '<p class="mb-0" style="white-space:pre-wrap;">' + esc(content) + '<span class="chat-bubble-tail"></span></p>' +
                '<span class="chat-bubble-meta">' +
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
        var today = new Date();
        var yesterday = new Date(today);
        yesterday.setDate(today.getDate() - 1);
        var todayStr = today.toISOString().slice(0, 10);
        var yesterdayStr = yesterday.toISOString().slice(0, 10);
        if (dateStr === todayStr) { return 'Heute'; }
        if (dateStr === yesterdayStr) { return 'Gestern'; }
        var d = new Date(dateStr + 'T00:00:00');
        return d.toLocaleDateString('de-DE', { month: 'long', day: 'numeric', year: 'numeric' });
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
            '<span>Chat konnte nicht geladen werden. Bitte versuchen Sie es erneut.</span>' +
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
