/**
 * Request Create / Edit
 *
 * Works for both the Create and Edit views.
 * The Edit view defines `existingFiles` in an inline <script> block before this file loads.
 * The Create view does not define it, so it defaults to [].
 */

'use strict';

Dropzone.autoDiscover = false;

(function () {

  // imageMaxCount, imageMaxSize, videoMaxCount, videoMaxSize — always defined by the inline <script>.
  // existingFiles — defined only by the Edit view; defaults to [] on Create.
  const _existingFiles = (typeof existingFiles !== 'undefined') ? existingFiles : [];

  // ── Notyf ─────────────────────────────────────────────────────────────────
  const notyf = new Notyf({ duration: 10000, position: { x: 'center', y: 'top' } });

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
  let loadingExisting = _existingFiles.length > 0;

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
      dictFileTooBig:   'File is too large.',
      dictRemoveFile:   '<i class="icon-base ri ri-delete-bin-line text-danger me-1 icon-14px"></i>Remove file'
    });

    mediaDropzone.on('addedfile', function (file) {
      document.getElementById('mediaError')?.classList.add('d-none');

      // Simulate instant upload completion for newly added files so the
      // progress bar fills and the success mark appears (we never actually
      // POST through Dropzone — files are collected on form submit).
      // Deferred with setTimeout so Dropzone's own addedfile → enqueueFile
      // logic runs first (it requires file.status === ADDED at that point).
      if (!file._existing) {
        setTimeout(function () {
          mediaDropzone.emit('uploadprogress', file, 100, file.size);
          file.status = Dropzone.SUCCESS;
          mediaDropzone.emit('success', file);
          mediaDropzone.emit('complete', file);
        }, 0);
      }

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

      // Don't auto-set main while loading existing files — restored explicitly after
      if (!loadingExisting) {
        if (!mainImageFile) {
          setMainFile(file);
        } else {
          updateMainBadges();
        }
      }
    });

    mediaDropzone.on('removedfile', function (file) {
      if (mainImageFile === file) {
        const nextImage = mediaDropzone.files.find(isImageFile);
        mainImageFile = nextImage ?? null;
      }
      updateMainBadges();
    });

    // ── Load existing files as mock entries (Edit only) ─────────────────────
    _existingFiles.forEach(function (ef) {
      const mockFile = {
        name:      ef.fileName,
        size:      ef.size,
        type:      ef.type === 'image' ? 'image/jpeg' : 'video/mp4',
        status:    Dropzone.ADDED,
        accepted:  true,
        _existing: true,
        _fileId:   ef.fileId,
        _isMain:   ef.isMain
      };
      mediaDropzone.files.push(mockFile);
      mediaDropzone.emit('addedfile', mockFile);
      if (ef.type === 'image') {
        mediaDropzone.emit('thumbnail', mockFile, ef.url);
      }
      mockFile.status = Dropzone.SUCCESS;
      mediaDropzone.emit('success', mockFile);
      mediaDropzone.emit('complete', mockFile);
    });

    if (_existingFiles.length > 0) {
      // Restore the main image that was saved on the server
      const serverMain = mediaDropzone.files.find(f => f._existing && f._isMain && isImageFile(f));
      if (serverMain) {
        setMainFile(serverMain);
      } else {
        const firstImage = mediaDropzone.files.find(isImageFile);
        if (firstImage) { setMainFile(firstImage); }
      }
      loadingExisting = false;
    }
  }

  function setMainFile(file) {
    mainImageFile = file;
    updateMainBadges();
  }

  function updateMainBadges() {
    if (!mediaDropzone) { return; }
    mediaDropzone.files.filter(isImageFile).forEach(function (f) {
      const badge = f.previewElement?.querySelector('.dz-main-badge');
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

  function getMainImageIndex(newImageFiles) {
    if (!mainImageFile || mainImageFile._existing) { return 0; }
    const idx = newImageFiles.indexOf(mainImageFile);
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
      dateFormat: 'F j, Y',
      minDate:    'today',
      onChange:   function () {
        dateInput.classList.remove('is-invalid');
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
  ['title', 'pickupAddress', 'deliveryAddress'].forEach(function (id) {
    const el = document.getElementById(id);
    if (el) {
      el.addEventListener('input', function () {
        el.classList.remove('is-invalid');
      });
    }
  });

  const costInput = document.getElementById('proposedCost');
  if (costInput) {
    costInput.addEventListener('keydown', function (e) {
      if (e.key === '-' || e.key === '+' || e.key === 'e' || e.key === 'E') {
        e.preventDefault();
      }
    });
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
    costInput.addEventListener('blur', function () {
      const val = parseFloat(costInput.value);
      if (!isNaN(val) && val < 1) {
        costInput.value = 1;
      }
    });
  }

  // ── Bootstrap-select (selectpicker) init ──────────────────────────────────
  if (typeof $ !== 'undefined' && typeof $.fn.selectpicker !== 'undefined') {
    $('#vehicleType, #vehicleCondition').selectpicker();
    if (typeof handleBootstrapSelectEvents === 'function') {
      handleBootstrapSelectEvents();
    }
  }

  // ── Additional Details visibility ─────────────────────────────────────────
  const noDetailsMsg        = document.getElementById('noDetailsMsg');
  const transportFields     = document.getElementById('transportFields');
  const elevatorParkingFields = document.getElementById('elevatorParkingFields');

  function updateAdditionalDetails(service) {
    if (!service) {
      noDetailsMsg.classList.remove('d-none');
      transportFields.classList.add('d-none');
      elevatorParkingFields.classList.add('d-none');
    } else if (service === 'transport') {
      noDetailsMsg.classList.add('d-none');
      transportFields.classList.remove('d-none');
      elevatorParkingFields.classList.add('d-none');
    } else {
      noDetailsMsg.classList.add('d-none');
      transportFields.classList.add('d-none');
      elevatorParkingFields.classList.remove('d-none');
    }
  }

  const destAddressWrapper = document.getElementById('deliveryAddress')?.closest('.col-12');

  function toggleDestinationAddress(service) {
    if (!destAddressWrapper) { return; }
    const destInput = document.getElementById('deliveryAddress');
    if (service === 'removal') {
      destAddressWrapper.classList.add('d-none');
      if (destInput) {
        destInput.value = '';
        destInput.classList.remove('is-invalid');
      }
    } else {
      destAddressWrapper.classList.remove('d-none');
    }
  }

  document.querySelectorAll('input[name="service"]').forEach(function (radio) {
    radio.addEventListener('change', function () {
      document.getElementById('serviceError').classList.add('d-none');
      toggleDestinationAddress(this.value);
      updateAdditionalDetails(this.value);
      document.getElementById('vehicleTypeError').classList.add('d-none');
      document.getElementById('vehicleConditionError').classList.add('d-none');
    });
  });

  // Apply on page load (Edit mode may have a service pre-selected)
  const initialService = document.querySelector('input[name="service"]:checked')?.value ?? null;
  if (initialService) { toggleDestinationAddress(initialService); }
  updateAdditionalDetails(initialService);

  // ── Form submission ───────────────────────────────────────────────────────
  const form      = document.getElementById('requestForm');
  const submitBtn = document.getElementById('btnSubmitRequest');
  const cancelBtn = document.getElementById('btnCancel');

  if (cancelBtn) {
    cancelBtn.addEventListener('click', function () {
      window.location.href = '/Dashboard';
    });
  }

  if (submitBtn && form) {
    submitBtn.addEventListener('click', async function () {

      // ── Clear previous validation errors ────────────────────────────────
      document.querySelectorAll('#requestForm .is-invalid').forEach(function (el) {
        el.classList.remove('is-invalid');
      });
      document.getElementById('serviceError').classList.add('d-none');
      document.getElementById('vehicleTypeError').classList.add('d-none');
      document.getElementById('vehicleConditionError').classList.add('d-none');
      const mediaErrorEl = document.getElementById('mediaError');
      mediaErrorEl.textContent = '';
      mediaErrorEl.classList.add('d-none');

      // ── Collect files ───────────────────────────────────────────────────
      const keptImages  = mediaDropzone ? mediaDropzone.files.filter(f =>  f._existing && isImageFile(f))  : [];
      const keptVideos  = mediaDropzone ? mediaDropzone.files.filter(f =>  f._existing && !isImageFile(f)) : [];
      const imageFiles  = mediaDropzone ? mediaDropzone.files.filter(f => !f._existing && isImageFile(f))  : [];
      const videoFiles  = mediaDropzone ? mediaDropzone.files.filter(f => !f._existing && !isImageFile(f)) : [];
      const totalImages = keptImages.length + imageFiles.length;
      const totalVideos = keptVideos.length + videoFiles.length;

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

      const sourceInput = document.getElementById('pickupAddress');
      if (!sourceInput.value.trim()) {
        sourceInput.classList.add('is-invalid');
        hasErrors = true;
      }

      const destInput = document.getElementById('deliveryAddress');
      const selectedService = document.querySelector('input[name="service"]:checked')?.value;
      if (selectedService !== 'removal' && !destInput.value.trim()) {
        destInput.classList.add('is-invalid');
        hasErrors = true;
      }

      const cost = parseFloat(costInput.value);
      if (!cost || cost < 1 || cost > 10000) {
        costInput.classList.add('is-invalid');
        hasErrors = true;
      }

      if (totalImages === 0) {
        mediaErrorEl.textContent = 'At least one image is required.';
        mediaErrorEl.classList.remove('d-none');
        hasErrors = true;
      } else if (totalImages > imageMaxCount) {
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
        if (totalVideos > videoMaxCount) {
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

      if (selectedService === 'transport') {
        const vType = document.getElementById('vehicleType').value;
        if (!vType) {
          document.getElementById('vehicleTypeError').classList.remove('d-none');
          hasErrors = true;
        }
        const vCond = document.getElementById('vehicleCondition').value;
        if (!vCond) {
          document.getElementById('vehicleConditionError').classList.remove('d-none');
          hasErrors = true;
        }
      }

      const isASAP = document.querySelector('input[name="isASAP"]:checked')?.value === 'true';
      if (!isASAP) {
        if (!dateInput || !dateInput.value) {
          dateInput?.classList.add('is-invalid');
          hasErrors = true;
        }
        if (!timeInput || !timeInput.value) {
          timeInput.classList.add('is-invalid');
          hasErrors = true;
        }
      }

      if (hasErrors) {
        notyf.error('Please, fill all required fields.');
        return;
      }

      // ── Build FormData ──────────────────────────────────────────────────
      const formData = new FormData(form);

      // Existing files to keep (Edit only; empty on Create — harmless)
      keptImages.concat(keptVideos).forEach(function (f) {
        formData.append('keepFileIds', f._fileId);
      });

      // Main image: existing file or new upload
      const mainIsExisting = mainImageFile?._existing ?? false;
      formData.append('keepMainFileId', mainIsExisting ? mainImageFile._fileId : 0);

      // New files
      imageFiles.forEach(function (file) {
        formData.append('images', file, file.name);
      });
      formData.append('mainImageIndex', getMainImageIndex(imageFiles));

      videoFiles.forEach(function (file) {
        formData.append('videos', file, file.name);
      });

      // ── Submit ──────────────────────────────────────────────────────────
      submitBtn.disabled = true;

      // Page blocking — Multiple Message style
      const loadingMessages = ['Please wait...', 'Uploading files...', 'Almost done...'];
      let loadingMsgIndex   = 0;

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

      setLoadingMessage(loadingMessages[0]);
      const loadingInterval = setInterval(function () {
        loadingMsgIndex = (loadingMsgIndex + 1) % loadingMessages.length;
        setLoadingMessage(loadingMessages[loadingMsgIndex]);
      }, 1000);

      try {
        const response = await fetch(form.action, {
          method:  'POST',
          body:    formData,
          headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        const result = await response.json();

        clearInterval(loadingInterval);

        if (result.success) {
          Loading.standard({
            backgroundColor: 'rgba(' + window.Helpers.getCssVar('black-rgb') + ', 0.5)',
            svgSize: '0px'
          });
          const successEl = document.querySelector('.notiflix-loading');
          if (successEl) {
            successEl.innerHTML = `<div class="px-12 py-3 bg-success text-white">Success</div>`;
          }
          setTimeout(function () {
            window.location.href = result.redirect;
          }, 2000);
        } else {
          Loading.remove();
          notyf.error(result.error ?? 'Something went wrong. Please try again.');
          submitBtn.disabled = false;
        }
      } catch {
        clearInterval(loadingInterval);
        Loading.remove();
        notyf.error('Something went wrong. Please try again.');
        submitBtn.disabled = false;
      }
    });
  }

})();
