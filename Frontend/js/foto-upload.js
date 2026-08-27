const CROP_WRAP    = 320;
const CROP_CIRKEL  = 240;
const CROP_UITVOER = 400;

function toonCropModal(bestand) {
    return new Promise((resolve) => {

        // --- Bouw modal ---
        const overlay = document.createElement('div');
        overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.82);z-index:9999;display:flex;align-items:center;justify-content:center';

        const marge = (CROP_WRAP - CROP_CIRKEL) / 2;
        const r     = CROP_CIRKEL / 2;

        overlay.innerHTML = `
<div style="background:#18182a;border-radius:14px;padding:1.5rem 1.5rem 1.25rem;width:${CROP_WRAP + 48}px;max-width:95vw;color:#fff;box-shadow:0 24px 60px rgba(0,0,0,.5)">
  <p style="margin:0 0 .9rem;font-weight:700;font-size:1.05rem">Foto bijsnijden</p>
  <div id="kcf-wrap" style="width:${CROP_WRAP}px;height:${CROP_WRAP}px;background:#111;position:relative;overflow:hidden;border-radius:8px;cursor:grab;touch-action:none;margin:0 auto;user-select:none">
    <img id="kcf-img" style="position:absolute;transform-origin:0 0;pointer-events:none;user-select:none;display:block;max-width:none">
    <div style="position:absolute;inset:0;pointer-events:none;z-index:2;background:radial-gradient(circle ${r}px at 50% 50%,transparent ${r - 1}px,rgba(0,0,0,.65) ${r}px)"></div>
    <div style="position:absolute;left:${marge}px;top:${marge}px;width:${CROP_CIRKEL}px;height:${CROP_CIRKEL}px;border-radius:50%;border:2px solid rgba(255,255,255,.8);pointer-events:none;z-index:3"></div>
  </div>
  <div style="display:flex;align-items:center;gap:.75rem;margin:.9rem 0 0">
    <span style="font-size:.78rem;color:#aaa;flex-shrink:0">Zoom</span>
    <input id="kcf-zoom" type="range" style="flex:1;accent-color:#b8a000" min="0.1" max="4" step="0.005" value="1">
  </div>
  <p style="font-size:.75rem;color:#666;margin:.4rem 0 1rem;text-align:center">Sleep om te verschuiven &nbsp;·&nbsp; Scroll of pinch om te zoomen</p>
  <div style="display:flex;gap:.75rem">
    <button id="kcf-annuleer" style="flex:1;padding:.65rem;border:1px solid #444;background:transparent;color:#fff;border-radius:8px;cursor:pointer;font-size:.93rem">Annuleren</button>
    <button id="kcf-opslaan" style="flex:1;padding:.65rem;background:#b8a000;color:#fff;border:none;border-radius:8px;cursor:pointer;font-size:.93rem;font-weight:700">Opslaan</button>
  </div>
</div>`;

        document.body.appendChild(overlay);

        const wrap = document.getElementById('kcf-wrap');
        const img  = document.getElementById('kcf-img');
        const zoom = document.getElementById('kcf-zoom');

        let posX = 0, posY = 0, schaal = 1;
        let sleepAan = false, sleepX = 0, sleepY = 0;
        let pinchDist = null;

        function refresh() {
            img.style.left      = posX + 'px';
            img.style.top       = posY + 'px';
            img.style.transform = `scale(${schaal})`;
        }

        function zoomNaar(nieuw, fxX, fxY) {
            nieuw  = Math.max(parseFloat(zoom.min), Math.min(parseFloat(zoom.max), nieuw));
            posX   = fxX - (fxX - posX) * nieuw / schaal;
            posY   = fxY - (fxY - posY) * nieuw / schaal;
            schaal = nieuw;
            zoom.value = nieuw;
            refresh();
        }

        // --- Registreer onload VOOR src ---
        img.onload = () => {
            const vul  = Math.max(CROP_CIRKEL / img.naturalWidth, CROP_CIRKEL / img.naturalHeight);
            schaal     = vul;
            zoom.min   = (vul * 0.5).toFixed(4);
            zoom.max   = (vul * 6).toFixed(4);
            zoom.value = vul;
            posX = (CROP_WRAP - img.naturalWidth  * schaal) / 2;
            posY = (CROP_WRAP - img.naturalHeight * schaal) / 2;
            refresh();
        };

        img.onerror = () => {
            overlay.remove();
            resolve(null);
            alert('Kan afbeelding niet laden. Probeer een ander bestand.');
        };

        // --- Laad de afbeelding via FileReader ---
        const reader = new FileReader();
        reader.onload = (e) => { img.src = e.target.result; };
        reader.onerror = () => { overlay.remove(); resolve(null); };
        reader.readAsDataURL(bestand);

        // --- Muis slepen ---
        wrap.addEventListener('mousedown', (e) => {
            sleepAan = true;
            sleepX   = e.clientX - posX;
            sleepY   = e.clientY - posY;
            wrap.style.cursor = 'grabbing';
            e.preventDefault();
        });
        window.addEventListener('mousemove', (e) => {
            if (!sleepAan) return;
            posX = e.clientX - sleepX;
            posY = e.clientY - sleepY;
            refresh();
        });
        window.addEventListener('mouseup', () => { sleepAan = false; wrap.style.cursor = 'grab'; });

        // --- Scrollwiel zoomen ---
        wrap.addEventListener('wheel', (e) => {
            e.preventDefault();
            const rect = wrap.getBoundingClientRect();
            zoomNaar(schaal * (e.deltaY < 0 ? 1.1 : 0.91), e.clientX - rect.left, e.clientY - rect.top);
        }, { passive: false });

        // --- Touch: slepen + pinch ---
        wrap.addEventListener('touchstart', (e) => {
            if (e.touches.length === 1) {
                sleepAan  = true;
                sleepX    = e.touches[0].clientX - posX;
                sleepY    = e.touches[0].clientY - posY;
                pinchDist = null;
            } else if (e.touches.length === 2) {
                sleepAan  = false;
                pinchDist = Math.hypot(e.touches[0].clientX - e.touches[1].clientX, e.touches[0].clientY - e.touches[1].clientY);
            }
        }, { passive: true });

        wrap.addEventListener('touchmove', (e) => {
            e.preventDefault();
            if (e.touches.length === 1 && sleepAan) {
                posX = e.touches[0].clientX - sleepX;
                posY = e.touches[0].clientY - sleepY;
                refresh();
            } else if (e.touches.length === 2 && pinchDist) {
                const nd   = Math.hypot(e.touches[0].clientX - e.touches[1].clientX, e.touches[0].clientY - e.touches[1].clientY);
                const rect = wrap.getBoundingClientRect();
                const fxX  = ((e.touches[0].clientX + e.touches[1].clientX) / 2) - rect.left;
                const fxY  = ((e.touches[0].clientY + e.touches[1].clientY) / 2) - rect.top;
                zoomNaar(schaal * nd / pinchDist, fxX, fxY);
                pinchDist = nd;
            }
        }, { passive: false });

        wrap.addEventListener('touchend', () => { sleepAan = false; pinchDist = null; });

        // --- Zoomschuiver ---
        zoom.addEventListener('input', () => {
            zoomNaar(parseFloat(zoom.value), CROP_WRAP / 2, CROP_WRAP / 2);
        });

        // --- Knoppen ---
        document.getElementById('kcf-annuleer').onclick = () => { overlay.remove(); resolve(null); };

        document.getElementById('kcf-opslaan').onclick = () => {
            const canvas = document.createElement('canvas');
            canvas.width  = CROP_UITVOER;
            canvas.height = CROP_UITVOER;
            const ctx = canvas.getContext('2d');
            const srcX = (CROP_WRAP / 2 - r - posX) / schaal;
            const srcY = (CROP_WRAP / 2 - r - posY) / schaal;
            const srcW = CROP_CIRKEL / schaal;
            ctx.drawImage(img, srcX, srcY, srcW, srcW, 0, 0, CROP_UITVOER, CROP_UITVOER);
            canvas.toBlob((blob) => { overlay.remove(); resolve(blob); }, 'image/jpeg', 0.88);
        };
    });
}

async function uploadFoto(input) {
    const bestand = input.files[0];
    input.value = '';
    if (!bestand) return;

    const meldingEl = document.getElementById('foto-melding');
    meldingEl.textContent = '';

    const blob = await toonCropModal(bestand);
    if (!blob) return;

    meldingEl.textContent = 'Uploaden...';
    meldingEl.style.color = 'var(--kleur-subtekst)';

    try {
        const formData = new FormData();
        formData.append('bestand', blob, 'profielfoto.jpg');

        const antwoord = await fetch('https://racecoachfinder-production.up.railway.app/api/upload/profielfoto', {
            method: 'POST',
            headers: authHeader(),
            body: formData
        });

        if (!antwoord.ok) {
            const tekst = await antwoord.text();
            throw new Error(tekst || `Fout ${antwoord.status}`);
        }

        const data = await antwoord.json();
        document.getElementById('fotoUrl').value = data.url;
        toonFotoCirkel(data.url, document.getElementById('naam')?.value || '');

        meldingEl.textContent = 'Foto opgeslagen';
        meldingEl.style.color = '#2e7d32';
    } catch (e) {
        meldingEl.textContent = e.message;
        meldingEl.style.color = '#c62828';
        console.error('Upload fout:', e);
    }
}
