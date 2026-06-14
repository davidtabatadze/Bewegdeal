const CACHE = 'bewegdeal-v1';

const PRECACHE = [
  '/offline.html',
  '/css/site.css',
  '/css/demo.css',
  '/img/branding/logo.png',
  '/img/branding/brand-img-light.png',
  '/img/branding/brand-img-dark.png',
  '/img/branding/brand-img-small.png'
];

self.addEventListener('install', function (e) {
  e.waitUntil(
    caches.open(CACHE).then(function (cache) {
      return cache.addAll(PRECACHE);
    })
  );
  self.skipWaiting();
});

self.addEventListener('activate', function (e) {
  e.waitUntil(
    caches.keys().then(function (keys) {
      return Promise.all(
        keys.filter(function (k) { return k !== CACHE; }).map(function (k) { return caches.delete(k); })
      );
    })
  );
  self.clients.claim();
});

self.addEventListener('fetch', function (e) {
  if (e.request.method !== 'GET') { return; }

  if (e.request.mode === 'navigate') {
    e.respondWith(
      fetch(e.request).catch(function () {
        return caches.match('/offline.html');
      })
    );
    return;
  }

  e.respondWith(
    caches.match(e.request).then(function (cached) {
      return cached || fetch(e.request).then(function (response) {
        if (!response || response.status !== 200 || response.type !== 'basic') {
          return response;
        }
        var toCache = response.clone();
        caches.open(CACHE).then(function (cache) {
          cache.put(e.request, toCache);
        });
        return response;
      });
    })
  );
});
