/**
 * New Request wizard — Bewegdeal
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {

  // ── Notyf helper ──────────────────────────────────────────────────────────
  const notyf = new Notyf({ duration: 4000, position: { x: 'center', y: 'top' } });

  // ── bs-stepper ────────────────────────────────────────────────────────────
  const wizardEl = document.querySelector('#newRequestWizard');
  if (!wizardEl) { return; }

  const stepper = new Stepper(wizardEl, { linear: true, animation: true });

  // ── State ─────────────────────────────────────────────────────────────────
  let uploadedPhotos = [];   // File objects
  let uploadedVideo  = null; // File object | null
  let mainPhotoIdx   = 0;    // index inside uploadedPhotos that is the main photo

  // ── Type cards ────────────────────────────────────────────────────────────
  const typeCards   = document.querySelectorAll('.type-card');
  const selectedTypeInput = document.querySelector('#selectedType');

  const typeColorMap = {
    moving:    { border: 'var(--bs-primary)', bg: 'rgba(var(--bs-primary-rgb),.06)' },
    removal:   { border: 'var(--bs-danger)',  bg: 'rgba(var(--bs-danger-rgb),.06)'  },
    pickup:    { border: 'var(--bs-success)', bg: 'rgba(var(--bs-success-rgb),.06)' },
    transport: { border: 'var(--bs-warning)', bg: 'rgba(var(--bs-warning-rgb),.06)' }
  };

  const typeLabelMap = {
    moving:    'Moving',
    removal:   'Junk Removal',
    pickup:    'Store Pickup',
    transport: 'Vehicle Transport'
  };

  typeCards.forEach(card => {
    card.addEventListener('click', function () {
      // Clear all
      typeCards.forEach(c => {
        c.style.borderColor = '';
        c.style.background  = '';
      });
      // Highlight selected
      const t    = this.dataset.type;
      const conf = typeColorMap[t] || {};
      this.style.borderColor = conf.border || 'var(--bs-primary)';
      this.style.background  = conf.bg     || 'rgba(var(--bs-primary-rgb),.06)';
      selectedTypeInput.value = t;
    });
  });

  // ── Step navigation ───────────────────────────────────────────────────────
  // Map each step index to its validator (1-based, matching step order)
  function validateStep(step) {
    switch (step) {
      case 1: {
        if (!selectedTypeInput.value) {
          notyf.error('Please select a service type.');
          return false;
        }
        return true;
      }
      case 2: {
        const name = (document.querySelector('#requestName')?.value || '').trim();
        if (name.length < 3) {
          notyf.error('Please enter a request title (at least 3 characters).');
          document.querySelector('#requestName')?.focus();
          return false;
        }
        return true;
      }
      default:
        return true;
    }
  }

  // Current step index (1-based)
  let currentStep = 1;

  document.querySelectorAll('.btn-next').forEach(btn => {
    btn.addEventListener('click', function () {
      if (!validateStep(currentStep)) { return; }
      if (currentStep === 4) { updateSummary(); }
      stepper.next();
      currentStep++;
    });
  });

  document.querySelectorAll('.btn-prev').forEach(btn => {
    btn.addEventListener('click', function () {
      stepper.previous();
      currentStep--;
    });
  });

  // ── File upload ───────────────────────────────────────────────────────────
  const uploadZone     = document.querySelector('#uploadZone');
  const photoUploadBtn = document.querySelector('#photoUploadBtn');
  const videoUploadBtn = document.querySelector('#videoUploadBtn');
  const photoPreview   = document.querySelector('#photoPreview');
  const videoPreview   = document.querySelector('#videoPreview');
  const photosInput    = document.querySelector('#photosInput');    // hidden form input
  const videoFileInput = document.querySelector('#videoFileInput'); // hidden form input

  // Invisible pickers (separate from the form inputs)
  const photoPickerInput = document.createElement('input');
  photoPickerInput.type     = 'file';
  photoPickerInput.multiple = true;
  photoPickerInput.accept   = 'image/*';

  const videoPickerInput = document.createElement('input');
  videoPickerInput.type   = 'file';
  videoPickerInput.accept = 'video/*';

  photoUploadBtn?.addEventListener('click', () => photoPickerInput.click());
  videoUploadBtn?.addEventListener('click', () => videoPickerInput.click());

  // Click anywhere in the drop zone also opens photo picker
  uploadZone?.addEventListener('click', function (e) {
    if (!e.target.closest('button')) { photoPickerInput.click(); }
  });

  // Drag-and-drop
  uploadZone?.addEventListener('dragover', e => {
    e.preventDefault();
    uploadZone.style.borderColor = 'var(--bs-primary)';
    uploadZone.style.background  = 'rgba(var(--bs-primary-rgb),.04)';
  });
  uploadZone?.addEventListener('dragleave', () => {
    uploadZone.style.borderColor = '';
    uploadZone.style.background  = '';
  });
  uploadZone?.addEventListener('drop', e => {
    e.preventDefault();
    uploadZone.style.borderColor = '';
    uploadZone.style.background  = '';
    const files  = Array.from(e.dataTransfer.files);
    const imgs   = files.filter(f => f.type.startsWith('image/'));
    const vids   = files.filter(f => f.type.startsWith('video/'));
    if (imgs.length) { addPhotos(imgs); }
    if (vids.length) { addVideo(vids[0]); }
  });

  photoPickerInput.addEventListener('change', function () {
    addPhotos(Array.from(this.files));
    this.value = '';
  });

  videoPickerInput.addEventListener('change', function () {
    if (this.files.length) { addVideo(this.files[0]); }
    this.value = '';
  });

  // ── Photo management ──────────────────────────────────────────────────────
  function addPhotos(files) {
    for (const file of files) {
      if (uploadedPhotos.length >= 5) {
        notyf.error('Maximum 5 photos allowed.');
        break;
      }
      if (file.size > 2 * 1024 * 1024) {
        notyf.error(`"${file.name}" exceeds the 2 MB photo limit.`);
        continue;
      }
      uploadedPhotos.push(file);
    }
    renderPhotoPreview();
    syncPhotosInput();
  }

  function removePhoto(idx) {
    uploadedPhotos.splice(idx, 1);
    if (mainPhotoIdx >= uploadedPhotos.length) {
      mainPhotoIdx = Math.max(0, uploadedPhotos.length - 1);
    }
    renderPhotoPreview();
    syncPhotosInput();
  }

  function setMainPhoto(idx) {
    mainPhotoIdx = idx;
    document.querySelector('#mainPhotoIndex').value = idx;
    renderPhotoPreview();
  }

  function renderPhotoPreview() {
    if (!photoPreview) { return; }
    photoPreview.innerHTML = '';

    uploadedPhotos.forEach((file, idx) => {
      const url  = URL.createObjectURL(file);
      const isMain = idx === mainPhotoIdx;

      const col  = document.createElement('div');
      col.className = 'col-auto';

      col.innerHTML = `
        <div class="position-relative d-inline-block">
          <img src="${url}" alt="photo ${idx + 1}"
               style="width:110px;height:110px;object-fit:cover;border-radius:10px;display:block;"
               class="${isMain ? 'border border-3 border-primary' : 'border border-1 border-secondary'}">
          ${isMain ? `<span class="badge bg-primary position-absolute bottom-0 start-50 translate-middle-x mb-1" style="font-size:10px">Main</span>` : ''}
          <button type="button"
                  class="btn btn-danger btn-icon rounded-circle position-absolute top-0 end-0"
                  style="width:22px;height:22px;padding:0;font-size:11px;transform:translate(40%,-40%)"
                  data-remove="${idx}" title="Remove">
            <i class="ri ri-close-line"></i>
          </button>
        </div>
        ${!isMain ? `
        <div class="text-center mt-1">
          <button type="button" class="btn btn-sm btn-text-primary px-2 py-0" data-setmain="${idx}" style="font-size:11px">
            Set as main
          </button>
        </div>` : '<div class="text-center mt-1" style="height:24px"></div>'}
      `;
      photoPreview.appendChild(col);
    });

    // Bind remove buttons
    photoPreview.querySelectorAll('[data-remove]').forEach(btn => {
      btn.addEventListener('click', () => removePhoto(parseInt(btn.dataset.remove)));
    });
    // Bind set-as-main buttons
    photoPreview.querySelectorAll('[data-setmain]').forEach(btn => {
      btn.addEventListener('click', () => setMainPhoto(parseInt(btn.dataset.setmain)));
    });

    // Update counter badge
    const photoCount = document.querySelector('#photoCount');
    if (photoCount) { photoCount.textContent = `${uploadedPhotos.length} / 5`; }
  }

  // ── Video management ──────────────────────────────────────────────────────
  function addVideo(file) {
    if (file.size > 10 * 1024 * 1024) {
      notyf.error('Video exceeds the 10 MB limit.');
      return;
    }
    uploadedVideo = file;
    renderVideoPreview();
    syncVideoInput();
  }

  function renderVideoPreview() {
    if (!videoPreview) { return; }
    const statusEl = document.querySelector('#videoStatus');

    if (!uploadedVideo) {
      videoPreview.innerHTML = '';
      if (statusEl) { statusEl.textContent = 'None'; }
      return;
    }

    const sizeMB = (uploadedVideo.size / (1024 * 1024)).toFixed(1);
    if (statusEl) { statusEl.textContent = '1 added'; }

    videoPreview.innerHTML = `
      <div class="d-flex align-items-center gap-3 p-3 rounded-3 border bg-light-subtle">
        <div class="avatar-initial rounded bg-label-primary d-flex align-items-center justify-content-center" style="width:48px;height:48px;flex-shrink:0">
          <i class="ri ri-video-line icon-24px"></i>
        </div>
        <div class="flex-grow-1 min-w-0">
          <p class="mb-0 fw-medium text-truncate">${uploadedVideo.name}</p>
          <small class="text-muted">${sizeMB} MB</small>
        </div>
        <button type="button" id="removeVideo" class="btn btn-icon btn-text-danger rounded-pill" title="Remove">
          <i class="ri ri-delete-bin-7-line icon-20px"></i>
        </button>
      </div>
    `;

    document.querySelector('#removeVideo')?.addEventListener('click', function () {
      uploadedVideo = null;
      renderVideoPreview();
      syncVideoInput();
    });
  }

  // ── Sync files to hidden form inputs ─────────────────────────────────────
  function syncPhotosInput() {
    if (!photosInput) { return; }
    const dt = new DataTransfer();
    // Put main photo first so the server always receives it at index 0
    if (uploadedPhotos.length) {
      dt.items.add(uploadedPhotos[mainPhotoIdx]);
      uploadedPhotos.forEach((f, i) => { if (i !== mainPhotoIdx) { dt.items.add(f); } });
    }
    photosInput.files = dt.files;
  }

  function syncVideoInput() {
    if (!videoFileInput) { return; }
    const dt = new DataTransfer();
    if (uploadedVideo) { dt.items.add(uploadedVideo); }
    videoFileInput.files = dt.files;
  }

  // ── Summary (step 5) ──────────────────────────────────────────────────────
  function updateSummary() {
    const type   = selectedTypeInput?.value || '';
    const name   = (document.querySelector('#requestName')?.value || '').trim();
    const pickup = (document.querySelector('#pickupAddress')?.value || '').trim();
    const del_   = (document.querySelector('#deliveryAddress')?.value || '').trim();

    const el = id => document.querySelector(id);
    if (el('#summaryType'))     { el('#summaryType').textContent     = typeLabelMap[type] || type || '—'; }
    if (el('#summaryName'))     { el('#summaryName').textContent     = name   || '—'; }
    if (el('#summaryPhotos'))   { el('#summaryPhotos').textContent   = uploadedPhotos.length; }
    if (el('#summaryPickup'))   { el('#summaryPickup').textContent   = pickup || '—'; }
    if (el('#summaryDelivery')) { el('#summaryDelivery').textContent = del_   || '—'; }
  }

  // ── Form submit ───────────────────────────────────────────────────────────
  document.querySelector('#newRequestForm')?.addEventListener('submit', function (e) {
    e.preventDefault();

    // Final validation
    if (!selectedTypeInput?.value) {
      notyf.error('Please select a service type.');
      stepper.to(0);
      currentStep = 1;
      return;
    }
    const name = (document.querySelector('#requestName')?.value || '').trim();
    if (name.length < 3) {
      notyf.error('Please enter a request title.');
      stepper.to(1);
      currentStep = 2;
      return;
    }

    // Sync files to hidden inputs before native submit
    syncPhotosInput();
    syncVideoInput();

    // Disable submit button to prevent double-submit
    const submitBtn = document.querySelector('#submitBtn');
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Publishing…';
    }

    this.submit();
  });

});
