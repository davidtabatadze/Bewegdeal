// v1.1.1
(function () {
  'use strict';

  // Register service worker
  if ('serviceWorker' in navigator) {
    window.addEventListener('load', function () {
      navigator.serviceWorker.register('/sw.js');
    });
  }

  // Don't show if already running as installed PWA
  if (window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone) {
    return;
  }

  var DISMISS_KEY = 'pwa-banner-dismissed';
  var DISMISS_DAYS = 7;

  function isDismissed() {
    try {
      var ts = localStorage.getItem(DISMISS_KEY);
      if (!ts) { return false; }
      return (Date.now() - parseInt(ts, 10)) < DISMISS_DAYS * 86400000;
    } catch (e) { return false; }
  }

  function dismiss() {
    try { localStorage.setItem(DISMISS_KEY, String(Date.now())); } catch (e) {}
  }

  function createBanner(html, onInstall) {
    var banner = document.createElement('div');
    banner.id = 'pwa-install-banner';
    banner.style.cssText = [
      'position:fixed', 'bottom:0', 'left:0', 'right:0', 'z-index:99999',
      'background:#fff', 'border-top:1px solid #e0e0e0',
      'box-shadow:0 -2px 12px rgba(0,0,0,.12)',
      'padding:12px 16px', 'display:flex', 'align-items:center',
      'gap:12px', 'font-family:Inter,sans-serif', 'font-size:14px', 'color:#333'
    ].join(';');

    var icon = document.createElement('img');
    icon.src = '/img/branding/icon-192.png';
    icon.style.cssText = 'width:44px;height:44px;border-radius:10px;flex-shrink:0';

    var textDiv = document.createElement('div');
    textDiv.style.flex = '1';
    textDiv.innerHTML = html;

    var closeBtn = document.createElement('button');
    closeBtn.textContent = '✕';
    closeBtn.style.cssText = [
      'background:none', 'border:none', 'font-size:18px', 'cursor:pointer',
      'color:#999', 'padding:4px 8px', 'flex-shrink:0'
    ].join(';');
    closeBtn.addEventListener('click', function () {
      dismiss();
      banner.remove();
    });

    banner.appendChild(icon);
    banner.appendChild(textDiv);
    if (onInstall) {
      var installBtn = document.createElement('button');
      installBtn.textContent = 'Install';
      installBtn.style.cssText = [
        'background:#696cff', 'color:#fff', 'border:none', 'border-radius:6px',
        'padding:8px 16px', 'font-size:13px', 'font-weight:600', 'cursor:pointer',
        'flex-shrink:0', 'white-space:nowrap'
      ].join(';');
      installBtn.addEventListener('click', function () {
        onInstall();
        banner.remove();
      });
      banner.appendChild(installBtn);
    }
    banner.appendChild(closeBtn);
    document.body.appendChild(banner);
  }

  function isIos() {
    return /iphone|ipad|ipod/i.test(navigator.userAgent);
  }

  function isSafariBrowser() {
    return /safari/i.test(navigator.userAgent) && !/chrome|crios|fxios/i.test(navigator.userAgent);
  }

  // Android / Chrome: capture beforeinstallprompt
  var deferredPrompt = null;
  window.addEventListener('beforeinstallprompt', function (e) {
    e.preventDefault();
    deferredPrompt = e;

    if (isDismissed()) { return; }

    createBanner(
      '<strong>Install Bewegdeal</strong><br>Add to your home screen for quick access.',
      function () {
        deferredPrompt.prompt();
        deferredPrompt.userChoice.then(function () { deferredPrompt = null; });
        dismiss();
      }
    );
  });

  // iOS Safari: no beforeinstallprompt — show manual instructions
  if (isIos() && isSafariBrowser() && !isDismissed()) {
    window.addEventListener('load', function () {
      // Small delay so the page settles first
      setTimeout(function () {
        createBanner(
          '<strong>Install Bewegdeal</strong><br>' +
          'Tap <strong>Share</strong> <span style="font-size:16px">&#x1F4E4;</span> then <strong>"Add to Home Screen"</strong>.'
        );
      }, 2000);
    });
  }
})();
