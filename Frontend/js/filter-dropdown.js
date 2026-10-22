// Maakt van elke lijst met vinkjes/keuzerondjes in een filterpaneel (.filter-check-lijst) een uitklapmenu.
// De vinkjes zelf blijven dezelfde elementen, dus bestaande onchange-handlers en id's blijven werken.
(function () {
    function labelTekst(lijst) {
        const inputs = [...lijst.querySelectorAll('input')];
        const gekozen = inputs.filter(i => i.checked);
        const tekst = i => i.parentElement.textContent.trim();
        if (inputs.length && inputs[0].type === 'radio') return gekozen[0] ? tekst(gekozen[0]) : 'Alle';
        return gekozen.length === 0 ? 'Alle' : gekozen.length === 1 ? tekst(gekozen[0]) : gekozen.length + ' gekozen';
    }

    function maak() {
        document.querySelectorAll('.filter-check-lijst:not(.fd-klaar)').forEach(lijst => {
            lijst.classList.add('fd-klaar', 'filter-dropdown-paneel');
            const wrap = document.createElement('div');
            wrap.className = 'filter-dropdown';
            const knop = document.createElement('button');
            knop.type = 'button';
            knop.className = 'filter-dropdown-knop';
            knop.innerHTML = '<span class="fd-label"></span><span class="fd-pijl">▾</span>';
            lijst.parentNode.insertBefore(wrap, lijst);
            wrap.appendChild(knop);
            wrap.appendChild(lijst);

            const ververs = () => { wrap.querySelector('.fd-label').textContent = labelTekst(lijst); };
            wrap._ververs = ververs;
            ververs();
            knop.addEventListener('click', e => {
                e.stopPropagation();
                document.querySelectorAll('.filter-dropdown.open').forEach(d => { if (d !== wrap) d.classList.remove('open'); });
                wrap.classList.toggle('open');
                ververs();
            });
            lijst.addEventListener('click', e => e.stopPropagation());
            lijst.addEventListener('change', e => {
                ververs();
                if (e.target.type === 'radio') wrap.classList.remove('open'); // één keuze: meteen dicht
            });
        });
    }

    // Na een reset (vinkjes in code uitgezet, zonder change-event) de teksten bijwerken
    window.verversFilterDropdowns = () => document.querySelectorAll('.filter-dropdown').forEach(d => d._ververs && d._ververs());
    window.maakFilterDropdowns = maak;

    document.addEventListener('click', e => {
        document.querySelectorAll('.filter-dropdown.open').forEach(d => d.classList.remove('open'));
        if (e.target.closest('.filter-reset, [onclick*="reset" i]')) setTimeout(window.verversFilterDropdowns, 0);
    });
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape') document.querySelectorAll('.filter-dropdown.open').forEach(d => d.classList.remove('open'));
    });

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', maak);
    else maak();
})();
