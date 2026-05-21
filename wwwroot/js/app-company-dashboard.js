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

  // ── Star rating SVGs ──────────────────────────────────────────────────────

  function buildStarSvgs() {
    var gray = '#cccccc';
    try { gray = window.Helpers.getCssVar('gray-200', true) || gray; } catch (_) {}
    var ri = parseInt(gray.slice(1, 3), 16) || 204;
    var gi = parseInt(gray.slice(3, 5), 16) || 204;
    var bi = parseInt(gray.slice(5, 7), 16) || 204;
    var grayEncoded = gray.replace('#', '%23');
    var gradient =
      "%3Cstop offset='50%25' style='stop-color:%23FFD700' /%3E" +
      "%3Cstop offset='50%25' style='stop-color:" + grayEncoded + "' /%3E";
    var path =
      "M21.947 9.179a1 1 0 0 0-.868-.676l-5.701-.453l-2.467-5.461a.998.998 0 0 0-1.822-.001" +
      "L8.622 8.05l-5.701.453a1 1 0 0 0-.619 1.713l4.213 4.107l-1.49 6.452a1 1 0 0 0 1.53 1.057" +
      "L12 18.202l5.445 3.63a1.001 1.001 0 0 0 1.517-1.106l-1.829-6.4l4.536-4.082c.297-.268.406-.686.278-1.065";
    return {
      full:  "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='16'%3E%3Cpath fill='%23FFD700' d='" + path + "'/%3E%3C/svg%3E",
      half:  "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'%3E%3Cdefs%3E%3ClinearGradient id='hsg'%3E" + gradient + "%3C/linearGradient%3E%3C/defs%3E%3Cpath fill='url(%23hsg)' d='" + path + "'/%3E%3C/svg%3E",
      empty: "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' width='16'%3E%3Cpath fill='rgb(" + ri + "," + gi + "," + bi + ")' d='" + path + "'/%3E%3C/svg%3E"
    };
  }

  var ratingInstance = null;

  function initRating(score) {
    var el = document.querySelector('#companyRating');
    if (!el) { return; }
    el.innerHTML = '';
    var svgs = buildStarSvgs();
    ratingInstance = new Raty(el, {
      score: score || 0, half: true, readOnly: true,
      starOn: svgs.full, starHalf: svgs.half, starOff: svgs.empty
    });
    ratingInstance.init();
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
