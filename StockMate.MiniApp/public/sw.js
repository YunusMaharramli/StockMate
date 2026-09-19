const appCache = 'stockmate-app-v1';
const imageCache = 'stockmate-product-images-v1';
const knownCaches = [appCache, imageCache];

self.addEventListener('install', event => {
  event.waitUntil(
    cacheApplicationShell()
      .then(() => self.skipWaiting()),
  );
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(
        keys
          .filter(key => key.startsWith('stockmate-') && !knownCaches.includes(key))
          .map(key => caches.delete(key)),
      ))
      .then(() => self.clients.claim()),
  );
});

self.addEventListener('fetch', event => {
  const { request } = event;
  if (request.method !== 'GET') return;

  const url = new URL(request.url);

  if (request.destination === 'image'
    && url.pathname.startsWith('/api/product-images/')) {
    event.respondWith(staleWhileRevalidate(request, imageCache));
    return;
  }

  if (request.mode === 'navigate') {
    event.respondWith(networkFirstNavigation(request));
    return;
  }

  if (url.origin === self.location.origin
    && ['script', 'style', 'font'].includes(request.destination)) {
    event.respondWith(staleWhileRevalidate(request, appCache));
  }
});

async function cacheApplicationShell() {
  const cache = await caches.open(appCache);
  const response = await fetch('/', { cache: 'reload' });
  if (!response.ok) return;

  await cache.put('/', response.clone());
  const html = await response.text();
  const assetUrls = [...html.matchAll(/(?:src|href)=["'](\/assets\/[^"']+)["']/g)]
    .map(match => match[1]);

  if (assetUrls.length > 0) await cache.addAll(assetUrls);
}

async function networkFirstNavigation(request) {
  const cache = await caches.open(appCache);

  try {
    const response = await fetch(request);
    if (response.ok) await cache.put('/', response.clone());
    return response;
  } catch {
    return await cache.match('/')
      ?? new Response('StockMate offline', {
        status: 503,
        headers: { 'Content-Type': 'text/plain; charset=utf-8' },
      });
  }
}

async function staleWhileRevalidate(request, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);
  const networkResponse = fetch(request)
    .then(async response => {
      if (response.ok || response.type === 'opaque') {
        await cache.put(request, response.clone());
      }
      return response;
    })
    .catch(() => null);

  return cached ?? await networkResponse ?? new Response('', { status: 503 });
}
