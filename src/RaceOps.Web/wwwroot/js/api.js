// api.js — wrapper para chamadas à API REST do backend

const Api = (() => {
  async function request(path, options) {
    const res = await fetch(`/api/${path}`, options);
    if (!res.ok) throw new Error(`Erro ${res.status}: ${path}`);
    if (res.status === 204) return null;
    return res.json();
  }

  const json = (method, body) => ({
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  return {
    getCurrentRaces: () => request('races/current'),
    getLiveView: (raceId) => request(`races/${raceId}/live`),
    getConfig: (raceId) => request(`races/${raceId}/config`),
    saveConfig: (raceId, config) => request(`races/${raceId}/config`, json('PUT', config)),
    saveStint: (raceId, kart, stint) => request(`races/${raceId}/stints/${encodeURIComponent(kart)}`, json('PUT', stint)),
  };
})();
