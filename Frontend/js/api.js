// Pas dit aan als je backend op een ander adres draait
const API_BASE = 'http://localhost:5000/api';

async function apiVerzoek(pad, methode = 'GET', body = null) {
    const opties = {
        method: methode,
        headers: { 'Content-Type': 'application/json' },
    };
    if (body) opties.body = JSON.stringify(body);

    const antwoord = await fetch(API_BASE + pad, opties);

    if (!antwoord.ok) {
        const tekst = await antwoord.text();
        throw new Error(tekst || `Fout ${antwoord.status}`);
    }

    if (antwoord.status === 204) return null;
    return antwoord.json();
}

// ===== Coach API =====
const CoachAPI = {
    haalAlleOp: (filters = {}) => {
        const params = new URLSearchParams();
        Object.entries(filters).forEach(([k, v]) => { if (v !== '' && v !== null && v !== undefined) params.append(k, v); });
        const query = params.toString() ? '?' + params.toString() : '';
        return apiVerzoek('/coach' + query);
    },
    haalEenOp: (id) => apiVerzoek(`/coach/${id}`),
    voegToe: (coach) => apiVerzoek('/coach', 'POST', coach),
    pasAan: (id, coach) => apiVerzoek(`/coach/${id}`, 'PUT', coach),
    verwijder: (id) => apiVerzoek(`/coach/${id}`, 'DELETE'),
};

// ===== Rijder API =====
const RijderAPI = {
    haalAlleOp: () => apiVerzoek('/rijder'),
    haalEenOp: (id) => apiVerzoek(`/rijder/${id}`),
    voegToe: (rijder) => apiVerzoek('/rijder', 'POST', rijder),
    pasAan: (id, rijder) => apiVerzoek(`/rijder/${id}`, 'PUT', rijder),
    verwijder: (id) => apiVerzoek(`/rijder/${id}`, 'DELETE'),
};
