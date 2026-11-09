// Kaart met coaches per stad. Simpel en licht: Leaflet + een rustige lichtgrijze Esri-ondergrond, en alles wordt
// pas geladen als iemand de kaart opent. Steden worden één keer opgezocht (Photon, anders Nominatim)
// en daarna in de browser onthouden.
//
//   openCoachKaart(coaches, { prijsTekst: c => '€50', onStadKies: stad => ... });
(function () {
    const FOTO_BASIS = 'https://racecoachfinder-production.up.railway.app';
    const CACHE_SLEUTEL = 'kcf_geo_v2';
    const NIET_GEVONDEN_DAGEN = 7;
    let leafletBelofte = null;
    let kaart = null, laag = null, overlay = null, opties = {};
    let verbergTimer = null;

    const esc = s => { const d = document.createElement('div'); d.textContent = String(s ?? ''); return d.innerHTML; };
    const isAanraak = () => window.matchMedia('(hover: none)').matches;

    function laadLeaflet() {
        if (window.L) return Promise.resolve();
        if (leafletBelofte) return leafletBelofte;
        leafletBelofte = new Promise((ok, fout) => {
            const css = document.createElement('link');
            css.rel = 'stylesheet';
            css.href = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.css';
            document.head.appendChild(css);
            const js = document.createElement('script');
            js.src = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.js';
            js.onload = ok;
            js.onerror = () => { leafletBelofte = null; fout(new Error('Kaart kon niet geladen worden.')); };
            document.head.appendChild(js);
        });
        return leafletBelofte;
    }

    // ---------- Steden opzoeken (met cache) ----------
    function leesCache() { try { return JSON.parse(localStorage.getItem(CACHE_SLEUTEL) || '{}'); } catch { return {}; } }
    function schrijfCache(c) { try { localStorage.setItem(CACHE_SLEUTEL, JSON.stringify(c)); } catch { } }

    // Alleen plaatsen (geen landen/regio's) in West-Europa, met voorkeur voor NL/BE
    const PLAATS_TAGS = ['city', 'town', 'village', 'hamlet', 'suburb'].map(t => '&osm_tag=place:' + t).join('');
    const LANDEN = ['nederland', 'netherlands', 'holland', 'belgie', 'belgië', 'belgium', 'duitsland', 'germany',
        'deutschland', 'frankrijk', 'france', 'luxemburg', 'luxembourg', 'engeland', 'england', 'verenigd koninkrijk'];

    async function zoekPhoton(stad) {
        const url = 'https://photon.komoot.io/api/?limit=1&lat=51.8&lon=5.0&bbox=-6,42,16,57' + PLAATS_TAGS + '&q=' + encodeURIComponent(stad);
        const r = await fetch(url);
        if (!r.ok) throw new Error('photon');
        const f = (await r.json()).features?.[0];
        return f ? { lat: f.geometry.coordinates[1], lon: f.geometry.coordinates[0], naam: f.properties?.name } : null;
    }
    async function zoekNominatim(stad) {
        const url = 'https://nominatim.openstreetmap.org/search?format=json&limit=1&featureType=settlement&viewbox=-6,57,16,42&bounded=1&q=' + encodeURIComponent(stad);
        const r = await fetch(url, { headers: { 'Accept-Language': 'nl' } });
        if (!r.ok) throw new Error('nominatim');
        const f = (await r.json())[0];
        return f ? { lat: +f.lat, lon: +f.lon, naam: f.name } : null;
    }

    async function zoekSteden(steden) {
        const cache = leesCache();
        const nu = Date.now();
        const teZoeken = steden.filter(s => {
            const c = cache[s];
            return !c || (!c.lat && nu - (c.t || 0) > NIET_GEVONDEN_DAGEN * 864e5);
        });
        // Een paar tegelijk, zodat het snel gaat zonder de dienst te overladen
        let i = 0;
        async function werker() {
            while (i < teZoeken.length) {
                const stad = teZoeken[i++];
                let plek = null;
                if (LANDEN.includes(stad)) { cache[stad] = { t: nu }; continue; } // een land is geen woonplaats
                try { plek = await zoekPhoton(stad); }
                catch { try { plek = await zoekNominatim(stad); } catch { plek = undefined; } }
                if (plek !== undefined) cache[stad] = plek ? { ...plek, t: nu } : { t: nu };
            }
        }
        await Promise.all([werker(), werker(), werker()]);
        schrijfCache(cache);
        return cache;
    }

    // ---------- Weergave ----------
    function initialen(naam) { return (naam || '?').split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase(); }
    function avatar(c, klasse = '') {
        return c.fotoUrl
            ? `<img class="ck-foto ${klasse}" src="${FOTO_BASIS}${esc(c.fotoUrl)}" alt="">`
            : `<span class="ck-foto ck-initialen ${klasse}">${esc(initialen(c.naam))}</span>`;
    }
    function sterren(score) {
        let h = '';
        for (let i = 1; i <= 5; i++) h += `<span style="color:${i <= Math.round(score) ? '#c9a800' : '#ddd'}">★</span>`;
        return h;
    }
    function hoverKaartHtml(c) {
        const prijs = opties.prijsTekst ? opties.prijsTekst(c) : '';
        return `
            <div class="ck-hk-kop">${avatar(c, 'ck-foto-groot')}
                <div><div class="ck-hk-naam">${esc(c.naam)}</div><div class="ck-hk-sub">📍 ${esc(c.woonplaats || '—')}</div></div>
            </div>
            <div class="ck-hk-badges">
                ${c.onlineCoaching ? '<span class="badge badge-groen">Online</span>' : ''}
                ${c.jarenErvaring > 0 ? `<span class="badge badge-grijs">${c.jarenErvaring} jr. ervaring</span>` : ''}
            </div>
            ${c.aantalReviews > 0 ? `<div class="ck-hk-sterren">${sterren(c.gemiddeldeScore)} <small>${(+c.gemiddeldeScore).toFixed(1)} (${c.aantalReviews})</small></div>` : ''}
            ${prijs ? `<div class="ck-hk-prijs">${esc(prijs)}</div>` : ''}
            ${c.bio ? `<div class="ck-hk-bio">${esc(c.bio)}</div>` : ''}
            <a class="btn btn-primair ck-hk-knop" href="coach-detail.html?id=${c.id}">Bekijk profiel</a>`;
    }

    function toonHoverKaart(c, anker) {
        clearTimeout(verbergTimer);
        const hk = overlay.querySelector('.ck-hoverkaart');
        hk.innerHTML = hoverKaartHtml(c);
        hk.style.display = 'block';
        const venster = overlay.querySelector('.ck-kaart').getBoundingClientRect();
        const a = anker.getBoundingClientRect();
        const b = hk.offsetWidth, h = hk.offsetHeight;
        const lijst = anker.closest('.leaflet-popup');
        let left, top;
        if (lijst) {
            // In de lijst van een stad: naast de lijst, zodat de andere coaches zichtbaar blijven
            const l = lijst.getBoundingClientRect();
            left = l.right - venster.left + 8;
            if (left + b > venster.width - 8) left = l.left - venster.left - b - 8;
            top = a.top - venster.top - 12;
        } else {
            left = a.left + a.width / 2 - b / 2 - venster.left;
            top = a.top - venster.top - h - 10;                 // liefst boven de coach
            if (top < 8) top = a.bottom - venster.top + 10;     // anders eronder
        }
        left = Math.max(8, Math.min(left, venster.width - b - 8));
        top = Math.max(8, Math.min(top, venster.height - h - 8));
        hk.style.left = left + 'px';
        hk.style.top = top + 'px';
    }
    function verbergHoverKaart(direct) {
        clearTimeout(verbergTimer);
        const doe = () => { const hk = overlay && overlay.querySelector('.ck-hoverkaart'); if (hk) hk.style.display = 'none'; };
        if (direct) doe(); else verbergTimer = setTimeout(doe, 180);
    }

    // Hover (computer) of tik (telefoon) op een coach
    function koppelCoach(el, c) {
        el.addEventListener('mouseenter', () => { if (!isAanraak()) toonHoverKaart(c, el); });
        el.addEventListener('mouseleave', () => { if (!isAanraak()) verbergHoverKaart(); });
        el.addEventListener('click', e => {
            e.stopPropagation();
            if (isAanraak()) toonHoverKaart(c, el);
            else location.href = 'coach-detail.html?id=' + c.id;
        });
    }

    function maakOverlay() {
        overlay = document.createElement('div');
        overlay.className = 'ck-overlay';
        overlay.innerHTML = `
            <div class="ck-venster" role="dialog" aria-modal="true" aria-label="Coaches op de kaart">
                <div class="ck-kop">
                    <button type="button" class="ck-filterknop">⚙ Filters</button>
                    <strong>🗺️ Coaches op de kaart</strong>
                    <span class="ck-info"></span>
                    <button type="button" class="ck-sluiten" title="Sluiten">✕</button>
                </div>
                <div class="ck-hoofd">
                    <div class="ck-zijkant">
                        <div class="ck-zijkant-inhoud"></div>
                        <button type="button" class="btn btn-primair ck-zijkant-klaar">Toon op de kaart</button>
                    </div>
                    <div class="ck-kaartvak">
                        <div class="ck-kaart"></div>
                        <div class="ck-hoverkaart"></div>
                    </div>
                </div>
                <div class="ck-voet"></div>
            </div>`;
        document.body.appendChild(overlay);
        overlay.querySelector('.ck-sluiten').addEventListener('click', sluit);
        overlay.addEventListener('mousedown', e => { if (e.target === overlay) sluit(); });
        const zijkant = overlay.querySelector('.ck-zijkant');
        overlay.querySelector('.ck-filterknop').addEventListener('click', () => zijkant.classList.toggle('open'));
        overlay.querySelector('.ck-zijkant-klaar').addEventListener('click', () => zijkant.classList.remove('open'));
        const hk = overlay.querySelector('.ck-hoverkaart');
        hk.addEventListener('mouseenter', () => clearTimeout(verbergTimer));
        hk.addEventListener('mouseleave', () => verbergHoverKaart());
        document.addEventListener('keydown', e => { if (e.key === 'Escape' && overlay.classList.contains('open')) sluit(); });
    }

    // De filters van de pagina zelf verhuizen tijdens het kijken naar de kaart naar de zijkant
    // (zelfde elementen, dus alle filters blijven werken) en gaan bij sluiten weer terug.
    let filterPlek = null;
    function leenFilters(el) {
        if (!el || filterPlek) return;
        filterPlek = document.createComment('filters');
        el.parentNode.insertBefore(filterPlek, el);
        overlay.querySelector('.ck-zijkant-inhoud').appendChild(el);
        overlay.classList.add('met-filters');
    }
    function geefFiltersTerug() {
        if (!filterPlek) return;
        const el = overlay.querySelector('.ck-zijkant-inhoud').firstElementChild;
        if (el) filterPlek.parentNode.insertBefore(el, filterPlek);
        filterPlek.remove();
        filterPlek = null;
        overlay.classList.remove('met-filters');
    }

    function sluit() {
        overlay.classList.remove('open');
        overlay.querySelector('.ck-zijkant').classList.remove('open');
        document.body.style.overflow = '';
        verbergHoverKaart(true);
        geefFiltersTerug();
    }

    function stadPopup(stad, lijst) {
        const el = document.createElement('div');
        el.className = 'ck-lijst';
        el.innerHTML = `<div class="ck-lijst-kop">📍 ${esc(stad)} · ${lijst.length} coaches</div>`;
        lijst.forEach(c => {
            const rij = document.createElement('div');
            rij.className = 'ck-rij';
            rij.innerHTML = `${avatar(c)}<span>${esc(c.naam)}</span>`;
            koppelCoach(rij, c);
            el.appendChild(rij);
        });
        if (opties.onStadKies) {
            const knop = document.createElement('button');
            knop.type = 'button';
            knop.className = 'ck-lijst-filter';
            knop.textContent = `Toon alleen ${stad} in de lijst`;
            knop.addEventListener('click', () => { sluit(); opties.onStadKies(stad); });
            el.appendChild(knop);
        }
        return el;
    }

    // Pins tekenen. Bij het openen zoomt de kaart naar de coaches; bij een filterwijziging blijft hij staan.
    let tekenVolgnummer = 0;
    async function teken(coaches, inzoomen) {
        const nr = ++tekenVolgnummer;
        const info = overlay.querySelector('.ck-info');
        const voet = overlay.querySelector('.ck-voet');

        // Coaches per stad groeperen (hoofdletters/spaties maken niet uit)
        const perStad = new Map();
        const zonderStad = [];
        coaches.forEach(c => {
            const naam = (c.woonplaats || '').trim();
            if (!naam) { zonderStad.push(c); return; }
            const sleutel = naam.toLowerCase();
            if (!perStad.has(sleutel)) perStad.set(sleutel, { naam, coaches: [] });
            perStad.get(sleutel).coaches.push(c);
        });

        if (perStad.size) info.textContent = 'Plaatsen zoeken…';
        const plekken = await zoekSteden([...perStad.keys()]);
        if (nr !== tekenVolgnummer) return; // intussen weer een filter gewijzigd

        verbergHoverKaart(true);
        kaart.closePopup();
        if (laag) laag.remove();
        laag = L.layerGroup().addTo(kaart);
        const punten = [];
        let onbekend = zonderStad.length;

        perStad.forEach((stad, sleutel) => {
            const plek = plekken[sleutel];
            if (!plek || plek.lat == null) { onbekend += stad.coaches.length; return; }
            const pos = [plek.lat, plek.lon];
            punten.push(pos);
            if (plek.naam) stad.naam = plek.naam; // nette schrijfwijze, bv. 'Den Haag' i.p.v. 'den haag'
            if (stad.coaches.length === 1) {
                const c = stad.coaches[0];
                const m = L.marker(pos, { icon: L.divIcon({ className: 'ck-icoon', html: `<div class="ck-pin">${avatar(c)}<span class="ck-naam">${esc(c.naam)}</span></div>`, iconSize: null }), riseOnHover: true });
                m.on('add', () => koppelCoach(m.getElement().querySelector('.ck-pin'), c));
                m.addTo(laag);
            } else {
                const m = L.marker(pos, { icon: L.divIcon({ className: 'ck-icoon', html: `<div class="ck-pin ck-groep"><span class="ck-aantal">${stad.coaches.length}</span><span class="ck-naam">${esc(stad.naam)}</span></div>`, iconSize: null }), riseOnHover: true }).addTo(laag);
                m.bindPopup(() => stadPopup(stad.naam, stad.coaches), { offset: [0, -44], closeButton: true, autoPanPadding: [20, 60], className: 'ck-popup' });
                m.on('popupclose', () => verbergHoverKaart(true));
            }
        });

        // Bij openen, of als na een filterwijziging geen enkele coach meer in beeld is: naar de coaches toe
        const zichtbaar = punten.some(pt => kaart.getBounds().contains(pt));
        if (inzoomen || (punten.length && !zichtbaar)) {
            if (punten.length > 1) kaart.fitBounds(punten, { padding: [60, 60], maxZoom: 10 });
            else if (punten.length === 1) kaart.setView(punten[0], 9);
        }

        const totaal = coaches.length - onbekend;
        info.textContent = totaal === 0
            ? 'Geen coaches met deze filters'
            : `${totaal} coach${totaal === 1 ? '' : 'es'} in ${punten.length} plaats${punten.length === 1 ? '' : 'en'}`;
        overlay.querySelector('.ck-filterknop').textContent = `⚙ Filters · ${totaal}`;
        voet.textContent = (isAanraak() ? 'Tik op een coach voor zijn kaart. ' : 'Ga met je muis over een coach voor zijn kaart, klik om het profiel te openen. ')
            + 'Een getal = meerdere coaches in die plaats.'
            + (onbekend ? ` ${onbekend} coach${onbekend === 1 ? ' staat' : 'es staan'} niet op de kaart (woonplaats onbekend).` : '');
    }

    // opties: prijsTekst(c), onStadKies(stad), filters (element dat tijdens de kaart in de zijkant komt)
    window.openCoachKaart = async function (coaches, o = {}) {
        opties = o;
        if (!overlay) maakOverlay();
        overlay.classList.add('open');
        document.body.style.overflow = 'hidden';
        leenFilters(o.filters);
        const info = overlay.querySelector('.ck-info');
        overlay.querySelector('.ck-voet').textContent = '';
        info.textContent = 'Kaart laden…';
        try {
            await laadLeaflet();
        } catch (e) { info.textContent = e.message; return; }

        if (!kaart) {
            kaart = L.map(overlay.querySelector('.ck-kaart'), { minZoom: 5, maxZoom: 13, zoomSnap: 0.5 }).setView([51.6, 5.3], 7);
            // Rustige lichtgrijze ondergrond met alleen grenzen, wegen en plaatsnamen (geen sleutel nodig)
            const esri = 'https://server.arcgisonline.com/ArcGIS/rest/services/Canvas/';
            L.tileLayer(esri + 'World_Light_Gray_Base/MapServer/tile/{z}/{y}/{x}', {
                maxZoom: 16, attribution: 'Kaart © Esri, HERE, Garmin, © OpenStreetMap'
            }).addTo(kaart);
            L.tileLayer(esri + 'World_Light_Gray_Reference/MapServer/tile/{z}/{y}/{x}', { maxZoom: 16 }).addTo(kaart);
            kaart.on('click movestart zoomstart', () => verbergHoverKaart(true));
            kaart.on('click', () => overlay.querySelector('.ck-zijkant').classList.remove('open'));
        }
        setTimeout(() => kaart.invalidateSize(), 0);
        await teken(coaches, true);
    };

    // Na een filterwijziging: pins bijwerken als de kaart open is
    window.verversCoachKaart = function (coaches) {
        if (overlay && overlay.classList.contains('open') && kaart) teken(coaches, false);
    };
    window.coachKaartIsOpen = () => !!(overlay && overlay.classList.contains('open'));
})();
