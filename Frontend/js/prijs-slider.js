// Prijsfilter met twee schuifjes (minimum en maximum).
//   const slider = maakPrijsSlider('f-prijs-slider', laadAanbod);
//   slider.past(a.isGratis ? 0 : a.prijsPerRijder)  -> true/false
//   slider.reset()
function maakPrijsSlider(containerId, onChange, opties = {}) {
    const MAX = opties.max ?? 500;
    const STAP = opties.stap ?? 5;
    const el = document.getElementById(containerId);
    el.className = 'prijs-slider';
    el.innerHTML = `
        <div class="ps-waarden"><span class="ps-min"></span><span class="ps-max"></span></div>
        <div class="ps-baan">
            <div class="ps-vulling"></div>
            <input type="range" class="ps-invoer-min" min="0" max="${MAX}" step="${STAP}" value="0" aria-label="Minimumprijs">
            <input type="range" class="ps-invoer-max" min="0" max="${MAX}" step="${STAP}" value="${MAX}" aria-label="Maximumprijs">
        </div>`;
    const lo = el.querySelector('.ps-invoer-min');
    const hi = el.querySelector('.ps-invoer-max');
    let timer = null;

    function teken() {
        // Schuifjes kunnen elkaar niet passeren
        if (+lo.value > +hi.value) {
            if (document.activeElement === lo) lo.value = hi.value; else hi.value = lo.value;
        }
        const a = +lo.value, b = +hi.value;
        el.querySelector('.ps-min').textContent = '€' + a;
        el.querySelector('.ps-max').textContent = b >= MAX ? '€' + MAX + '+' : '€' + b;
        const vulling = el.querySelector('.ps-vulling');
        vulling.style.left = (a / MAX * 100) + '%';
        vulling.style.right = (100 - b / MAX * 100) + '%';
        // Het schuifje dat bovenop ligt moet pakbaar blijven als ze helemaal rechts samenkomen
        lo.style.zIndex = a >= MAX - STAP ? 5 : 3;
    }

    function gewijzigd() {
        teken();
        clearTimeout(timer);
        timer = setTimeout(() => onChange && onChange(), 250);
    }
    lo.addEventListener('input', gewijzigd);
    hi.addEventListener('input', gewijzigd);
    teken();

    return {
        // Geen beperking = alles past (ook aanbiedingen zonder prijs)
        actief: () => +lo.value > 0 || +hi.value < MAX,
        past(prijs) {
            if (!this.actief()) return true;
            if (prijs == null) return false;
            return prijs >= +lo.value && (+hi.value >= MAX || prijs <= +hi.value);
        },
        reset() { lo.value = 0; hi.value = MAX; teken(); }
    };
}
