'use strict';

document.addEventListener('DOMContentLoaded', function () {

  // ── Service config ────────────────────────────────────────────────────────

  var services = [
    { key: 'moving',    label: 'Moving Service',    icon: 'ri-truck-line',      color: 'primary' },
    { key: 'removal',   label: 'Junk Removal',      icon: 'ri-delete-bin-line', color: 'danger'  },
    { key: 'pickup',    label: 'Store Pickup',      icon: 'ri-map-pin-line',    color: 'warning' },
    { key: 'transport', label: 'Vehicle Transport', icon: 'ri-car-line',        color: 'info'    }
  ];

  // ── Month inputs — set defaults to current month ──────────────────────────

  var now = new Date();
  var curMonth = now.getFullYear() + '-' + String(now.getMonth() + 1).padStart(2, '0');

  document.querySelector('#monthFrom').value = curMonth;
  document.querySelector('#monthTo').value   = curMonth;

  function getDateRange() {
    var from = document.querySelector('#monthFrom').value; // "2026-05"
    var to   = document.querySelector('#monthTo').value;
    if (!from || !to) { return null; }
    var tp   = to.split('-');
    var last = new Date(parseInt(tp[0]), parseInt(tp[1]), 0).getDate();
    return {
      from: from + '-01',
      to:   to   + '-' + String(last).padStart(2, '0')
    };
  }

  // ── Star rating ───────────────────────────────────────────────────────────

  var ratingInstance = null;

  function initRating(score) {
    var el = document.querySelector('#companyRating');
    if (!el) { return; }
    el.innerHTML = '';
    try {
      ratingInstance = new Raty(el, {
        starType: 'i',
        starOn:   'icon-base ri ri-star-fill text-warning',
        starHalf: 'icon-base ri ri-star-half-line text-warning',
        starOff:  'icon-base ri ri-star-line text-muted',
        score: score || 0, half: true, readOnly: true
      });
      ratingInstance.init();
    } catch (_) {}
  }

  // ── Widget rendering ──────────────────────────────────────────────────────

  function formatCount(val) {
    return Number(val || 0).toLocaleString('de-AT');
  }

  function formatCurrency(val) {
    return '€ ' + Number(val || 0).toLocaleString('de-AT', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  function serviceRow(svc, labelText, valueText, pct, isLast) {
    var mb = isLast ? '' : ' mb-1';
    return (
      '<li class="d-flex align-items-center' + mb + '">' +
        '<div class="avatar avatar-sm flex-shrink-0 me-2">' +
          '<div class="avatar-initial bg-label-' + svc.color + ' rounded">' +
            '<div class="icon-base ri ' + svc.icon + ' icon-16px"></div>' +
          '</div>' +
        '</div>' +
        '<span class="flex-grow-1 text-truncate" style="font-size:0.75rem">' + labelText + '</span>' +
        '<span class="fw-semibold me-2" style="font-size:0.75rem">' + valueText + '</span>' +
        '<div class="progress bg-label-' + svc.color + ' mb-0" style="height:3px;width:50px;">' +
          '<div class="progress-bar bg-' + svc.color + '" style="width:' + pct + '%" role="progressbar"></div>' +
        '</div>' +
      '</li>'
    );
  }

  function buildServiceList(containerId, data, total, formatter) {
    var el = document.querySelector('#' + containerId);
    if (!el) { return; }
    el.innerHTML = '';
    services.forEach(function (svc, i) {
      var val = data[svc.key] != null ? data[svc.key] : 0;
      var pct = total > 0 ? Math.round((val / total) * 100) : 0;
      el.insertAdjacentHTML('beforeend', serviceRow(svc, svc.label, formatter(val), pct, i === services.length - 1));
    });
  }

  function buildRatingBreakdown(data) {
    var el = document.querySelector('#ratingByService');
    if (!el) { return; }
    el.innerHTML = '';
    services.forEach(function (svc, i) {
      var done      = data.completed[svc.key] != null ? data.completed[svc.key] : 0;
      var cancelled = data.rejected[svc.key]  != null ? data.rejected[svc.key]  : 0;
      var base = done + cancelled;
      var pct  = base > 0 ? Math.round((done / base) * 100) : 0;
      el.insertAdjacentHTML('beforeend', serviceRow(svc, svc.label, pct + '%', pct, i === services.length - 1));
    });
  }

  function updateWidgets(data) {
    var score = data.rating || 0;
    document.querySelector('#ratingValue').textContent = Number(score).toFixed(1);
    document.querySelector('#ratingCount').textContent = formatCount(data.ratingCount);
    initRating(score);
    buildRatingBreakdown(data);

    document.querySelector('#completedTotal').textContent = formatCount(data.completed.total);
    buildServiceList('completedByService', data.completed, data.completed.total, formatCount);

    document.querySelector('#rejectedTotal').textContent = formatCount(data.rejected.total);
    buildServiceList('rejectedByService', data.rejected, data.rejected.total, formatCount);

    document.querySelector('#revenueTotal').textContent = formatCurrency(data.revenue.total);
    buildServiceList('revenueByService', data.revenue, data.revenue.total, formatCurrency);

    document.querySelector('#paidTotal').textContent = formatCount(data.paidInvoices.total);
    buildServiceList('paidByService', data.paidInvoices, data.paidInvoices.total, formatCount);

    document.querySelector('#pendingTotal').textContent = formatCount(data.pendingInvoices.total);
    buildServiceList('pendingByService', data.pendingInvoices, data.pendingInvoices.total, formatCount);
  }

  // ── Data fetch ────────────────────────────────────────────────────────────

  function loadStats() {
    var range = getDateRange();
    if (!range) { return; }

    var stats = document.querySelector('#dashboardStats');
    if (stats) { stats.style.opacity = '0.4'; }

    fetch('/Dashboard/CompanyStats?from=' + range.from + '&to=' + range.to)
      .then(function (res) { return res.json(); })
      .then(function (data) { if (!data.error) { updateWidgets(data); } })
      .catch(function () {})
      .finally(function () {
        if (stats) { stats.style.opacity = '1'; }
      });
  }

  // ── Controls ──────────────────────────────────────────────────────────────

  document.querySelector('#monthFrom').addEventListener('change', loadStats);
  document.querySelector('#monthTo').addEventListener('change', loadStats);

  document.querySelector('#btnResetFilter').addEventListener('click', function () {
    document.querySelector('#monthFrom').value = curMonth;
    document.querySelector('#monthTo').value   = curMonth;
    loadStats();
  });

  // ── Init ──────────────────────────────────────────────────────────────────

  initRating(0);
  loadStats();

});
