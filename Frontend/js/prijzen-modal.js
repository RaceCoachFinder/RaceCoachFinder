// Gedeelde logica voor de prijspakketten modal
let huidigePakketten = [];

function openModal() {
    renderPakketLijst();
    document.getElementById('modal-overlay').classList.add('open');
}

function sluitModal() {
    document.getElementById('modal-overlay').classList.remove('open');
}

// Sluit modal als je buiten het witte vlak klikt
function sluitModalBuiten(event) {
    if (event.target === document.getElementById('modal-overlay')) sluitModal();
}

function voegPakketToe() {
    huidigePakketten.push({ label: '', prijs: 0 });
    renderPakketLijst();
    // Focus op het nieuwe lege label veld
    const inputs = document.querySelectorAll('.pakket-label');
    inputs[inputs.length - 1].focus();
}

function verwijderPakket(index) {
    huidigePakketten.splice(index, 1);
    renderPakketLijst();
}

function renderPakketLijst() {
    const lijst = document.getElementById('pakket-lijst');
    if (huidigePakketten.length === 0) {
        lijst.innerHTML = `<p style="color:var(--kleur-subtekst);font-size:0.88rem;margin-bottom:0.75rem">Nog geen pakketten. Klik op "+ Pakket toevoegen".</p>`;
        return;
    }
    lijst.innerHTML = huidigePakketten.map((p, i) => `
        <div class="pakket-rij">
            <input type="text" class="pakket-label" placeholder="bijv. Racedag coachen"
                value="${escapeAttr(p.label)}"
                oninput="huidigePakketten[${i}].label = this.value">
            <input type="number" placeholder="€ prijs" min="0" step="5"
                value="${p.prijs || ''}"
                oninput="huidigePakketten[${i}].prijs = parseFloat(this.value) || 0">
            <button type="button" class="pakket-verwijder" onclick="verwijderPakket(${i})" title="Verwijder">&#x2715;</button>
        </div>
    `).join('');
}

function slaanPrijzenOp() {
    // Verwijder lege rijen
    huidigePakketten = huidigePakketten.filter(p => p.label.trim() !== '');
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
