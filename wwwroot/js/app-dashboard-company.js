/**
 * Company Dashboard — Bewegdeal
 * v1.1.0
 */

'use strict';

(function () {
    const cardColor = config.colors.cardColor;
    const labelColor = config.colors.textMuted;
    const borderColor = config.colors.borderColor;
    const fontFamily = config.fontFamily;

    const colorByKey = {
        moving: config.colors.success,
        pickup: config.colors.warning,
        removal: config.colors.danger,
        transport: config.colors.info,
        fees: config.colors.secondary
    };

    let profitChart = null;
    let serviceChart = null;

    function getColumnWidth() {
        const w = window.innerWidth;
        if (w >= 576) { return '20%'; }
        return '80%';
    }

    function initInvoiceChart(data) {
        const nameToColor = Object.fromEntries(
            Object.keys(colorByKey).map(k => [data.groupNames[k], colorByKey[k]])
        );
        const el = document.querySelector('#incomesChart');
        if (!el) { return; }

        if (profitChart) { profitChart.destroy(); }

        profitChart = new ApexCharts(el, {
            chart: {
                type: 'bar',
                height: 300,
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

    function initServiceChart(data) {
        const nameToColor = Object.fromEntries(
            Object.keys(colorByKey).map(k => [data.groupNames[k], colorByKey[k]])
        );
        const el = document.querySelector('#dealsChart');
        if (!el) { return; }

        if (serviceChart) { serviceChart.destroy(); }

        const seriesLength = data.series.length > 0 ? data.series[0].data.length : 12;
        const categories = Array.from(data.xaxisValues).slice(0, seriesLength);

        serviceChart = new ApexCharts(el, {
            chart: {
                height: 300,
                type: 'line',
                parentHeightOffset: 0,
                toolbar: { show: false },
                zoom: { enabled: false }
            },
            series: data.series,
            dataLabels: { enabled: false },
            stroke: { show: true, curve: 'smooth', width: 4 },
            legend: {
                show: true,
                position: 'bottom',
                markers: { size: 6, strokeWidth: 0 },
                labels: { colors: labelColor, useSeriesColors: false }
            },
            colors: data.series.map(s => nameToColor[s.name]),
            grid: {
                borderColor: borderColor,
                xaxis: { lines: { show: true } }
            },
            xaxis: {
                categories: categories,
                axisBorder: { show: false },
                axisTicks: { show: false },
                labels: {
                    style: { colors: labelColor, fontSize: '13px', fontFamily: fontFamily }
                }
            },
            yaxis: {
                tickAmount: 4,
                labels: {
                    formatter: val => parseInt(val),
                    style: { colors: labelColor, fontSize: '13px', fontFamily: fontFamily }
                }
            },
            tooltip: { shared: false }
        });

        serviceChart.render();
    }

    function buildYearDropdown(years, selectedYear) {
        const menu = document.querySelector('#incomesYearDropdown');
        const label = document.querySelector('#selectedIncomesYear');
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

    function buildYearDropdown2(years, selectedYear) {
        const menu = document.querySelector('#dealsYearDropdown');
        const label = document.querySelector('#selectedDealsYear');
        if (!menu || !label) { return; }

        label.textContent = selectedYear;
        menu.innerHTML = '';

        years.forEach(function (y) {
            const li = document.createElement('li');
            const a = document.createElement('a');
            a.className = 'dropdown-item year-filter-item2' + (y === selectedYear ? ' active' : '');
            a.href = 'javascript:void(0);';
            a.dataset.year = y;
            a.textContent = y;
            li.appendChild(a);
            menu.appendChild(li);
        });
    }

    function loadDeals(year) {
        Block.pulse('#dealsCard');
        $.get('/Dashboard/GetCompanyBoardDeal', { year: year }, function (res) {
            Block.remove('#dealsCard');
            if (!res || !res.result) { return; }
            const data = res.result;
            buildYearDropdown2(data.years, data.year);
            initServiceChart(data.chart);
        });
    }

    function loadIncomes(year) {
        Block.pulse('#incomesCard');
        $.get('/Dashboard/GetCompanyBoardIncome', { year: year }, function (res) {
            Block.remove('#incomesCard');
            if (!res || !res.result) { return; }
            const data = res.result;
            buildYearDropdown(data.years, data.year);
            initInvoiceChart(data.chart);
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

    const ratingEl = document.querySelector('#companyRating');
    if (ratingEl) {
        new Raty(ratingEl, {
            starType: 'i',
            starOn: 'icon-base ri ri-star-fill text-warning icon-16px',
            starHalf: 'icon-base ri ri-star-half-line text-warning icon-16px',
            starOff: 'icon-base ri ri-star-line text-muted icon-16px',
            score: parseFloat(ratingEl.dataset.score) || 0,
            half: true,
            readOnly: true
        }).init();

        let lastFilled = null;
        ratingEl.querySelectorAll('i').forEach(function (star) {
            if (star.classList.contains('ri-star-fill') || star.classList.contains('ri-star-half-line')) {
                lastFilled = star;
            }
        });
        if (lastFilled) {
            lastFilled.classList.add('icon-36px');
        }
    }

    loadIncomes(0);
    loadDeals(0);

    $(document).on('click', '.year-filter-item', function () {
        const year = parseInt($(this).data('year'));
        loadIncomes(year);
    });

    $(document).on('click', '.year-filter-item2', function () {
        const year = parseInt($(this).data('year'));
        loadDeals(year);
    });
})();
