/**
 * Request Create
 */

'use strict';

Dropzone.autoDiscover = false;

(function () {

  // imageMaxCount, imageMaxSize, videoMaxCount, videoMaxSize
  // are declared as globals by the inline <script> block in PageScripts (before this file loads)

  // ── Notyf ─────────────────────────────────────────────────────────────────
  const notyf = new Notyf({ duration: 5000, position: { x: 'center', y: 'top' } });

  // ── Dropzone preview template ─────────────────────────────────────────────
  const previewTemplate = `<div class="dz-preview dz-file-preview">
  <div class="dz-details">
    <div class="dz-thumbnail">
      <img data-dz-thumbnail>
      <span class="dz-nopreview">No preview</span>
      <div class="dz-success-mark"></div>
      <div class="dz-error-mark"></div>
      <div class="dz-error-message"><span data-dz-errormessage></span></div>
      <div class="progress">
        <div class="progress-bar progress-bar-primary" role="progressbar" aria-valuemin="0" aria-valuemax="100" data-dz-uploadprogress></div>
      </div>
    </div>
    <div class="dz-filename" data-dz-name></div>
    <div class="dz-size" data-dz-size></div>
  </div>
</div>`;

  // ── Combined media Dropzone ───────────────────────────────────────────────
  let mediaDropzone = null;
  let mainImageFile = null;

  const isImageFile = file => file.type.startsWith('image/');

  const mediaDropzoneEl = document.querySelector('#dropzone-media');
  if (mediaDropzoneEl) {
    mediaDropzone = new Dropzone(mediaDropzoneEl, {
      url:              '#',
      autoProcessQueue: false,
      addRemoveLinks:   true,
      maxFilesize:      Math.max(imageMaxSize, videoMaxSize),
      acceptedFiles:    '.png,.jpg,.jpeg,.mp4,.mov',
      previewTemplate:  previewTemplate,
      dictInvalidFileType: 'Only PNG, JPG, MP4 and MOV files are accepted.',
      dictFileTooBig:   'File is too large.'
    });

    mediaDropzone.on('addedfile', function (file) {
      document.getElementById('mediaError')?.classList.add('d-none');
      if (!isImageFile(file)) { return; }

      // Append main badge to image thumbnails only
      const thumbnail = file.previewElement.querySelector('.dz-thumbnail');
      const badge = document.createElement('span');
      badge.className = 'dz-main-badge badge position-absolute bottom-0 start-0 m-1';
      badge.style.cursor = 'pointer';
      badge.addEventListener('click', function () {
        setMainFile(file);
      });
      thumbnail.appendChild(badge);

      // First image → auto-set as main
      if (!mainImageFile) {
        setMainFile(file);
      } else {
        updateMainBadges();
      }
    });

    mediaDropzone.on('removedfile', function (file) {
      if (mainImageFile === file) {
        const nextImage = mediaDropzone.files.find(isImageFile);
        mainImageFile = nextImage ?? null;
      }
      updateMainBadges();
    });
  }

  function setMainFile(file) {
    mainImageFile = file;
    updateMainBadges();
  }

  function updateMainBadges() {
    if (!mediaDropzone) { return; }
    mediaDropzone.files.filter(isImageFile).forEach(function (f) {
      const badge = f.previewElement.querySelector('.dz-main-badge');
      if (!badge) { return; }
      if (f === mainImageFile) {
        badge.textContent = '★ Main';
        badge.classList.remove('bg-secondary');
        badge.classList.add('bg-primary');
      } else {
        badge.textContent = 'Set Main';
        badge.classList.remove('bg-primary');
        badge.classList.add('bg-secondary');
      }
    });
  }

  function getMainImageIndex() {
    if (!mediaDropzone || !mainImageFile) { return 0; }
    const images = mediaDropzone.files.filter(isImageFile);
    const idx = images.indexOf(mainImageFile);
    return idx >= 0 ? idx : 0;
  }

  // ── Timing toggle ─────────────────────────────────────────────────────────
  const scheduledFields = document.getElementById('scheduled-fields');
  document.querySelectorAll('input[name="isASAP"]').forEach(function (radio) {
    radio.addEventListener('change', function () {
      if (this.value === 'false') {
        scheduledFields.classList.remove('d-none');
      } else {
        scheduledFields.classList.add('d-none');
      }
    });
  });

  // ── Date picker (flatpickr) ───────────────────────────────────────────────
  let datePicker = null;
  const dateInput = document.getElementById('proposedDate');
  if (dateInput && typeof flatpickr !== 'undefined') {
    datePicker = flatpickr(dateInput, {
      dateFormat: 'Y-m-d',
      altInput:   true,
      altFormat:  'F j, Y',
      minDate:    'today',
      onChange:   function (selectedDates, dateStr, instance) {
        instance.altInput?.classList.remove('is-invalid');
      }
    });
  }

  // ── Time picker (jQuery Timepicker) ───────────────────────────────────────
  const timeInput = document.getElementById('proposedTime');
  if (timeInput && typeof $ !== 'undefined' && typeof $.fn.timepicker !== 'undefined') {
    $(timeInput).timepicker({ timeFormat: 'H:i', step: 15 });
    timeInput.addEventListener('change', function () {
      timeInput.classList.remove('is-invalid');
    });
  }

  // ── Inline validation clearing ────────────────────────────────────────────
  ['title', 'sourceAddress', 'destinationAddress'].forEach(function (id) {
    const el = document.getElementById(id);
    if (el) {
      el.addEventListener('input', function () {
        el.classList.remove('is-invalid');
      });
    }
  });

  const costInput = document.getElementById('proposedCost');
  if (costInput) {
    // Block characters that make no sense for a currency amount
    costInput.addEventListener('keydown', function (e) {
      if (e.key === '-' || e.key === '+' || e.key === 'e' || e.key === 'E') {
        e.preventDefault();
      }
    });
    // Clamp to max while typing; strip extra decimal places; clear error
    costInput.addEventListener('input', function () {
      costInput.classList.remove('is-invalid');
      const dotIndex = costInput.value.indexOf('.');
      if (dotIndex !== -1 && costInput.value.length - dotIndex > 3) {
        costInput.value = costInput.value.substring(0, dotIndex + 3);
      }
      const val = parseFloat(costInput.value);
      if (!isNaN(val) && val > 10000) {
        costInput.value = 10000;
      }
    });
    // Clamp to min when leaving the field
    costInput.addEventListener('blur', function () {
      const val = parseFloat(costInput.value);
      if (!isNaN(val) && val < 1) {
        costInput.value = 1;
      }
    });
  }

  document.querySelectorAll('input[name="service"]').forEach(function (radio) {
    radio.addEventListener('change', function () {
      document.getElementById('serviceError').classList.add('d-none');
    });
  });

  // ── Form submission ───────────────────────────────────────────────────────
  const form      = document.getElementById('requestCreateForm');
  const submitBtn = document.getElementById('btnPlaceRequest');
  const cancelBtn = document.getElementById('btnCancel');

  if (cancelBtn) {
    cancelBtn.addEventListener('click', function () {
      window.location.href = '/Dashboard';
    });
  }

  if (submitBtn && form) {
    submitBtn.addEventListener('click', async function () {

      // ── Clear previous validation errors ────────────────────────────────
      document.querySelectorAll('#requestCreateForm .is-invalid').forEach(function (el) {
        el.classList.remove('is-invalid');
      });
      document.getElementById('serviceError').classList.add('d-none');
      const mediaErrorEl = document.getElementById('mediaError');
      mediaErrorEl.textContent = '';
      mediaErrorEl.classList.add('d-none');

      // ── Collect files ───────────────────────────────────────────────────
      const imageFiles = mediaDropzone ? mediaDropzone.files.filter(isImageFile) : [];
      const videoFiles = mediaDropzone ? mediaDropzone.files.filter(f => !isImageFile(f)) : [];

      // ── Client-side validation ──────────────────────────────────────────
      let hasErrors = false;

      if (!document.querySelector('input[name="service"]:checked')) {
        document.getElementById('serviceError').classList.remove('d-none');
        hasErrors = true;
      }

      const titleInput = document.getElementById('title');
      if (!titleInput.value.trim()) {
        titleInput.classList.add('is-invalid');
        hasErrors = true;
      }

      const sourceInput = document.getElementById('sourceAddress');
      if (!sourceInput.value.trim()) {
        sourceInput.classList.add('is-invalid');
        hasErrors = true;
      }

      const destInput = document.getElementById('destinationAddress');
      if (!destInput.value.trim()) {
        destInput.classList.add('is-invalid');
        hasErrors = true;
      }

      const cost = parseFloat(costInput.value);
      if (!cost || cost < 1 || cost > 10000) {
        costInput.classList.add('is-invalid');
        hasErrors = true;
      }

      if (imageFiles.length === 0) {
        mediaErrorEl.textContent = 'At least one image is required.';
        mediaErrorEl.classList.remove('d-none');
        hasErrors = true;
      } else if (imageFiles.length > imageMaxCount) {
        mediaErrorEl.textContent = `Maximum ${imageMaxCount} images allowed.`;
        mediaErrorEl.classList.remove('d-none');
        hasErrors = true;
      } else {
        const imageMaxBytes = imageMaxSize * 1024 * 1024;
        for (const img of imageFiles) {
          if (img.size > imageMaxBytes) {
            mediaErrorEl.textContent = `Each image must be under ${imageMaxSize} MB.`;
            mediaErrorEl.classList.remove('d-none');
            hasErrors = true;
            break;
          }
        }
      }

      if (mediaErrorEl.classList.contains('d-none')) {
        if (videoFiles.length > videoMaxCount) {
          mediaErrorEl.textContent = `Maximum ${videoMaxCount} videos allowed.`;
          mediaErrorEl.classList.remove('d-none');
          hasErrors = true;
        } else {
          const videoMaxBytes = videoMaxSize * 1024 * 1024;
          for (const vid of videoFiles) {
            if (vid.size > videoMaxBytes) {
              mediaErrorEl.textContent = `Each video must be under ${videoMaxSize} MB.`;
              mediaErrorEl.classList.remove('d-none');
              hasErrors = true;
              break;
            }
          }
        }
      }

      const isASAP = document.querySelector('input[name="isASAP"]:checked')?.value === 'true';
      if (!isASAP) {
        if (!dateInput || !dateInput.value) {
          (datePicker?.altInput ?? dateInput)?.classList.add('is-invalid');
          hasErrors = true;
        }
        if (!timeInput || !timeInput.value) {
          timeInput.classList.add('is-invalid');
          hasErrors = true;
        }
      }

      if (hasErrors) {
        notyf.error('Please fill all required fields.');
        return;
      }

      // ── Build FormData ──────────────────────────────────────────────────
      const formData = new FormData(form);

      imageFiles.forEach(function (file) {
        formData.append('images', file, file.name);
      });
      formData.append('mainImageIndex', getMainImageIndex());

      videoFiles.forEach(function (file) {
        formData.append('videos', file, file.name);
      });

      // ── Submit ──────────────────────────────────────────────────────────
      submitBtn.disabled = true;

      try {
        const response = await fetch(form.action, {
          method:  'POST',
          body:    formData,
          headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        const result = await response.json();

        if (result.success) {
          window.location.href = result.redirect;
        } else {
          notyf.error(result.error ?? 'Something went wrong. Please try again.');
          submitBtn.disabled = false;
        }
      } catch {
        notyf.error('Something went wrong. Please try again.');
        submitBtn.disabled = false;
      }
    });
  }

})();
