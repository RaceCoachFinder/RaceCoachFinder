// Service worker van RaceCoachFinder: toont pushmeldingen en opent de juiste pagina bij een tik
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));

self.addEventListener('push', event => {
    let data = {};
    try { data = event.data ? event.data.json() : {}; } catch { data = { tekst: event.data ? event.data.text() : '' }; }
    const titel = data.titel || 'RaceCoachFinder';
    event.waitUntil(self.registration.showNotification(titel, {
        body: data.tekst || '',
        icon: '/icons/icon-192.png',
        badge: '/icons/badge-96.png',
        tag: data.tag || undefined,
        renotify: !!data.tag,
        data: { url: data.url || '/berichten.html' }
    }));
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    const doel = new URL(event.notification.data && event.notification.data.url || '/', self.location.origin).href;
    event.waitUntil((async () => {
        const vensters = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        for (const venster of vensters) {
            if (new URL(venster.url).origin === self.location.origin && 'focus' in venster) {
                await venster.focus();
                if ('navigate' in venster) return venster.navigate(doel);
                return;
            }
        }
        return self.clients.openWindow(doel);
    })());
});
