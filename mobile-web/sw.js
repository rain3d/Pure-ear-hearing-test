// Service Worker for Offline Hearing Test PWA (v8 - Multi-Language i18n & syntax fix)
const CACHE_NAME = 'hearing-test-pwa-v8-i18n';
const ASSETS = [
  './',
  './index.html',
  './manifest.json',
  './css/app.css',
  './js/i18n.js',
  './js/audio-engine.js',
  './js/state-machine.js',
  './js/audiogram.js',
  './js/calibrator.js',
  './js/app.js',
  './icons/icon.svg'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS))
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(
        keys.map((key) => {
          if (key !== CACHE_NAME) return caches.delete(key);
        })
      )
    )
  );
  self.clients.claim();
});

// Network-First with Cache Fallback ensures mobile devices always receive latest JS code updates
self.addEventListener('fetch', (event) => {
  event.respondWith(
    fetch(event.request)
      .then((response) => {
        if (response && response.status === 200 && event.request.method === 'GET') {
          const responseClone = response.clone();
          caches.open(CACHE_NAME).then((cache) => {
            cache.put(event.request, responseClone);
          });
        }
        return response;
      })
      .catch(() => caches.match(event.request))
  );
});
