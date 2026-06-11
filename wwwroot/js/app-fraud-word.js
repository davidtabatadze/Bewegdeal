'use strict';

(function () {

    Swal.fire({
        title: 'Fraud Words',
        icon: 'warning',
        html:
            '<ul class="text-start ps-4 mb-0">' +
                '<li>These words are used to monitor and flag chat messages.</li>' +
                '<li>Be careful what you add or remove.</li>' +
                '<li>Changes may impact the chat flow.</li>' +
            '</ul>',
        confirmButtonText: 'I understand',
        customClass: { confirmButton: 'btn btn-danger' },
        buttonsStyling: false
    });

    var input = document.getElementById('fraudWordsInput');
    if (!input) { return; }

    var tagify = new Tagify(input);

    tagify.on('add', function (e) {
        var word = (e.detail.data.value || '').trim().toLowerCase();
        if (!word) { return; }
        fetch('/FraudWord/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'word=' + encodeURIComponent(word)
        });
    });

    tagify.on('remove', function (e) {
        var word = (e.detail.data.value || '').trim().toLowerCase();
        if (!word) { return; }
        fetch('/FraudWord/Delete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'word=' + encodeURIComponent(word)
        });
    });

})();
