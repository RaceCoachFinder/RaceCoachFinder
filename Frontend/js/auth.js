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
    navLinks.querySelectorAll('.nav-auth').forEach(function(el) { el.remove(); });

    if (gebruiker) {
        const rollen = (gebruiker.rol || '').split(',').map(function(r) { return r.trim(); });
        const heeftBeide = rollen.indexOf('Coach') !== -1 && rollen.indexOf('Rijder') !== -1;

        let dashboardHtml = '';
        let rolKnoppenHtml = '';
        if (heeftBeide) {
            const actief = getActieveRol();
            const andereRol = actief === 'Coach' ? 'Rijder' : 'Coach';
            const icoon = actief === 'Coach' ? '🏁' : '🏎️';
            const dashUrl = actief === 'Coach' ? 'dashboard-coach.html' : 'dashboard-rijder.html';
            dashboardHtml =
                '<li class="nav-auth"><a href="' + dashUrl + '">' + icoon + ' ' + _esc(gebruiker.naam) + '</a></li>' +
                '<li class="nav-auth"><a href="#" onclick="wisselRol();return false;" style="background:#f0f0f0;color:var(--kleur-donker);padding:0.25rem 0.9rem;border-radius:6px;font-weight:700;font-size:0.82rem;white-space:nowrap">⇄ ' + andereRol + '</a></li>';
            // Only show role-specific buttons based on active role
            if (actief === 'Rijder') {
                rolKnoppenHtml = '<li class="nav-auth"><a href="coach-nodig.html" style="background:var(--kleur-primair);color:var(--kleur-donker);padding:0.25rem 0.9rem;border-radius:6px;font-weight:700">Coach nodig?</a></li>';
            } else if (actief === 'Coach') {
                rolKnoppenHtml = '<li class="nav-auth"><a href="coach-gezocht.html" style="background:var(--kleur-primair);color:var(--kleur-donker);padding:0.25rem 0.9rem;border-radius:6px;font-weight:700">Coach gezocht</a></li>';
            }
        } else {
            let dashboardUrl = 'dashboard-rijder.html';
            if (rollen.indexOf('Coach') !== -1) dashboardUrl = 'dashboard-coach.html';
            if (rollen.indexOf('Admin') !== -1) dashboardUrl = 'admin.html';
            dashboardHtml = '<li class="nav-auth"><a href="' + dashboardUrl + '">' + _esc(gebruiker.naam) + '</a></li>';
            if (rollen.indexOf('Rijder') !== -1) {
                rolKnoppenHtml += '<li class="nav-auth"><a href="coach-nodig.html" style="background:var(--kleur-primair);color:var(--kleur-donker);padding:0.25rem 0.9rem;border-radius:6px;font-weight:700">Coach nodig?</a></li>';
            }
            if (rollen.indexOf('Coach') !== -1) {
                rolKnoppenHtml += '<li class="nav-auth"><a href="coach-gezocht.html" style="background:var(--kleur-primair);color:var(--kleur-donker);padding:0.25rem 0.9rem;border-radius:6px;font-weight:700">Coach gezocht</a></li>';
            }
        }

        navLinks.innerHTML +=
            '<li class="nav-auth"><a href="berichten.html">Berichten' +
            '<span id="nav-badge" style="display:none;background:#c62828;color:#fff;border-radius:50%;padding:0.05rem 0.42rem;font-size:0.7rem;font-weight:700;margin-left:0.35rem;vertical-align:middle"></span>' +
            '</a></li>' +
            dashboardHtml +
            rolKnoppenHtml +
            '<li class="nav-auth"><a href="instellingen.html" title="Instellingen">⚙</a></li>' +
            '<li class="nav-auth"><a href="#" onclick="uitloggen();return false;" style="color:var(--kleur-primair)">Uitloggen</a></li>';

        laadBerichtenBadge();
    } else {
        navLinks.innerHTML +=
            '<li class="nav-auth"><a href="inloggen.html">Inloggen</a></li>' +
            '<li class="nav-auth"><a href="registreren.html" style="background:var(--kleur-primair);color:#fff;padding:0.3rem 0.9rem;border-radius:6px;font-weight:600">Registreren</a></li>';
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
