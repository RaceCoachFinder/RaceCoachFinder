// Gedeelde logica voor de prijspakketten modal
let huidigePakketten = [];

function openModal() {
    renderPakketLijst();
    document.getElementById('modal-overlay').classList.add('open');
}

function sluitModal() {
    document.getElementById('modal-overlay').classList.remove('open');
}

function sluitModalBuiten(event) {
    if (event.target === document.getElementById('modal-overlay')) sluitModal();
}

function voegPakketToe() {
    const labelEl = document.getElementById('nieuw-pakket-label');
    const prijsEl = document.getElementById('nieuw-pakket-prijs');
    const label = (labelEl?.value || '').trim();
    const prijs = parseFloat(prijsEl?.value) || 0;
    if (!label) { labelEl?.focus(); return; }
    huidigePakketten.push({ label, prijs });
    renderPakketLijst();
    document.getElementById('nieuw-pakket-label')?.focus();
}

function verwijderPakket(index) {
    huidigePakketten.splice(index, 1);
    renderPakketLijst();
}

function renderPakketLijst() {
    const lijst = document.getElementById('pakket-lijst');

    const bevestigdHtml = huidigePakketten.map((p, i) => `
        <div class="pakket-bevestigd">
            <span class="pakket-bevestigd-label">${escapeHtml(p.label)}</span>
            <span class="pakket-bevestigd-prijs">€${p.prijs}</span>
            <button type="button" class="pakket-verwijder" onclick="verwijderPakket(${i})" title="Verwijder">&#x2715;</button>
        </div>
    `).join('');

    lijst.innerHTML = bevestigdHtml + `
        <div class="pakket-rij">
            <input type="text" class="pakket-label" id="nieuw-pakket-label" placeholder="bijv. Racedag coachen"
                onkeydown="if(event.key==='Enter'){event.preventDefault();voegPakketToe();}">
            <input type="number" id="nieuw-pakket-prijs" placeholder="€ prijs" min="0" step="5"
                onkeydown="if(event.key==='Enter'){event.preventDefault();voegPakketToe();}">
            <button type="button" class="pakket-toevoegen" onclick="voegPakketToe()" title="Toevoegen">&#x2713;</button>
        </div>
    `;
}

function slaanPrijzenOp() {
    // Voeg eventueel ingevuld maar niet bevestigd pakket ook toe
    const labelEl = document.getElementById('nieuw-pakket-label');
    const prijsEl = document.getElementById('nieuw-pakket-prijs');
    const label = (labelEl?.value || '').trim();
    if (label) {
        huidigePakketten.push({ label, prijs: parseFloat(prijsEl?.value) || 0 });
    }
    sluitModal();
    updatePrijzenPreview();
}

function updatePrijzenPreview() {
    const preview = document.getElementById('prijzen-preview');
    if (!preview) return;
    if (huidigePakketten.length === 0) {
        preview.innerHTML = '';
        return;
    }
    preview.innerHTML = huidigePakketten.map(p =>
        `<div class="prijs-item">
            <span>${escapeHtml(p.label)}</span>
            <span class="prijs-item-prijs">€${p.prijs}</span>
        </div>`
    ).join('');
}

function laadPakketten(jsonString) {
    try {
        huidigePakketten = jsonString ? JSON.parse(jsonString) : [];
    } catch {
        huidigePakketten = [];
    }
    updatePrijzenPreview();
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = String(str);
    return div.innerHTML;
}

function escapeAttr(str) {
    return String(str).replace(/"/g, '&quot;');
}
