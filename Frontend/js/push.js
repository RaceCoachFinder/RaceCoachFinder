// Pushmeldingen op telefoon/computer (webpush, zonder app of telefoonnummer)
(function () {
    'use strict';
    const API = 'https://racecoachfinder-production.up.railway.app/api';

    function naarUint8(base64) {
        const opvulling = '='.repeat((4 - base64.length % 4) % 4);
        const ruw = atob((base64 + opvulling).replace(/-/g, '+').replace(/_/g, '/'));
        return Uint8Array.from([...ruw].map(c => c.charCodeAt(0)));
    }

    function token() { return localStorage.getItem('kcf_token'); }

    async function api(pad, body) {
        const res = await fetch(API + pad, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token() },
            body: JSON.stringify(body || {})
        });
        if (!res.ok) throw new Error((await res.text()) || ('Fout ' + res.status));
    }

    const Push = {
        isIOS() {
            return /iphone|ipad|ipod/i.test(navigator.userAgent) ||
                (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
        },
        isBeginscherm() {
            return window.matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
        },
        ondersteund() {
            return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
        },

        async registratie() {
            if (!('serviceWorker' in navigator)) return null;
            try { return await navigator.serviceWorker.register('/sw.js'); } catch { return null; }
        },

        // 'aan' | 'uit' | 'geweigerd' | 'ios-beginscherm' | 'niet-ondersteund'
        async status() {
            if (!this.ondersteund()) return this.isIOS() && !this.isBeginscherm() ? 'ios-beginscherm' : 'niet-ondersteund';
            if (Notification.permission === 'denied') return 'geweigerd';
            const reg = await this.registratie();
            const abo = reg && await reg.pushManager.getSubscription();
            return abo && Notification.permission === 'granted' ? 'aan' : 'uit';
        },

        async aanzetten() {
            const toestemming = await Notification.requestPermission();
            if (toestemming !== 'granted') throw new Error('Je hebt geen toestemming gegeven voor meldingen.');
            const reg = await this.registratie();
            if (!reg) throw new Error('Meldingen konden niet worden ingesteld op dit apparaat.');
            await navigator.serviceWorker.ready;
            const { sleutel } = await fetch(API + '/push/publieke-sleutel').then(r => r.json());
            let abo = await reg.pushManager.getSubscription();
            if (!abo) abo = await reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: naarUint8(sleutel) });
            await api('/push/abonneer', abo.toJSON());
            sessionStorage.setItem('kcf_push_gesynct', '1');
        },

        async uitzetten() {
            const reg = await this.registratie();
            const abo = reg && await reg.pushManager.getSubscription();
            if (abo) {
                try { await api('/push/afmelden', { endpoint: abo.endpoint }); } catch {}
                await abo.unsubscribe();
            }
        },

        async test() { await api('/push/test'); },

        // Na inloggen (of wisselen van account) het apparaat aan het juiste account koppelen
        async synchroniseer() {
            if (!token() || !this.ondersteund() || Notification.permission !== 'granted') return;
            if (sessionStorage.getItem('kcf_push_gesynct')) return;
            const reg = await this.registratie();
            const abo = reg && await reg.pushManager.getSubscription();
            if (!abo) return;
            try { await api('/push/abonneer', abo.toJSON()); sessionStorage.setItem('kcf_push_gesynct', '1'); } catch {}
        }
    };

    window.Push = Push;
    window.addEventListener('load', () => { Push.registratie().then(() => Push.synchroniseer()); });
})();
