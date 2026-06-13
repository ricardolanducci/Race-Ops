// app.js — inicialização, seleção de corrida, roteamento e tick de timers

const App = (() => {
  const raceSelector = document.getElementById('race-selector');
  const emptyState = document.getElementById('empty-state');
  const raceDataBar = document.getElementById('race-data-bar');
  const streamBadge = document.getElementById('stream-badge');

  let raceId = null;
  let view = null;          // último LiveViewDto recebido
  let viewReceivedAt = 0;   // Date.now() do recebimento (para tick local)
  let config = null;
  let activeScreen = 'overview';
  let alerts = [];
  let lastStatuses = {};

  // --- Corridas disponíveis ---
  async function loadRaces() {
    try {
      const races = await Api.getCurrentRaces();
      raceSelector.innerHTML = '<option value="">Selecione uma corrida...</option>';
      for (const race of races) {
        const opt = document.createElement('option');
        opt.value = race.id;
        opt.textContent = `${race.name} — ${race.track}${race.isLive ? ' 🔴' : ''}`;
        raceSelector.appendChild(opt);
      }
    } catch (e) {
      raceSelector.innerHTML = '<option value="">Erro ao carregar corridas</option>';
      console.error('Erro ao carregar corridas:', e);
    }
  }

  raceSelector.addEventListener('change', async () => {
    raceId = parseInt(raceSelector.value, 10) || null;
    view = null;
    alerts = [];
    lastStatuses = {};
    if (!raceId) return;

    emptyState.innerHTML = '<h2>Carregando dados da corrida...</h2><p>Baixando sessão e histórico de voltas — pode levar alguns segundos.</p>';
    emptyState.hidden = false;

    config = await Api.getConfig(raceId).catch(() => null);
    await Hub.connect(raceId);
    await waitForLiveView();
  });

  /** Aguarda o seed inicial do backend (endpoint retorna 204 até concluir). */
  async function waitForLiveView() {
    const target = raceId;
    for (let attempt = 0; attempt < 60 && raceId === target; attempt++) {
      try {
        const v = await Api.getLiveView(target);
        if (v) { setView(v); return; }
      } catch (e) {
        console.error('Erro ao carregar live view:', e);
      }
      await new Promise((r) => setTimeout(r, 2000));
    }
  }

  // --- Atualizações em tempo real ---
  Hub.on('onLiveViewUpdated', (v) => {
    if (v.raceId === raceId) setView(v);
  });

  function setView(v) {
    view = v;
    viewReceivedAt = Date.now();
    deriveAlerts(v);
    rerender();
  }

  function rerender() {
    if (!view) return;
    emptyState.hidden = true;
    raceDataBar.hidden = false;

    streamBadge.textContent = view.isStreaming ? '● STREAM' : '● POLLING';
    streamBadge.className = `stream-badge ${view.isStreaming ? 'live' : 'poll'}`;

    renderRaceDataBar();

    document.getElementById('view-overview').hidden = activeScreen !== 'overview';
    document.getElementById('view-classification').hidden = activeScreen !== 'classification';
    document.getElementById('view-strategy').hidden = activeScreen !== 'strategy';

    if (activeScreen === 'overview') {
      Overview.render(view);
      Overview.renderAlerts(alerts);
    } else if (activeScreen === 'classification') {
      Classification.render(view);
    } else if (activeScreen === 'strategy') {
      Strategy.render(view);
    }
  }

  function renderRaceDataBar() {
    document.getElementById('rdb-clock').textContent =
      view.currentTime || new Date().toLocaleTimeString('pt-BR', { hour12: false });
    document.getElementById('rdb-elapsed').textContent = view.sessionTime || '--:--:--';
    document.getElementById('rdb-togo').textContent = view.timeToGo || '--:--:--';
    document.getElementById('rdb-avg').textContent = Fmt.lap(view.raceData.avgTrackLapSeconds);
    document.getElementById('rdb-avg10').textContent = Fmt.lap(view.raceData.avgTop10LapSeconds);

    const flag = document.getElementById('rdb-flag');
    const f = (view.flagStatus || '').toLowerCase();
    flag.textContent = f && f !== 'unknown' ? view.flagStatus.toUpperCase() : '—';
    flag.className = `rdb-value flag ${f}`;
  }

  // --- Alertas derivados (mudança de status dos karts monitorados) ---
  function deriveAlerts(v) {
    for (const k of v.monitored) {
      const prev = lastStatuses[k.kart];
      if (prev && prev !== k.status) {
        const label = Fmt.statusLabel[k.status] || k.status;
        alerts.unshift({
          text: `Kart #${k.kart} — ${label}`,
          time: new Date().toLocaleTimeString('pt-BR', { hour12: false }),
        });
      }
      lastStatuses[k.kart] = k.status;
    }
    alerts = alerts.slice(0, 30);
  }

  // --- Navegação lateral ---
  document.querySelectorAll('.side-item[data-view]').forEach((btn) => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.side-item[data-view]').forEach((b) => b.classList.remove('active'));
      btn.classList.add('active');
      activeScreen = btn.dataset.view;
      rerender();
    });
  });

  // --- Tick local: avança cronômetros entre atualizações do servidor ---
  setInterval(() => {
    if (!view) return;
    const elapsed = (Date.now() - viewReceivedAt) / 1000;
    document.querySelectorAll('[data-tick]').forEach((el) => {
      const base = parseFloat(el.dataset.base);
      if (Number.isNaN(base)) return;
      el.textContent = el.dataset.tick === 'running'
        ? Fmt.running(base + elapsed)
        : Fmt.clock(base + elapsed);
    });
  }, 100);

  async function refresh() {
    if (!raceId) return;
    try {
      const v = await Api.getLiveView(raceId);
      if (v) setView(v);
    } catch (e) {
      console.error('Erro ao atualizar:', e);
    }
  }

  loadRaces();

  return {
    rerender,
    refresh,
    currentView: () => view,
    currentRaceId: () => raceId,
    currentConfig: () => config,
    setConfig: (c) => { config = c; },
  };
})();
