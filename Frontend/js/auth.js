const TOKEN_KEY = 'kcf_token';
const GEBRUIKER_KEY = 'kcf_gebruiker';
const ACTIEVE_ROL_KEY = 'kcf_actieve_rol';

function getToken() { return localStorage.getItem(TOKEN_KEY); }

function getGebruiker() {
    const data = localStorage.getItem(GEBRUIKER_KEY);
    return data ? JSON.parse(data) : null;
}

function isIngelogd() { return !!getToken(); }

function getActieveRol() { return localStorage.getItem(ACTIEVE_ROL_KEY); }
function setActieveRol(rol) { localStorage.setItem(ACTIEVE_ROL_KEY, rol); }

function wisselRol() {
    const actief = getActieveRol();
    const nieuw = actief === 'Coach' ? 'Rijder' : 'Coach';
    setActieveRol(nieuw);
    location.href = nieuw === 'Coach' ? 'dashboard-coach.html' : 'dashboard-rijder.html';
}

function uitloggen() {
    // Dit apparaat geen meldingen meer laten krijgen voor het account dat uitlogt
    try {
        const t = localStorage.getItem(TOKEN_KEY);
        if (t && navigator.serviceWorker) {
            navigator.serviceWorker.getRegistration().then(reg => reg && reg.pushManager.getSubscription()).then(abo => {
                if (abo) fetch('https://racecoachfinder-production.up.railway.app/api/push/afmelden', {
                    method: 'POST', keepalive: true,
                    headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + t },
                    body: JSON.stringify({ endpoint: abo.endpoint })
                });
            }).catch(() => {});
        }
        sessionStorage.removeItem('kcf_push_gesynct');
    } catch (e) {}
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(GEBRUIKER_KEY);
    location.href = 'index.html';
}

function authHeader() {
    const token = getToken();
    return token ? { 'Authorization': 'Bearer ' + token } : {};
}

async function authVerzoek(pad, methode, body) {
    methode = methode || 'GET';
    const opties = {
        method: methode,
        headers: Object.assign({ 'Content-Type': 'application/json' }, authHeader()),
    };
    if (body) opties.body = JSON.stringify(body);
    const antwoord = await fetch('https://racecoachfinder-production.up.railway.app/api' + pad, opties);
    if (antwoord.status === 401) { uitloggen(); return; }
    if (!antwoord.ok) {
        const tekst = await antwoord.text();
        throw new Error(tekst || 'Fout ' + antwoord.status);
    }
    if (antwoord.status === 204) return null;
    return antwoord.json();
}

function heeftRol(rol) {
    const g = getGebruiker();
    if (!g) return false;
    return (g.rol || '').split(',').map(function(r) { return r.trim(); }).indexOf(rol) !== -1;
}

function vereisAuth(vereistRol) {
    vereistRol = vereistRol || null;
    const gebruiker = getGebruiker();
    if (!gebruiker) { location.href = 'inloggen.html'; return false; }
    if (gebruiker.heeftAccountIngericht === false) { location.href = 'account-inrichten.html'; return false; }
    if (vereistRol) {
        const rollen = (gebruiker.rol || '').split(',').map(function(r) { return r.trim(); });
        if (rollen.indexOf(vereistRol) === -1) {
            // User doesn't have this role at all
            if (rollen.indexOf('Coach') !== -1) location.href = 'dashboard-coach.html';
            else if (rollen.indexOf('Admin') !== -1) location.href = 'admin.html';
            else location.href = 'dashboard-rijder.html';
            return false;
        }
        // Dual-role: enforce active role
        const heeftBeide = rollen.indexOf('Coach') !== -1 && rollen.indexOf('Rijder') !== -1;
        if (heeftBeide) {
            const actief = getActieveRol();
            if (!actief) { location.href = 'kies-rol.html'; return false; }
            if (actief !== vereistRol) {
                location.href = actief === 'Coach' ? 'dashboard-coach.html' : 'dashboard-rijder.html';
                return false;
            }
        }
    }
    return true;
}

const _LOGO_HTML = '<img src="Logo\'s/RaceCoachFinder logo - rechthoek (zwart url).png" alt="RaceCoachFinder" style="height:80px;width:auto;display:block">';

function updateNav() {
    const gebruiker = getGebruiker();
    const navLinks = document.querySelector('.nav-links');

    // Logo: vervang span door klikbare <a>
    const logoEl = document.querySelector('.nav-logo');
    if (logoEl) {
        if (logoEl.tagName !== 'A') {
            const a = document.createElement('a');
            a.href = 'index.html';
            a.className = 'nav-logo';
            a.innerHTML = _LOGO_HTML;
            logoEl.replaceWith(a);
        } else {
            logoEl.href = 'index.html';
            logoEl.innerHTML = _LOGO_HTML;
        }
    }

    // Hamburger knop toevoegen als die er nog niet is
    const navEl = document.querySelector('nav, .navbar');
    if (navEl && !navEl.querySelector('.nav-hamburger')) {
        const btn = document.createElement('button');
        btn.className = 'nav-hamburger';
        btn.setAttribute('aria-label', 'Menu');
        btn.innerHTML = '<span></span><span></span><span></span>';
        btn.addEventListener('click', function() {
            btn.classList.toggle('open');
            if (navLinks) navLinks.classList.toggle('open');
        });
        navEl.appendChild(btn);
    }

    if (!navLinks) return;

    // Huidige pagina (werkt met en zonder .html in de url)
    var pagina = (location.pathname.split('/').pop() || 'index.html').toLowerCase();
    if (pagina.indexOf('.') === -1) pagina += '.html';
    var actiefVoor = { 'coach-detail.html': 'coaches.html' };
    var huidige = actiefVoor[pagina] || pagina;
    function link(href, tekst, extra) {
        return '<li><a href="' + href + '"' + (href === huidige ? ' class="actief" aria-current="page"' : '') + '>' +
               tekst + (extra || '') + '</a></li>';
    }

    var actieveTaal = (window.I18N && window.I18N.taal) || localStorage.getItem('kcf_taal') || 'nl';
    var taalHtml =
        '<li class="nav-taal"><div class="taal-toggle">' +
        '<button type="button" class="taal-optie' + (actieveTaal === 'nl' ? ' actief' : '') + '" id="taal-nl-btn" onclick="window.I18N&&window.I18N.setTaal(\'nl\')">NL</button>' +
        '<button type="button" class="taal-optie' + (actieveTaal === 'en' ? ' actief' : '') + '" id="taal-en-btn" onclick="window.I18N&&window.I18N.setTaal(\'en\')">EN</button>' +
        '</div></li>';

    var html =
        link('index.html', 'Home') +
        link('coaches.html', 'Coaches') +
        link('trainingen.html', 'Groepscoaching') +
        link('persoonlijk-coachen.html', 'Persoonlijk coachen');

    if (gebruiker) {
        var rollen = (gebruiker.rol || '').split(',').map(function(r) { return r.trim(); });
        var heeftBeide = rollen.indexOf('Coach') !== -1 && rollen.indexOf('Rijder') !== -1;
        var dashUrl, rolTekst, wisselHtml = '';
        if (heeftBeide) {
            var actief = getActieveRol() === 'Coach' ? 'Coach' : 'Rijder';
            dashUrl = actief === 'Coach' ? 'dashboard-coach.html' : 'dashboard-rijder.html';
            rolTekst = actief === 'Coach' ? 'Actief als coach' : 'Actief als rijder';
            wisselHtml = '<a href="#" onclick="wisselRol();return false;"><span class="nav-menu-icoon">⇄</span><span>' +
                         (actief === 'Coach' ? 'Wissel naar rijder' : 'Wissel naar coach') + '</span></a>';
        } else if (rollen.indexOf('Admin') !== -1) {
            dashUrl = 'admin.html'; rolTekst = 'Beheerder';
        } else if (rollen.indexOf('Coach') !== -1) {
            dashUrl = 'dashboard-coach.html'; rolTekst = 'Coach';
        } else {
            dashUrl = 'dashboard-rijder.html'; rolTekst = 'Rijder';
        }
        var naam = String(gebruiker.naam || '').trim() || 'Account';
        var delen = naam.split(/\s+/);
        var initialen = (delen[0].charAt(0) + (delen.length > 1 ? delen[delen.length - 1].charAt(0) : '')).toUpperCase();

        html +=
            link('berichten.html', 'Berichten', '<span id="nav-badge" class="nav-badge" style="display:none"></span>') +
            '<li class="nav-scheiding" aria-hidden="true"></li>' +
            taalHtml +
            '<li class="nav-account">' +
                '<button type="button" class="nav-account-knop" aria-haspopup="true" aria-expanded="false" title="' + _esc(naam) + '">' +
                    '<span class="nav-avatar">' + _esc(initialen) + '</span>' +
                    '<span class="nav-account-naam">' + _esc(delen[0]) + '</span>' +
                    '<span class="nav-pijl" aria-hidden="true">▾</span>' +
                '</button>' +
                '<div class="nav-account-menu">' +
                    '<div class="nav-account-kop"><strong>' + _esc(naam) + '</strong><span>' + rolTekst + '</span></div>' +
                    '<a href="' + dashUrl + '"><span class="nav-menu-icoon">▦</span><span>Mijn dashboard</span></a>' +
                    wisselHtml +
                    '<a href="instellingen.html"><span class="nav-menu-icoon">⚙</span><span>Instellingen</span></a>' +
                    '<a href="#" class="nav-uitloggen" onclick="uitloggen();return false;"><span class="nav-menu-icoon">↪</span><span>Uitloggen</span></a>' +
                '</div>' +
            '</li>';
    } else {
        html +=
            '<li class="nav-scheiding" aria-hidden="true"></li>' +
            taalHtml +
            link('inloggen.html', 'Inloggen') +
            '<li><a href="registreren.html" class="nav-cta">Registreren</a></li>';
    }
    navLinks.innerHTML = html;

    // Accountmenu open/dicht
    var account = navLinks.querySelector('.nav-account');
    if (account) {
        var knop = account.querySelector('.nav-account-knop');
        knop.addEventListener('click', function(e) {
            e.stopPropagation();
            var open = account.classList.toggle('open');
            knop.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
    }
    if (!window._navMenuLuisteraar) {
        window._navMenuLuisteraar = true;
        document.addEventListener('click', function(e) {
            document.querySelectorAll('.nav-account.open').forEach(function(a) {
                if (!a.contains(e.target)) { a.classList.remove('open'); a.querySelector('.nav-account-knop').setAttribute('aria-expanded', 'false'); }
            });
        });
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') document.querySelectorAll('.nav-account.open').forEach(function(a) { a.classList.remove('open'); });
        });
        var wacht = null;
        window.addEventListener('resize', function() { cancelAnimationFrame(wacht); wacht = requestAnimationFrame(pasNavAan); });
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(pasNavAan);
        var logoImg = document.querySelector('.nav-logo img');
        if (logoImg && !logoImg.complete) logoImg.addEventListener('load', pasNavAan);
        setTimeout(pasNavAan, 400); // na vertaling naar het Engels
    }
    pasNavAan();

    if (gebruiker) laadBerichtenBadge();
}

// Past alles naast het logo? Zo niet: menuknop (hamburger) gebruiken.
function pasNavAan() {
    var nav = document.querySelector('nav, .navbar');
    var links = nav && nav.querySelector('.nav-links');
    var logo = nav && nav.querySelector('.nav-logo');
    if (!links || !logo) return;
    var vak = nav.querySelector('.nav-container') || nav;
    var wasOpen = links.classList.contains('open');
    nav.classList.remove('nav-compact');
    var stijl = getComputedStyle(vak);
    var ruimte = vak.clientWidth - parseFloat(stijl.paddingLeft) - parseFloat(stijl.paddingRight);
    var nodig = logo.getBoundingClientRect().width + 32 + links.scrollWidth;
    var compact = window.innerWidth <= 768 || nodig > ruimte;
    nav.classList.toggle('nav-compact', compact);
    if (!compact) {
        links.classList.remove('open');
        var h = nav.querySelector('.nav-hamburger');
        if (h) h.classList.remove('open');
    } else if (wasOpen) {
        links.classList.add('open');
    }
}

async function laadBerichtenBadge() {
    try {
        const aantal = await authVerzoek('/chat/ongelezen');
        const badge = document.getElementById('nav-badge');
        if (!badge) return;
        if (aantal > 0) {
            badge.textContent = aantal;
            badge.style.display = 'inline';
        } else {
            badge.style.display = 'none';
        }
    } catch {}
}

function _esc(str) {
    const d = document.createElement('div');
    d.textContent = String(str);
    return d.innerHTML;
}

// Cookie consent banner
(function() {
    const COOKIE_KEY = 'kcf_cookie_consent';

    function sluitBanner(banner) {
        banner.style.transform = 'translateY(100%)';
        banner.style.opacity = '0';
        setTimeout(function() { banner.remove(); }, 300);
    }

    function toonCookieBanner() {
        if (localStorage.getItem(COOKIE_KEY)) return;

        var stijl = document.createElement('style');
        stijl.textContent =
            '#kcf-cookie-banner{position:fixed;bottom:0;left:0;right:0;z-index:9999;' +
            'background:#111;color:#e8e8e8;padding:1rem 1.5rem;display:flex;align-items:center;' +
            'gap:1.25rem;flex-wrap:wrap;box-shadow:0 -2px 16px rgba(0,0,0,0.25);' +
            'transform:translateY(0);opacity:1;transition:transform 0.3s ease,opacity 0.3s ease;}' +
            '#kcf-cookie-banner p{margin:0;font-size:0.83rem;line-height:1.5;flex:1;min-width:200px;}' +
            '#kcf-cookie-banner a{color:#F5C200;text-underline-offset:2px;}' +
            '#kcf-cookie-banner .kcf-cookie-knoppen{display:flex;gap:0.6rem;flex-shrink:0;}' +
            '#kcf-cookie-accept{background:#F5C200;color:#111;border:none;padding:0.5rem 1.1rem;' +
            'border-radius:6px;font-weight:700;font-size:0.82rem;cursor:pointer;}' +
            '#kcf-cookie-accept:hover{background:#e6b800;}' +
            '#kcf-cookie-functional{background:transparent;color:#aaa;border:1px solid #444;' +
            'padding:0.5rem 1rem;border-radius:6px;font-size:0.82rem;cursor:pointer;}' +
            '#kcf-cookie-functional:hover{color:#fff;border-color:#888;}';
        document.head.appendChild(stijl);

        var banner = document.createElement('div');
        banner.id = 'kcf-cookie-banner';
        banner.innerHTML =
            '<p>We gebruiken functionele cookies om het platform te laten werken. ' +
            'We plaatsen geen tracking- of advertentiecookies. ' +
            'Lees meer in ons <a href="privacybeleid.html">privacybeleid</a>.</p>' +
            '<div class="kcf-cookie-knoppen">' +
            '<button id="kcf-cookie-functional">Alleen functioneel</button>' +
            '<button id="kcf-cookie-accept">Accepteren</button>' +
            '</div>';
        document.body.appendChild(banner);

        document.getElementById('kcf-cookie-accept').addEventListener('click', function() {
            localStorage.setItem(COOKIE_KEY, 'accepted');
            sluitBanner(banner);
        });
        document.getElementById('kcf-cookie-functional').addEventListener('click', function() {
            localStorage.setItem(COOKIE_KEY, 'functional');
            sluitBanner(banner);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', toonCookieBanner);
    } else {
        toonCookieBanner();
    }
})();
