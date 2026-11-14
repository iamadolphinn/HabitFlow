// Minimal service worker: makes the app installable and caches static assets.
// The Blazor Server UI itself needs a live connection, so pages are never served from cache.
const CACHE = "habittracker-static-v1";
const ASSETS = ["css/site.css", "js/site.js", "icon.svg"];

self.addEventListener("install", e => {
    e.waitUntil(caches.open(CACHE).then(c => c.addAll(ASSETS)));
    self.skipWaiting();
});

self.addEventListener("activate", e => {
    e.waitUntil(caches.keys().then(keys =>
        Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k)))));
});

self.addEventListener("fetch", e => {
    const url = new URL(e.request.url);
    if (e.request.method === "GET" && ASSETS.some(a => url.pathname.endsWith(a))) {
        e.respondWith(caches.match(e.request).then(r => r || fetch(e.request)));
    }
});
