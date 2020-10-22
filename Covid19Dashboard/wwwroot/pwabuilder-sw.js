// This is the "Offline page" service worker

importScripts('https://storage.googleapis.com/workbox-cdn/releases/5.1.2/workbox-sw.js');

const CACHE = "pwabuilder-page";

// TODO: replace the following with the correct offline fallback page i.e.: const offlineFallbackPage = "offline.html";
const offlineFallbackPage = "Offline";

self.addEventListener("message", (event) => {
	if (event.data && event.data.type === "SKIP_WAITING") {
		self.skipWaiting();
	}
});

self.addEventListener('install', async (event) => {
	event.waitUntil(
		caches.open(CACHE)
			.then((cache) => cache.addAll([
				offlineFallbackPage,
				'/css/site.css'
			]))
	);
});

if (workbox.navigationPreload.isSupported()) {
	workbox.navigationPreload.enable();
}

self.addEventListener('fetch', (event) => {
	const { request } = event;
	event.respondWith((async () => {
		if (event.request.mode === 'navigate') {
			try {
				const preloadResp = await event.preloadResponse;

				if (preloadResp) {
					return preloadResp;
				}

				const networkResp = await fetch(event.request);
				return networkResp;
			} catch (error) {
				const cache = await caches.open(CACHE);
				const cachedResp = await cache.match(offlineFallbackPage);
				return cachedResp;
			}
		} else {
			return fetch(event.request).then(function (response) {
				return response;
			}).catch(async (error) => {
				const cachedResponse = await caches.match(request);
				if (cachedResponse) return cachedResponse;

				throw error;
			});
		}
	})());

});
