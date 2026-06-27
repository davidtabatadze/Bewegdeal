/**
 * Page auth two steps
 * v1.0.3
 */
'use strict';

document.addEventListener('DOMContentLoaded', function () {
  (() => {
    const twoStepsForm = document.querySelector('#twoStepsForm');
    if (!twoStepsForm) { return; }

    // Initialise a single OTP digit-group: wires up focus movement and assembles
    // the collected value into the hidden input identified by hiddenName.
    function initOtpWrapper(wrapperId, hiddenName) {
      const wrapper = document.getElementById(wrapperId);
      if (!wrapper) { return; }

      const hidden = twoStepsForm.querySelector('[name="' + hiddenName + '"]');
      const pins   = Array.from(wrapper.children);

      function assembleOtp() {
        const complete = pins.every(p => p.value !== '');
        hidden.value = complete ? pins.map(p => p.value).join('') : '';
      }

      pins.forEach(function (pin) {
        pin.addEventListener('keyup', function (e) {
          if (/^\d$/.test(e.key)) {
            if (pin.nextElementSibling && pin.value.length === parseInt(pin.getAttribute('maxlength'))) {
              pin.nextElementSibling.focus();
            }
          } else if (e.key === 'Backspace') {
            if (pin.previousElementSibling) {
              pin.previousElementSibling.focus();
            }
          }
          assembleOtp();
        });

        pin.addEventListener('keypress', function (e) {
          if (e.key === '-') { e.preventDefault(); }
        });
      });
    }

    initOtpWrapper('emailOtpWrapper', 'emailOtp');
    initOtpWrapper('mobileOtpWrapper', 'mobileOtp');

    if (typeof FormValidation !== 'undefined') {
      FormValidation.formValidation(twoStepsForm, {
        fields: {
          emailOtp:  { validators: { notEmpty: { message: '' } } },
          mobileOtp: { validators: { notEmpty: { message: '' } } }
        },
        plugins: {
          trigger:       new FormValidation.plugins.Trigger(),
          bootstrap5:    new FormValidation.plugins.Bootstrap5({
            eleValidClass: '',
            rowSelector: '.form-control-validation'
          }),
          submitButton:  new FormValidation.plugins.SubmitButton(),
          defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
          autoFocus:     new FormValidation.plugins.AutoFocus()
        }
      });
    }
  })();
});
