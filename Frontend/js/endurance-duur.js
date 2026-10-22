// Duur van een endurance-race. Bij elke keuze Sprint/Endurance (radio met value "Endurance") komt
// automatisch een keuzelijst "Hoe lang duurt de race?" die alleen zichtbaar is bij Endurance.
// De duur wordt opgeslagen als kenmerk "Duur: 6 uur" in de bestaande lijst (categorieën/kwaliteiten).
(function () {
    const OPTIES = ['1 uur', '2 uur', '3 uur', '4 uur', '6 uur', '8 uur', '12 uur', '24 uur'];

    function maak() {
        document.querySelectorAll('input[type="radio"][value="Endurance"]').forEach(radio => {
            const naam = radio.name;
            if (!naam || document.querySelector(`.duur-veld[data-groep="${naam}"]`)) return;
            const rij = radio.closest('label').parentElement;
            const veld = document.createElement('div');
            veld.className = 'duur-veld';
            veld.dataset.groep = naam;
            veld.style.display = 'none';
            veld.innerHTML = `
                <label style="display:block;font-size:0.88rem;font-weight:600;margin-bottom:0.3rem">Hoe lang duurt de race?</label>
                <div style="display:flex;gap:0.5rem;flex-wrap:wrap">
                    <select class="duur-keuze" style="padding:0.5rem 0.6rem;border:1.5px solid #e0e0e0;border-radius:8px;font-family:inherit;font-size:0.9rem;background:#fff">
                        <option value="">Kies de duur…</option>
                        ${OPTIES.map(o => `<option>${o}</option>`).join('')}
                        <option value="anders">Anders…</option>
                    </select>
                    <input type="text" class="duur-anders" placeholder="bijv. 90 minuten" maxlength="30" style="display:none;flex:1;min-width:140px;margin:0">
                </div>`;
            rij.insertAdjacentElement('afterend', veld);
            veld.style.marginTop = '0.75rem';
            const keuze = veld.querySelector('.duur-keuze');
            const anders = veld.querySelector('.duur-anders');
            keuze.addEventListener('change', () => { anders.style.display = keuze.value === 'anders' ? '' : 'none'; });
            const ververs = () => {
                const endurance = document.querySelector(`input[name="${naam}"]:checked`)?.value === 'Endurance';
                veld.style.display = endurance ? '' : 'none';
            };
            document.querySelectorAll(`input[name="${naam}"]`).forEach(r => r.addEventListener('change', ververs));
            veld._ververs = ververs;
            ververs();
        });
    }

    // "Duur: 6 uur" voor de groep met deze radionaam, of '' (geen endurance / niets gekozen)
    window.leesDuur = function (naam) {
        if (document.querySelector(`input[name="${naam}"]:checked`)?.value !== 'Endurance') return '';
        const veld = document.querySelector(`.duur-veld[data-groep="${naam}"]`);
        if (!veld) return '';
        const keuze = veld.querySelector('.duur-keuze').value;
        const tekst = (keuze === 'anders' ? veld.querySelector('.duur-anders').value : keuze).replace(/,/g, ' ').trim();
        return tekst ? 'Duur: ' + tekst : '';
    };

    window.resetDuur = function () {
        document.querySelectorAll('.duur-veld').forEach(v => {
            v.querySelector('.duur-keuze').value = '';
            v.querySelector('.duur-anders').value = '';
            v.querySelector('.duur-anders').style.display = 'none';
            if (v._ververs) v._ververs();
        });
    };

    // Kenmerk netjes tonen: "Duur: 6 uur" -> "⏱️ 6 uur"
    window.isDuur = t => /^Duur:/.test((t || '').trim());
    window.toonKenmerk = t => isDuur(t) ? '⏱️ ' + t.trim().replace(/^Duur:\s*/, '') : t;
    window.duurUit = lijst => {
        const d = (lijst || '').split(',').map(x => x.trim()).find(isDuur);
        return d ? d.replace(/^Duur:\s*/, '') : '';
    };

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', maak);
    else maak();
})();
