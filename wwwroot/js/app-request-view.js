/**
 * Request View — Swiper Thumbs Gallery
 * v1.1.4
 *
 * `requestFiles` is always defined by the inline <script> in View.cshtml.
 */

'use strict';

(function () {

    const galleryThumbsEl = document.querySelector('.gallery-thumbs');
    const galleryTopEl = document.querySelector('.gallery-top');

    let galleryInstance;

    if (galleryThumbsEl) {
        galleryInstance = new Swiper(galleryThumbsEl, {
            spaceBetween: 10,
            slidesPerView: mediaCount,
            freeMode: false,
            watchSlidesVisibility: true,
            watchSlidesProgress: true
        });
    }

    if (galleryTopEl) {
        new Swiper(galleryTopEl, {
            spaceBetween: 10,
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev'
            },
            thumbs: {
                swiper: galleryInstance ?? null
            }
        });
    }

    // ── Cancel button ─────────────────────────────────────────────────────────
    function setLoadingMessage(text) {
        Loading.standard({
            backgroundColor: 'rgba(' + window.Helpers.getCssVar('black-rgb') + ', 0.5)',
            svgSize: '0px'
        });
        const loadingEl = document.querySelector('.notiflix-loading');
        if (loadingEl) {
            loadingEl.innerHTML = `
        <div class="d-flex justify-content-center">
          <p class="mb-0 text-white">${text}</p>
          <div class="sk-wave m-0">
            <div class="sk-rect sk-wave-rect"></div>
            <div class="sk-rect sk-wave-rect"></div>
            <div class="sk-rect sk-wave-rect"></div>
            <div class="sk-rect sk-wave-rect"></div>
            <div class="sk-rect sk-wave-rect"></div>
          </div>
        </div>`;
        }
    }

    // ── Resolve button ────────────────────────────────────────────────────────
    const resolveBtn = document.getElementById('btnRequestResolve');
    if (resolveBtn) {
        resolveBtn.addEventListener('click', function () {
            window.ResolveModal.open(window.Config.requestNumber);
        });
    }

    const cancelBtn = document.getElementById('btnRequestCancel');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', async function () {
            const confirmed = await Swal.fire({
                title: 'Aktion bestätigen',
                html: 'Möchten Sie die Anfrage wirklich <span class="text-danger fw-bold">Stornieren</span>?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Ja, stornieren',
                cancelButtonText: 'Nein, behalten',
                customClass: {
                    confirmButton: 'btn btn-danger me-3',
                    cancelButton: 'btn btn-label-secondary'
                },
                buttonsStyling: false
            });

            if (!confirmed.isConfirmed) { return; }

            cancelBtn.disabled = true;
            setLoadingMessage('Anfrage wird storniert...');

            try {
                const fd = new FormData();
                fd.append('number', window.Config.requestNumber);

                const response = await fetch('/Request/Cancel', {
                    method: 'POST',
                    body: fd,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                const result = await response.json();

                if (result.success) {
                    Loading.standard({
                        backgroundColor: 'rgba(' + window.Helpers.getCssVar('black-rgb') + ', 0.5)',
                        svgSize: '0px'
                    });
                    const successEl = document.querySelector('.notiflix-loading');
                    if (successEl) {
                        successEl.innerHTML = `<div class="px-12 py-3 bg-success text-white">Erfolgreich</div>`;
                    }
                    setTimeout(function () {
                        window.location.href = '/Request/List';
                    }, 2000);
                } else {
                    Loading.remove();
                    Notiflix.Notify.failure(result.error ?? 'Es ist ein Fehler aufgetreten. Bitte versuchen Sie es erneut.');
                    cancelBtn.disabled = false;
                }
            } catch {
                Loading.remove();
                Notiflix.Notify.failure('Es ist ein Fehler aufgetreten. Bitte versuchen Sie es erneut.');
                cancelBtn.disabled = false;
            }
        });
    }

    // ── Agreement Cancel button ───────────────────────────────────────────────
    const agreementCancelBtn = document.getElementById('btnAgreementCancel');
    if (agreementCancelBtn) {
        agreementCancelBtn.addEventListener('click', async function () {
            const isCustomer = window.Config?.isCustomer === true;

            const title = isCustomer ? 'Auftrag stornieren' : 'Auftrag absagen';
            const bodyHtml = isCustomer
                ? `<p class="text-start">Bei einer Stornierung können nach den wirksam vereinbarten Stornobedingungen der Firma oder gesetzlichen Vorschriften Kosten anfallen.</p>
                   <p class="text-start">Üben Sie ein bestehendes gesetzliches Rücktrittsrecht wirksam aus, fällt keine Stornogebühr an. Für bereits erbrachte Leistungen kann unter den gesetzlichen Voraussetzungen ein anteiliges Entgelt anfallen.</p>
                   <p class="text-start">Ihre gesetzlichen Rechte bleiben unberührt.</p>
                   <p class="text-start mb-0">Möchten Sie den Auftrag stornieren?</p>`
                : `<p class="text-start">Eine Absage befreit Sie nicht automatisch von Ihren vertraglichen Pflichten. Unter den gesetzlichen Voraussetzungen können Sie verpflichtet sein, dem Kunden Schäden zu ersetzen, etwa Mehrkosten für eine Ersatzfirma.</p>
                   <p class="text-start">Bestehende Rechte und Ansprüche beider Seiten bleiben unberührt.</p>
                   <p class="text-start mb-0">Möchten Sie den Auftrag absagen?</p>`;
            const confirmText = isCustomer ? 'Ja, stornieren' : 'Ja, absagen';

            const confirmed = await Swal.fire({
                title: title,
                html: bodyHtml,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: confirmText,
                cancelButtonText: 'Nein, behalten',
                customClass: {
                    confirmButton: 'btn btn-danger me-3',
                    cancelButton: 'btn btn-label-secondary'
                },
                buttonsStyling: false
            });

            if (!confirmed.isConfirmed) { return; }

            agreementCancelBtn.disabled = true;
            setLoadingMessage(isCustomer ? 'Auftrag wird storniert...' : 'Auftrag wird abgesagt...');

            try {
                const fd = new FormData();
                fd.append('number', window.Config.requestNumber);

                const response = await fetch('/Request/CancelAgreement', {
                    method: 'POST',
                    body: fd,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                const result = await response.json();

                if (result.success) {
                    Loading.standard({
                        backgroundColor: 'rgba(' + window.Helpers.getCssVar('black-rgb') + ', 0.5)',
                        svgSize: '0px'
                    });
                    const successEl = document.querySelector('.notiflix-loading');
                    if (successEl) {
                        successEl.innerHTML = `<div class="px-12 py-3 bg-success text-white">Erfolgreich</div>`;
                    }
                    setTimeout(function () {
                        window.location.href = '/Request/List';
                    }, 2000);
                } else {
                    Loading.remove();
                    Notiflix.Notify.failure(result.error ?? 'Es ist ein Fehler aufgetreten. Bitte versuchen Sie es erneut.');
                    agreementCancelBtn.disabled = false;
                }
            } catch {
                Loading.remove();
                Notiflix.Notify.failure('Es ist ein Fehler aufgetreten. Bitte versuchen Sie es erneut.');
                agreementCancelBtn.disabled = false;
            }
        });
    }

})();
