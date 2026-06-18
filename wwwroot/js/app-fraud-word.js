// v1.0.0
'use strict';

(function () {

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
