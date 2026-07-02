'use strict';

(function () {
  const cardColor = config.colors.cardColor;
  const labelColor = config.colors.textMuted;
  const borderColor = config.colors.borderColor;
  const fontFamily = config.fontFamily;

  const colorByKey = {
    moving:    config.colors.success,
    pickup:    config.colors.warning,
    removal:   config.colors.danger,
    transport: config.colors.info,
    fees:      config.colors.secondary
  };

  let profitChart = null;

  function getColumnWidth() {
    const w = window.innerWidth;
    if (w >= 576) { return '20%'; }
    return '80%';
  }

  function initInvoiceChart(data) {
    const nameToColor = Object.fromEntries(
      Object.keys(colorByKey).map(k => [data.groupNames[k], colorByKey[k]])
    );
    const el = document.querySelector('#totalProfitChart');
    if (!el) { return; }

    if (profitChart) { profitChart.destroy(); }

    profitChart = new ApexCharts(el, {
      chart: {
        type: 'bar',
        height: 260,
        parentHeightOffset: 0,
        stacked: true,
        toolbar: { show: false },
        zoom: { enabled: false }
      },
      series: data.series,
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: getColumnWidth(),
          borderRadius: 6,
          borderRadiusApplication: 'around',
          startingShape: 'rounded',
          endingShape: 'rounded'
        }
      },
      dataLabels: { enabled: false },
      stroke: {
        curve: 'smooth',
        width: 2,
        lineCap: 'round',
        colors: [cardColor]
      },
      legend: {
        show: true,
        position: 'bottom',
        fontSize: '13px',
        fontFamily: fontFamily,
        labels: { colors: labelColor }
      },
      colors: data.series.map(s => nameToColor[s.name]),
      grid: {
        xaxis: { lines: { show: false } },
        strokeDashArray: 8,
        borderColor: borderColor,
        padding: { top: -10, left: 15, right: -15, bottom: -10 }
      },
      xaxis: {
        axisTicks: { show: false },
        crosshairs: { opacity: 0 },
        axisBorder: { show: false },
        categories: data.xaxisValues,
        tickPlacement: 'on',
        labels: {
          style: { fontSize: '13px', fontFamily: fontFamily, colors: labelColor }
        }
      },
      yaxis: {
        labels: {
          formatter: val => '€' + parseInt(val),
          style: { fontSize: '13px', fontFamily: fontFamily, colors: labelColor }
        }
      },
      states: {
        hover: { filter: { type: 'none' } },
        active: { filter: { type: 'none' } }
      },
      responsive: [
        {
          breakpoint: 450,
          options: {
            chart: { height: 200 },
            xaxis: { labels: { rotate: 315, rotateAlways: true } }
          }
        }
      ]
    });

    profitChart.render();
  }

  function buildYearDropdown(years, selectedYear) {
    const menu = document.querySelector('#yearDropdownMenu');
    const label = document.querySelector('#selectedYear');
    if (!menu || !label) { return; }

    label.textContent = selectedYear;
    menu.innerHTML = '';

    years.forEach(function (y) {
      const li = document.createElement('li');
      const a = document.createElement('a');
      a.className = 'dropdown-item year-filter-item' + (y === selectedYear ? ' active' : '');
      a.href = 'javascript:void(0);';
      a.dataset.year = y;
      a.textContent = y;
      li.appendChild(a);
      menu.appendChild(li);
    });
  }

  function loadStats(year) {
    $.get('/Dashboard/CompanyStats', { year: year }, function (res) {
      if (!res || !res.result) { return; }
      const data = res.result;
      buildYearDropdown(data.years, data.year);
      initInvoiceChart(data.invoiceChart);
    });
  }

  let resizeTimer = null;
  window.addEventListener('resize', function () {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(function () {
      if (profitChart) {
        profitChart.updateOptions({
          plotOptions: { bar: { columnWidth: getColumnWidth() } }
        });
      }
    }, 150);
  });

  loadStats(0);

  $(document).on('click', '.year-filter-item', function () {
    const year = parseInt($(this).data('year'));
    loadStats(year);
  });
})();
