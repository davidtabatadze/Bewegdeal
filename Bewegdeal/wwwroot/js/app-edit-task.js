/**
 * Edit Task — Bewegdeal
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {

  const notyf = new Notyf({ duration: 4000, position: { x: 'center', y: 'top' } });

  // ── Type cards ────────────────────────────────────────────────────────────
  const typeColorMap = {
    moving:    { border: 'var(--bs-primary)', bg: 'rgba(var(--bs-primary-rgb),.06)' },
    removal:   { border: 'var(--bs-danger)',  bg: 'rgba(var(--bs-danger-rgb),.06)'  },
    pickup:    { border: 'var(--bs-success)', bg: 'rgba(var(--bs-success-rgb),.06)' },
    transport: { border: 'var(--bs-warning)', bg: 'rgba(var(--bs-warning-rgb),.06)' }
  };

  const selectedTypeInput = document.querySelector('#selectedType');
  const typeCards         = document.querySelectorAll('.type-card');

  function selectType(type) {
    typeCards.forEach(c => { c.style.borderColor = ''; c.style.background = ''; });
    const card = document.querySelector(`.type-card[data-type="${type}"]`);
    if (card) {
      const conf = typeColorMap[type] || {};
      card.style.borderColor = conf.border || '';
      card.style.background  = conf.bg     || '';
    }
    if (selectedTypeInput) { selectedTypeInput.value = type; }
  }

  // Pre-select current type on load
  if (selectedTypeInput?.value) { selectType(selectedTypeInput.value); }

  typeCards.forEach(card => {
    card.addEventListener('click', () => selectType(card.dataset.type));
  });

  // ── Remove existing main photo ────────────────────────────────────────────
  const removeMainPhotoBtn    = document.querySelector('#removeMainPhotoBtn');
  const removeMainPhotoInput  = document.querySelector('#removeMainPhoto');
  const mainPhotoWrapper      = document.querySelector('#mainPhotoWrapper');

  removeMainPhotoBtn?.addEventListener('click', function () {
    if (removeMainPhotoInput) { removeMainPhotoInput.value = 'true'; }
    mainPhotoWrapper?.closest('.d-flex')?.remove();
    this.remove();
  });

  // ── Remove existing additional media ─────────────────────────────────────
  const removeMediaInput = document.querySelector('#removeMedia');
  let   removedPaths     = [];

  document.querySelectorAll('.remove-existing').forEach(btn => {
    btn.addEventListener('click', function () {
      const item = this.closest('.existing-media-item');
      const path = item?.dataset.path;
      if (path) {
        removedPaths.push(path);
        removeMediaInput.value = removedPaths.join(',');
      }
      item?.remove();
    });
  });

  // ── New file upload ───────────────────────────────────────────────────────
  const uploadZone     = document.querySelector('#uploadZone');
  const photoUploadBtn = document.querySelector('#photoUploadBtn');
  const videoUploadBtn = document.querySelector('#videoUploadBtn');
  const photoPreview   = document.querySelector('#photoPreview');
  const videoPreview   = document.querySelector('#videoPreview');
  const photosInput    = document.querySelector('#photosInput');
  const videoFileInput = document.querySelector('#videoFileInput');

  let uploadedPhotos = [];
  let uploadedVideo  = null;
  let mainPhotoIdx   = 0;

  const photoPickerInput = document.createElement('input');
  photoPickerInput.type     = 'file';
  photoPickerInput.multiple = true;
  photoPickerInput.accept   = 'image/*';

  const videoPickerInput = document.createElement('input');
  videoPickerInput.type   = 'file';
  videoPickerInput.accept = 'video/*';

  photoUploadBtn?.addEventListener('click', () => photoPickerInput.click());
  videoUploadBtn?.addEventListener('click', () => videoPickerInput.click());

  uploadZone?.addEventListener('click', function (e) {
    if (!e.target.closest('button')) { photoPickerInput.click(); }
  });

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
    const files = Array.from(e.dataTransfer.files);
    const imgs  = files.filter(f => f.type.startsWith('image/'));
    const vids  = files.filter(f => f.type.startsWith('video/'));
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

  function addPhotos(files) {
    for (const file of files) {
      if (uploadedPhotos.length >= 5) { notyf.error('Maximum 5 new photos allowed.'); break; }
      if (file.size > 2 * 1024 * 1024) { notyf.error(`"${file.name}" exceeds the 2 MB photo limit.`); continue; }
      uploadedPhotos.push(file);
    }
    renderPhotoPreview();
    syncPhotosInput();
  }

  function removePhoto(idx) {
    uploadedPhotos.splice(idx, 1);
    if (mainPhotoIdx >= uploadedPhotos.length) { mainPhotoIdx = Math.max(0, uploadedPhotos.length - 1); }
    renderPhotoPreview();
    syncPhotosInput();
  }

  function setMainPhoto(idx) {
    mainPhotoIdx = idx;
    renderPhotoPreview();
  }

  function renderPhotoPreview() {
    if (!photoPreview) { return; }
    photoPreview.innerHTML = '';

    uploadedPhotos.forEach((file, idx) => {
      const url    = URL.createObjectURL(file);
      const isMain = idx === mainPhotoIdx;
      const col    = document.createElement('div');
      col.className = 'col-auto';
      col.innerHTML = `
        <div class="position-relative d-inline-block">
          <img src="${url}" alt="photo ${idx + 1}"
               style="width:110px;height:110px;object-fit:cover;border-radius:10px;display:block;"
               class="${isMain ? 'border border-3 border-primary' : 'border border-1 border-secondary'}">
          ${isMain ? `<span class="badge bg-primary position-absolute bottom-0 start-50 translate-middle-x mb-1" style="font-size:10px">Main</span>` : ''}
          <button type="button" class="btn btn-danger btn-icon rounded-circle position-absolute top-0 end-0"
                  style="width:22px;height:22px;padding:0;font-size:11px;transform:translate(40%,-40%)"
                  data-remove="${idx}" title="Remove">
            <i class="ri ri-close-line"></i>
          </button>
        </div>
        ${!isMain ? `<div class="text-center mt-1"><button type="button" class="btn btn-sm btn-text-primary px-2 py-0" data-setmain="${idx}" style="font-size:11px">Set as main</button></div>` : '<div style="height:24px"></div>'}
      `;
      photoPreview.appendChild(col);
    });

    photoPreview.querySelectorAll('[data-remove]').forEach(btn =>
      btn.addEventListener('click', () => removePhoto(parseInt(btn.dataset.remove)))
    );
    photoPreview.querySelectorAll('[data-setmain]').forEach(btn =>
      btn.addEventListener('click', () => setMainPhoto(parseInt(btn.dataset.setmain)))
    );

    const photoCount = document.querySelector('#photoCount');
    if (photoCount) { photoCount.textContent = `${uploadedPhotos.length} / 5`; }
  }

  function addVideo(file) {
    if (file.size > 10 * 1024 * 1024) { notyf.error('Video exceeds the 10 MB limit.'); return; }
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
        <button type="button" id="removeVideo" class="btn btn-icon btn-text-danger rounded-pill">
          <i class="ri ri-delete-bin-7-line icon-20px"></i>
        </button>
      </div>`;
    document.querySelector('#removeVideo')?.addEventListener('click', () => {
      uploadedVideo = null;
      renderVideoPreview();
      syncVideoInput();
    });
  }

  function syncPhotosInput() {
    if (!photosInput) { return; }
    const dt = new DataTransfer();
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

  // ── Form submit ───────────────────────────────────────────────────────────
  document.querySelector('#editTaskForm')?.addEventListener('submit', function (e) {
    e.preventDefault();

    if (!selectedTypeInput?.value) {
      notyf.error('Please select a service type.');
      return;
    }
    const name = (document.querySelector('#requestName')?.value || '').trim();
    if (name.length < 3) {
      notyf.error('Please enter a request title (at least 3 characters).');
      document.querySelector('#requestName')?.focus();
      return;
    }

    syncPhotosInput();
    syncVideoInput();

    const submitBtn = document.querySelector('#submitBtn');
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Saving…';
    }

    this.submit();
  });

});
