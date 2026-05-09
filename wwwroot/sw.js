// Vest Service Worker — handles Web Push notifications
self.addEventListener('push', function (event) {
    if (!event.data) return;

    let data = {};
    try { data = event.data.json(); } catch { data = { title: 'Vest', body: event.data.text() }; }

    const title   = data.title || 'Vest';
    const options = {
        body:    data.body  || '',
        icon:    '/images/vest-rocket.png',
        badge:   '/images/favicon-32x32.png',
        data:    { url: data.url || '/' },
        vibrate: [150, 50, 150],
        actions: [{ action: 'open', title: 'View Dashboard' }]
    };

    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const url = event.notification.data?.url || '/';
    event.waitUntil(clients.openWindow(url));
});
