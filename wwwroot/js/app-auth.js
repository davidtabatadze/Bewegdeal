/**
 * Pages Authentication
 * v1.0.3
 */
'use strict';

document.addEventListener('DOMContentLoaded', function () {
    (() => {
        const formAuthentication = document.querySelector('#formAuthentication');

        // Form validation for Add new record
        if (formAuthentication && typeof FormValidation !== 'undefined') {
            FormValidation.formValidation(formAuthentication, {
                fields: {
                    username: {
                        validators: {
                            notEmpty: {
                                message: 'Bitte Benutzernamen eingeben'
                            },
                            stringLength: {
                                min: 6,
                                message: 'Benutzername muss mehr als 6 Zeichen lang sein'
                            }
                        }
                    },
                    email: {
                        validators: {
                            notEmpty: {
                                message: 'Bitte E-Mail-Adresse eingeben'
                            },
                            emailAddress: {
                                message: 'Bitte eine gültige E-Mail-Adresse eingeben'
                            }
                        }
                    },
                    'email-username': {
                        validators: {
                            notEmpty: {
                                message: 'Bitte E-Mail / Benutzernamen eingeben'
                            },
                            stringLength: {
                                min: 6,
                                message: 'Benutzername muss mehr als 6 Zeichen lang sein'
                            }
                        }
                    },
                    password: {
                        validators: {
                            notEmpty: {
                                message: 'Bitte Passwort eingeben'
                            },
                            stringLength: {
                                min: 6,
                                message: 'Passwort muss mehr als 6 Zeichen lang sein'
                            }
                        }
                    },
                    'confirm-password': {
                        validators: {
                            notEmpty: {
                                message: 'Bitte Passwort bestätigen'
                            },
                            identical: {
                                compare: () => formAuthentication.querySelector('[name="password"]').value,
                                message: 'Das Passwort und die Bestätigung stimmen nicht überein'
                            },
                            stringLength: {
                                min: 6,
                                message: 'Passwort muss mehr als 6 Zeichen lang sein'
                            }
                        }
                    },
                    terms: {
                        validators: {
                            notEmpty: {
                                message: 'Bitte stimmen Sie den AGB zu'
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
                    submitButton: new FormValidation.plugins.SubmitButton(),
                    defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
                    autoFocus: new FormValidation.plugins.AutoFocus()
                },
                init: instance => {
                    instance.on('plugins.message.placed', e => {
                        if (e.element.parentElement.classList.contains('input-group')) {
                            e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                        }
                    });
                }
            });
        }

        // Two Steps Verification for numeral input mask
        const numeralMaskElements = document.querySelectorAll('.numeral-mask');

        // Format function for numeral mask
        const formatNumeral = value => value.replace(/\D/g, ''); // Only keep digits

        if (numeralMaskElements.length > 0) {
            numeralMaskElements.forEach(numeralMaskEl => {
                numeralMaskEl.addEventListener('input', event => {
                    numeralMaskEl.value = formatNumeral(event.target.value);
                });
            });
        }
    })();
});
