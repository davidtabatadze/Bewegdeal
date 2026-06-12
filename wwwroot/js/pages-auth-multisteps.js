/**
 *  Page auth register multi-steps
 */

'use strict';

// Select2 (jquery)
$(function () {
  var select2 = $('.select2');

  if (select2.length) {
    select2.each(function () {
      var $this = $(this);
      select2Focus($this);
      $this.select2({
        placeholder: 'Select an country',
        dropdownParent: $this.parent()
      });
    });
  }
});

// Multi Steps Validation
// --------------------------------------------------------------------
document.addEventListener('DOMContentLoaded', function (e) {
  (function () {
    const stepsValidation = document.querySelector('#multiStepsValidation');
    if (typeof stepsValidation !== undefined && stepsValidation !== null) {
      const stepsValidationForm = stepsValidation.querySelector('#multiStepsForm');

      const roleStep    = stepsValidationForm.querySelector('#roleSelectionValidation');
      const generalStep = stepsValidationForm.querySelector('#personalInfoValidation');
      const accountStep = stepsValidationForm.querySelector('#accountDetailsValidation');

      const stepsValidationNext = [].slice.call(stepsValidationForm.querySelectorAll('.btn-next'));
      const stepsValidationPrev = [].slice.call(stepsValidationForm.querySelectorAll('.btn-prev'));

      const roleIndicator    = document.querySelector('#roleIndicator');
      const multiStepsMobile = document.querySelector('.multi-steps-mobile');

      if (multiStepsMobile) {
        multiStepsMobile.addEventListener('input', event => {
          const cleanValue = event.target.value.replace(/\D/g, '');
          multiStepsMobile.value = formatGeneral(cleanValue, {
            blocks: [3, 3, 4],
            delimiters: [' ', ' ']
          });
        });
        registerCursorTracker({ input: multiStepsMobile, delimiter: ' ' });
      }

      let validationStepper = new Stepper(stepsValidation, { linear: true });

      function resetGeneralStep() {
        generalValidation.resetForm();
        ['mobile', 'number', 'address'].forEach(function (name) {
          const el = generalStep.querySelector('[name="' + name + '"]');
          if (!el) return;
          el.classList.remove('is-invalid');
          const fb = el.parentElement.querySelector('.invalid-feedback');
          if (fb) fb.remove();
        });
      }

      function validateGeneralManualFields() {
        const selected  = roleStep.querySelector('[name="role"]:checked');
        const isCompany = selected && selected.value === 'company';

        const toCheck = [
          { el: generalStep.querySelector('[name="mobile"]'),                msg: 'Please enter phone number' }
        ];
        if (isCompany) {
          toCheck.push({ el: generalStep.querySelector('[name="number"]'), msg: 'Please enter identification number' });
          toCheck.push({ el: generalStep.querySelector('[name="address"]'),              msg: 'Please enter your address' });
        }

        let passed = true;
        toCheck.forEach(function (item) {
          if (!item.el) return;
          if (!item.el.value.trim()) {
            item.el.classList.add('is-invalid');
            let fb = item.el.parentElement.querySelector('.invalid-feedback');
            if (!fb) {
              fb = document.createElement('div');
              fb.className = 'invalid-feedback';
              item.el.parentElement.appendChild(fb);
            }
            fb.textContent = item.msg;
            item.el.addEventListener('input', function () { item.el.classList.remove('is-invalid'); }, { once: true });
            passed = false;
          }
        });
        return passed;
      }

      // Step 1: Role
      const roleValidation = FormValidation.formValidation(roleStep, {
        fields: {
          role: {  // matches input name="role"
            validators: {
              notEmpty: {
                message: 'Please select an account type'
              }
            }
          }
        },
        plugins: {
          trigger: new FormValidation.plugins.Trigger(),
          bootstrap5: new FormValidation.plugins.Bootstrap5({
            eleValidClass: '',
            rowSelector: function (field, ele) {
              return '.form-control-validation';
            }
          }),
          autoFocus: new FormValidation.plugins.AutoFocus(),
          submitButton: new FormValidation.plugins.SubmitButton()
        }
      }).on('core.form.valid', function () {
        const selected = roleStep.querySelector('[name="role"]:checked');
        if (selected) {
          const isCompany = selected.value === 'company';
          if (roleIndicator) {
            const label = selected.value.charAt(0).toUpperCase() + selected.value.slice(1);
            roleIndicator.innerHTML = `<i class="icon-base ri ${isCompany ? 'ri-building-line' : 'ri-user-line'} me-1"></i>${label}`;
            roleIndicator.className = 'badge bg-label-primary';
          }
          const numberCol  = document.getElementById('numberCol');
          const addressCol = document.getElementById('addressCol');
          if (numberCol)  numberCol.classList.toggle('d-none', !isCompany);
          if (addressCol) addressCol.classList.toggle('d-none', !isCompany);
        }
        validationStepper.next();
      });

      // Step 2: General
      const generalValidation = FormValidation.formValidation(generalStep, {
        fields: {
          name: {
            validators: {
              notEmpty: {
                message: 'Please enter your name'
              }
            }
          }
        },
        plugins: {
          trigger: new FormValidation.plugins.Trigger(),
          bootstrap5: new FormValidation.plugins.Bootstrap5({
            eleValidClass: '',
            rowSelector: function (field, ele) {
              switch (field) {
                case 'name':
                  return '.form-control-validation';
                default:
                  return '.row';
              }
            }
          }),
          autoFocus: new FormValidation.plugins.AutoFocus(),
          submitButton: new FormValidation.plugins.SubmitButton()
        }
      }).on('core.form.valid', function () {
        if (!validateGeneralManualFields()) return;

        const isCompany = roleStep.querySelector('[name="role"]:checked') &&
                          roleStep.querySelector('[name="role"]:checked').value === 'company';
        const servicesSection = document.querySelector('#servicesSection');
        if (servicesSection) servicesSection.classList.toggle('d-none', !isCompany);
        const companyTermsUpload = document.querySelector('#companyTermsUpload');
        if (companyTermsUpload) companyTermsUpload.classList.toggle('d-none', !isCompany);

        validationStepper.next();
      });

      // Step 3: Account
      const accountValidation = FormValidation.formValidation(accountStep, {
        fields: {
          email: {
            validators: {
              notEmpty: {
                message: 'Please enter email address'
              },
              emailAddress: {
                message: 'The value is not a valid email address'
              }
            }
          },
          password: {
            validators: {
              notEmpty: {
                message: 'Please enter password'
              },
              stringLength: {
                min: 6,
                max: 16,
                message: 'Password must be between 6 and 16 characters'
              }
            }
          },
          confirmPassword: {
            validators: {
              notEmpty: {
                message: 'Confirm Password is required'
              },
              identical: {
                compare: function () {
                  return accountStep.querySelector('[name="password"]').value;
                },
                message: 'The password and its confirm are not the same'
              }
            }
          }
        },
        plugins: {
          trigger: new FormValidation.plugins.Trigger(),
          bootstrap5: new FormValidation.plugins.Bootstrap5({
            eleValidClass: '',
            rowSelector: '.form-control-validation'
          }),
          autoFocus: new FormValidation.plugins.AutoFocus(),
          submitButton: new FormValidation.plugins.SubmitButton()
        },
        init: instance => {
          instance.on('plugins.message.placed', function (e) {
            if (e.element.parentElement.classList.contains('input-group')) {
              e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
            }
          });
        }
      }).on('core.form.valid', function () {
        const agreeTerms = accountStep.querySelector('[name="agreeTerms"]');
        if (agreeTerms && !agreeTerms.checked) return;

        const servicesSection = document.querySelector('#servicesSection');
        if (servicesSection && !servicesSection.classList.contains('d-none')) {
          const anyService = accountStep.querySelectorAll('[name="Interests"]:checked').length > 0;
          if (!anyService) return;
        }

        stepsValidationForm.submit();
      });

      stepsValidationNext.forEach(item => {
        item.addEventListener('click', event => {
          switch (validationStepper._currentIndex) {
            case 0: roleValidation.validate();    break;
            case 1: validateGeneralManualFields(); generalValidation.validate(); break;
            case 2:
              const agreeTerms = accountStep.querySelector('[name="agreeTerms"]');
              if (agreeTerms && !agreeTerms.checked) {
                agreeTerms.classList.add('is-invalid');
                let fb = agreeTerms.parentElement.querySelector('.invalid-feedback');
                if (!fb) {
                  fb = document.createElement('div');
                  fb.className = 'invalid-feedback';
                  agreeTerms.parentElement.appendChild(fb);
                }
                fb.textContent = 'You must agree to the Terms & Conditions';
                agreeTerms.addEventListener('change', function () { agreeTerms.classList.remove('is-invalid'); }, { once: true });
              }

              const servicesSection = document.querySelector('#servicesSection');
              if (servicesSection && !servicesSection.classList.contains('d-none')) {
                const anyService = accountStep.querySelectorAll('[name="Interests"]:checked').length > 0;
                if (!anyService) {
                  let svcFb = servicesSection.querySelector('.services-invalid-feedback');
                  if (!svcFb) {
                    svcFb = document.createElement('div');
                    svcFb.className = 'services-invalid-feedback text-danger small mt-2';
                    servicesSection.appendChild(svcFb);
                  }
                  svcFb.textContent = 'Please select at least one service';
                  servicesSection.querySelectorAll('[name="Interests"]').forEach(function (cb) {
                    cb.addEventListener('change', function () {
                      if (servicesSection.querySelectorAll('[name="Interests"]:checked').length > 0) {
                        if (svcFb) svcFb.textContent = '';
                      }
                    });
                  });
                } else {
                  const svcFb = servicesSection.querySelector('.services-invalid-feedback');
                  if (svcFb) svcFb.textContent = '';
                }
              }

              accountValidation.validate();
              break;
            default: break;
          }
        });
      });

      stepsValidationPrev.forEach(item => {
        item.addEventListener('click', event => {
          switch (validationStepper._currentIndex) {
            case 1:
              resetGeneralStep();
              validationStepper.previous();
              break;
            case 2:
              resetGeneralStep();
              accountValidation.resetForm();
              const agreeTermsReset = accountStep.querySelector('[name="agreeTerms"]');
              if (agreeTermsReset) {
                agreeTermsReset.classList.remove('is-invalid');
                const fb = agreeTermsReset.parentElement.querySelector('.invalid-feedback');
                if (fb) fb.remove();
              }
              const svcFbReset = document.querySelector('#servicesSection .services-invalid-feedback');
              if (svcFbReset) svcFbReset.textContent = '';
              validationStepper.previous();
              break;
            default: break;
          }
        });
      });
    }
  })();
});
