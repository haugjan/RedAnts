// Minimal service worker for the ticket-scanner PWA.
// Its only job is to make the app installable (browsers require a registered
// service worker with a fetch handler). The scanner needs a live server
// connection (Blazor Server circuit), so no offline caching is attempted.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));

// A fetch handler must exist for installability. Pass every request straight
// through to the network (no respondWith = default browser handling).
self.addEventListener('fetch', () => { /* network passthrough */ });
