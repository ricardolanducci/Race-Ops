// format.js — formatação de tempos (valores chegam em segundos do backend)

const Fmt = (() => {
  const pad = (n, w = 2) => String(Math.floor(n)).padStart(w, '0');

  /** 58.891 → "00:58.891" | 400 → "06:40.000" */
  function lap(seconds) {
    if (seconds == null || seconds <= 0) return '--';
    const m = Math.floor(seconds / 60);
    const s = seconds - m * 60;
    return `${pad(m)}:${s.toFixed(3).padStart(6, '0')}`;
  }

  /** Cronômetro grande, com décimos: "00:32.3" */
  function running(seconds) {
    if (seconds == null || seconds < 0) return '--';
    const m = Math.floor(seconds / 60);
    const s = seconds - m * 60;
    return `${pad(m)}:${s.toFixed(1).padStart(4, '0')}`;
  }

  /** 3307 → "00:55:07" */
  function clock(seconds) {
    if (seconds == null || seconds < 0) return '--:--:--';
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = Math.floor(seconds % 60);
    return `${pad(h)}:${pad(m)}:${pad(s)}`;
  }

  /** Delta entre voltas: "+00.295" / "-00.014" */
  function delta(seconds) {
    if (seconds == null) return '';
    const sign = seconds >= 0 ? '+' : '-';
    return `${sign}${Math.abs(seconds).toFixed(3).padStart(6, '0')}`;
  }

  /** 7 → "7º" */
  const pos = (n) => (n > 0 ? `${n}º` : '--');

  /** ISO/Date → "HH:MM:SS" local */
  function time(value) {
    if (!value) return '--';
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? '--' : d.toLocaleTimeString('pt-BR', { hour12: false });
  }

  const statusLabel = {
    OnTrack: 'Em pista',
    NearStop: 'Próx. parar',
    Attention: 'Atenção',
    Box: 'Box',
  };

  const esc = (value) => String(value ?? '')
    .replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');

  return { lap, running, clock, delta, pos, time, statusLabel, esc };
})();
