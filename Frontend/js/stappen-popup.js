// Zet een formulier om in een pop-up met stappen (zoals bij registreren).
// Het formulier bevat blokken <div class="wiz-stap" data-label="...">; de rij met de verstuurknop
// heeft class "wiz-slot". Id's en onsubmit van het formulier blijven gewoon werken.
//
//   const wizard = maakStappenPopup('aanbod-formulier', 'Nieuwe aanbieding');
//   wizard.open(); wizard.sluit(); wizard.isOpen();
function maakStappenPopup(inhoudId, titel) {
    const inhoud = document.getElementById(inhoudId);
    const form = inhoud.querySelector('form');
    const stappen = [...form.querySelectorAll(':scope > .wiz-stap')];
    const slot = form.querySelector(':scope > .wiz-slot');
    const verstuurKnop = form.querySelector('button[type="submit"]');

    const overlay = document.createElement('div');
    overlay.className = 'wiz-overlay';
    overlay.innerHTML = `
        <div class="wiz-kaart" role="dialog" aria-modal="true">
            <button type="button" class="wiz-sluiten" title="Sluiten">✕</button>
            <h3 class="wiz-titel"></h3>
            <div class="wiz-indicator"></div>
            <div class="wiz-body"></div>
            <div class="wiz-nav">
                <button type="button" class="btn btn-secundair wiz-vorige">← Vorige</button>
                <button type="button" class="btn btn-primair wiz-volgende">Volgende →</button>
            </div>
        </div>`;
    overlay.querySelector('.wiz-titel').textContent = titel;
    document.body.appendChild(overlay);
    overlay.querySelector('.wiz-body').appendChild(inhoud);
    inhoud.style.display = '';
    if (slot) slot.style.display = 'none';

    const vorige = overlay.querySelector('.wiz-vorige');
    const volgende = overlay.querySelector('.wiz-volgende');
    const verstuurTekst = verstuurKnop ? verstuurKnop.textContent.trim() : 'Opslaan';
    let huidig = 0;

    function tekenIndicator() {
        overlay.querySelector('.wiz-indicator').innerHTML = stappen.map((s, i) => `
            ${i > 0 ? `<div class="wiz-lijn${i <= huidig ? ' actief' : ''}"></div>` : ''}
            <div class="wiz-stap-bol${i === huidig ? ' actief' : ''}${i < huidig ? ' klaar' : ''}">
                <div class="wiz-bal">${i < huidig ? '✓' : i + 1}</div>
                <div class="wiz-label">${s.dataset.label || ''}</div>
            </div>`).join('');
    }

    function toon(i) {
        huidig = Math.max(0, Math.min(i, stappen.length - 1));
        stappen.forEach((s, n) => s.classList.toggle('actief', n === huidig));
        const laatste = huidig === stappen.length - 1;
        vorige.style.visibility = huidig === 0 ? 'hidden' : 'visible';
        volgende.textContent = laatste ? verstuurTekst : 'Volgende →';
        tekenIndicator();
        overlay.querySelector('.wiz-body').scrollTop = 0;
        const eerste = stappen[huidig].querySelector('input:not([type=hidden]):not([type=radio]):not([type=checkbox]), textarea');
        if (eerste && window.matchMedia('(min-width: 701px)').matches) setTimeout(() => eerste.focus(), 30);
    }

    // Alleen de velden van de huidige stap controleren (verplichte velden, min/max, ...)
    function stapIsGeldig() {
        const fout = [...stappen[huidig].querySelectorAll('input, textarea, select')]
            .find(el => el.offsetParent !== null && !el.checkValidity());
        if (fout) { fout.reportValidity(); return false; }
        return true;
    }

    function verder() {
        if (!stapIsGeldig()) return;
        if (huidig < stappen.length - 1) toon(huidig + 1);
        else form.requestSubmit(verstuurKnop || undefined);
    }

    function open() {
        const melding = inhoud.querySelector('[id^="melding"]');
        if (melding) melding.innerHTML = '';
        overlay.classList.add('open');
        document.body.style.overflow = 'hidden';
        toon(0);
    }

    function sluit() {
        overlay.classList.remove('open');
        document.body.style.overflow = '';
        // Succesmelding staat in de (nu gesloten) pop-up: laat hem even onderin het scherm zien
        const ok = inhoud.querySelector('.melding-succes');
        if (ok) { toonToast(ok.textContent); ok.remove(); }
    }

    vorige.addEventListener('click', () => toon(huidig - 1));
    volgende.addEventListener('click', verder);
    overlay.querySelector('.wiz-sluiten').addEventListener('click', sluit);
    overlay.addEventListener('mousedown', e => { if (e.target === overlay) sluit(); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape' && overlay.classList.contains('open')) sluit(); });

    // Enter in een invoerveld = volgende stap (niet meteen versturen)
    form.addEventListener('keydown', e => {
        if (e.key !== 'Enter' || e.defaultPrevented || e.target.tagName !== 'INPUT') return;
        if (huidig < stappen.length - 1) { e.preventDefault(); verder(); }
    });
    form.addEventListener('submit', e => {
        if (huidig < stappen.length - 1) { e.preventDefault(); e.stopImmediatePropagation(); verder(); }
    }, true);

    // Tijdens versturen ook de knop in de pop-up uitschakelen
    if (verstuurKnop) {
        new MutationObserver(() => {
            volgende.disabled = verstuurKnop.disabled;
            if (huidig === stappen.length - 1) volgende.textContent = verstuurKnop.disabled ? verstuurKnop.textContent : verstuurTekst;
        }).observe(verstuurKnop, { attributes: true, childList: true, characterData: true, subtree: true });
    }

    return { open, sluit, isOpen: () => overlay.classList.contains('open') };
}

function toonToast(tekst) {
    const t = document.createElement('div');
    t.className = 'toast-melding';
    t.textContent = tekst;
    document.body.appendChild(t);
    setTimeout(() => t.classList.add('weg'), 2600);
    setTimeout(() => t.remove(), 3200);
}
