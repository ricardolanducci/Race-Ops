// overview.js — Visão Geral: módulos Cronometro, Alertas e Classificação compacta

const Overview = (() => {
  const chronoContainer = document.getElementById('chrono-modules');
  const compactBody = document.getElementById('compact-body');
  const alertsBody = document.getElementById('alerts-body');

  // Modo do painel Relativo por kart (REAL | PISTA)
  const relMode = {};

  chronoContainer.addEventListener('click', (e) => {
    const btn = e.target.closest('[data-rel]');
    if (!btn) return;
    relMode[btn.dataset.kart] = btn.dataset.rel;
    App.rerender();
  });

  function render(view) {
    renderChronoModules(view);
    renderCompact(view);
  }

  function renderChronoModules(view) {
    if (!view.monitored.length) {
      chronoContainer.innerHTML = `
        <div class="module chrono"><div class="module-header"><span class="module-icon">◷</span> Cronometro</div>
        <div class="module-body"><p class="empty-state" style="padding:24px;font-size:12px">
          Nenhum kart monitorado.<br>Cadastre os karts da equipe em <strong>Configurações evento</strong>.
        </p></div></div>`;
      return;
    }

    chronoContainer.innerHTML = view.monitored.map((k) => chronoModule(k, view)).join('');
  }

  function chronoModule(k, view) {
    const mode = relMode[k.kart] || 'real';
    const ahead = mode === 'real' ? k.relative.aheadReal : k.relative.aheadTrack;
    const behind = mode === 'real' ? k.relative.behindReal : k.relative.behindTrack;

    const lapsRows = k.recentLaps.map((l) => `
      <tr class="${l.isBest ? 'best' : ''}">
        <td>${l.lap}</td>
        <td class="num">${Fmt.lap(l.seconds)}</td>
        <td class="num ${l.deltaSeconds == null ? '' : l.deltaSeconds >= 0 ? 'delta-pos' : 'delta-neg'}">${Fmt.delta(l.deltaSeconds)}</td>
        <td class="num">${l.rank ? Fmt.pos(l.rank) : '--'}</td>
      </tr>`).join('');

    const historyRows = [...k.stints].reverse().map((s) => `
      <tr class="${s.isCurrent ? 'current' : ''}">
        <td>${s.number}</td>
        <td>${Fmt.esc(s.driver) || '—'}</td>
        <td class="num" ${s.isCurrent ? `data-tick="clock" data-base="${s.trackTimeSeconds ?? 0}"` : ''}>${Fmt.clock(s.trackTimeSeconds)}</td>
        <td class="num">${s.stopTimeSeconds ? Fmt.lap(s.stopTimeSeconds) : '--'}</td>
      </tr>`).join('');

    return `
    <div class="module chrono" data-kart="${Fmt.esc(k.kart)}">
      <div class="module-header"><span class="module-icon">◷</span> Cronometro</div>
      <div class="chrono-id">
        <div class="cell"><span class="cell-label">Kart #</span><span class="cell-value">${Fmt.esc(k.kart)}</span></div>
        <div class="cell"><span class="cell-label">Posição (Real/Pista)</span><span class="cell-value">${Fmt.pos(k.real)} / ${Fmt.pos(k.pista)}</span></div>
        <div class="cell"><span class="cell-label">Volta atual</span><span class="cell-value">${k.currentLap || '--'}</span></div>
      </div>
      <div class="chrono-timer">
        <div class="timer-main">
          <div class="timer-head">
            <span class="cell-label">Cronometro</span>
            <span class="badge status-${k.status}">${Fmt.statusLabel[k.status] || k.status}</span>
          </div>
          <span class="timer-value" data-tick="running" data-base="${k.secondsSinceLastPass}">${Fmt.running(k.secondsSinceLastPass)}</span>
        </div>
        <div class="timer-side">
          <span class="cell-label">Tempo de pista</span>
          <span class="big" data-tick="clock" data-base="${k.trackTimeSeconds}">${Fmt.clock(k.trackTimeSeconds)}</span>
          <span class="est">${k.estStintSeconds ? `est: ${Fmt.clock(k.estStintSeconds)}` : ''}</span>
        </div>
      </div>
      <table class="data-table laps-table">
        <thead><tr><th>Volta</th><th class="num">Tempo</th><th class="num">Delta</th><th class="num">Rank</th></tr></thead>
        <tbody>${lapsRows || '<tr class="empty-row"><td colspan="4">Sem voltas</td></tr>'}</tbody>
      </table>
      <div class="chrono-stats">
        <div class="cell"><span class="cell-label">MV Stint Atual</span><span class="cell-value">${Fmt.lap(k.bestStintLapSeconds)}</span></div>
        <div class="cell"><span class="cell-label">Média 5V</span><span class="cell-value">${Fmt.lap(k.avg5Seconds)}</span></div>
        <div class="cell"><span class="cell-label">Média 5V Rank</span><span class="cell-value">${Fmt.pos(k.avg5Rank)}</span></div>
      </div>
      <div class="chrono-section-title">
        <span>Relativo</span>
        <span class="rel-toggle">
          <button data-rel="real" data-kart="${Fmt.esc(k.kart)}" class="${mode === 'real' ? 'active' : ''}">REAL</button>
          <button data-rel="track" data-kart="${Fmt.esc(k.kart)}" class="${mode === 'track' ? 'active' : ''}">PISTA</button>
        </span>
      </div>
      <div class="relative-grid">
        ${relCell('Frente', ahead)}
        <div class="rel-cell center">
          <span class="cell-label">Dif.</span>
          <span class="rel-time">◂ ${ahead?.gapSeconds != null ? '+' + Fmt.lap(ahead.gapSeconds) : '--'}</span>
          <span class="rel-time">${behind?.gapSeconds != null ? '-' + Fmt.lap(behind.gapSeconds) : '--'} ▸</span>
        </div>
        ${relCell('Atrás', behind)}
      </div>
      <div class="chrono-section-title"><span>Histórico</span></div>
      <table class="data-table history-table">
        <thead><tr><th>Stint</th><th>Piloto</th><th class="num">Tempo pista</th><th class="num">Parada</th></tr></thead>
        <tbody>${historyRows || '<tr class="empty-row"><td colspan="4">Sem stints</td></tr>'}</tbody>
      </table>
    </div>`;
  }

  function relCell(label, entry) {
    if (!entry) return `<div class="rel-cell"><span class="cell-label">${label}</span><span class="rel-time">--</span></div>`;
    return `
      <div class="rel-cell">
        <span class="cell-label">${label}</span>
        <span class="rel-time">${Fmt.lap(entry.lastLapSeconds)}</span>
        <span class="rel-sub">#${Fmt.esc(entry.kart)}<br>${Fmt.esc(entry.team)}</span>
      </div>`;
  }

  function renderCompact(view) {
    compactBody.innerHTML = view.standings.map((r) => `
      <tr class="${r.isMonitored ? 'monitored' : ''}">
        <td>${Fmt.pos(r.real)}</td>
        <td>${r.pista || '--'}</td>
        <td>${Fmt.esc(r.kart)}</td>
        <td>${Fmt.esc(r.team) || Fmt.esc(r.driver)}</td>
        <td class="num">${Fmt.lap(r.lastLapSeconds)}</td>
        <td class="num">${Fmt.pos(r.lastLapRank)}</td>
        <td class="num">${r.lap || '--'}</td>
        <td class="num" data-tick="clock" data-base="${r.trackTimeSeconds}">${Fmt.clock(r.trackTimeSeconds)}</td>
      </tr>`).join('') || '<tr class="empty-row"><td colspan="8">Aguardando dados...</td></tr>';
  }

  function renderAlerts(alerts) {
    alertsBody.innerHTML = alerts.map((a) => `
      <tr><td>${Fmt.esc(a.text)}</td><td class="num">${a.time}</td></tr>`).join('')
      || '<tr class="empty-row"><td colspan="2">Sem alertas</td></tr>';
  }

  return { render, renderAlerts };
})();
