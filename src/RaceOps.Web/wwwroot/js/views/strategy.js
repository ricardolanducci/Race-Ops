// strategy.js — tela Estratégia: gerenciador de stints por kart monitorado

const Strategy = (() => {
  const tabs = document.getElementById('strategy-tabs');
  const summary = document.getElementById('strategy-summary');
  const body = document.getElementById('stints-body');
  const addBtn = document.getElementById('add-stint');

  let selectedKart = null;

  tabs.addEventListener('click', (e) => {
    const btn = e.target.closest('[data-kart]');
    if (!btn) return;
    selectedKart = btn.dataset.kart;
    App.rerender();
  });

  addBtn.addEventListener('click', async () => {
    const view = App.currentView();
    const kart = currentKart(view);
    if (!kart) return;
    const nextNumber = Math.max(0, ...kart.stints.map((s) => s.number)) + 1;
    await Api.saveStint(view.raceId, kart.kart, emptyStint(nextNumber));
    App.refresh();
  });

  body.addEventListener('change', async (e) => {
    const input = e.target.closest('[data-field]');
    if (!input) return;

    const view = App.currentView();
    const kart = currentKart(view);
    const stint = kart?.stints.find((s) => s.number === Number(input.dataset.number));
    if (!stint) return;

    const payload = { ...emptyStint(stint.number), id: stint.id };
    const field = input.dataset.field;

    if (field === 'driver') payload.driver = input.value.trim();
    if (field === 'heavy') payload.heavy = input.checked;
    if (field === 'exitLap') payload.exitLap = input.value ? Number(input.value) : null;
    if (field === 'stopTime') payload.stopTimeSeconds = parseLapInput(input.value);

    // Preserva edições manuais anteriores nos demais campos
    if (stint.isManual) {
      payload.driver = field === 'driver' ? payload.driver : stint.driver;
      payload.heavy = field === 'heavy' ? payload.heavy : stint.heavy;
    }

    try {
      await Api.saveStint(view.raceId, kart.kart, payload);
    } catch (err) {
      console.error('Erro ao salvar stint:', err);
    }
    App.refresh();
  });

  /** "06:03.100" | "6:03" | "363.1" → segundos */
  function parseLapInput(value) {
    if (!value || !value.trim()) return null;
    const parts = value.trim().split(':').map(Number);
    if (parts.some(Number.isNaN)) return null;
    if (parts.length === 3) return parts[0] * 3600 + parts[1] * 60 + parts[2];
    if (parts.length === 2) return parts[0] * 60 + parts[1];
    return parts[0];
  }

  function emptyStint(number) {
    return {
      id: 0, number, driver: '', entryTime: null, entryLap: null,
      exitTime: null, exitLap: null, stopTimeSeconds: null,
      heavy: false, isManual: true, trackTimeSeconds: null,
      estimatedSeconds: null, isCurrent: false,
    };
  }

  function currentKart(view) {
    if (!view?.monitored.length) return null;
    return view.monitored.find((k) => k.kart === selectedKart) || view.monitored[0];
  }

  function render(view) {
    const kart = currentKart(view);

    tabs.innerHTML = view.monitored.map((k) => `
      <button data-kart="${Fmt.esc(k.kart)}" class="${k === kart ? 'active' : ''}">#${Fmt.esc(k.kart)}</button>`).join('')
      || '<span style="padding:8px;color:var(--text-muted);font-size:12px">Nenhum kart monitorado — cadastre em Configurações evento.</span>';

    if (!kart) {
      summary.innerHTML = '';
      body.innerHTML = '<tr class="empty-row"><td colspan="10">Sem dados</td></tr>';
      return;
    }
    selectedKart = kart.kart;

    renderSummary(kart);

    // Não re-renderiza a tabela enquanto o usuário edita uma célula
    if (body.contains(document.activeElement)) return;

    body.innerHTML = kart.stints.map((s) => `
      <tr class="${s.isCurrent ? 'current' : ''}">
        <td class="stint-n">${s.number}</td>
        <td><input type="text" data-field="driver" data-number="${s.number}" value="${Fmt.esc(s.driver)}" placeholder="Selecione" /></td>
        <td class="num">${Fmt.time(s.entryTime)}</td>
        <td class="num">${s.entryLap ?? '--'}</td>
        <td class="num">${Fmt.time(s.exitTime)}</td>
        <td class="num"><input type="number" data-field="exitLap" data-number="${s.number}" value="${s.exitLap ?? ''}" placeholder="--" style="width:64px" /></td>
        <td class="num"><input type="text" data-field="stopTime" data-number="${s.number}" value="${s.stopTimeSeconds ? Fmt.lap(s.stopTimeSeconds) : ''}" placeholder="--" style="width:84px" /></td>
        <td style="text-align:center"><input type="checkbox" data-field="heavy" data-number="${s.number}" ${s.heavy ? 'checked' : ''} /></td>
        <td class="num" ${s.isCurrent ? `data-tick="clock" data-base="${s.trackTimeSeconds ?? 0}"` : ''}>${s.trackTimeSeconds != null ? Fmt.clock(s.trackTimeSeconds) : '--'}</td>
        <td class="num">${s.estimatedSeconds ? Fmt.clock(s.estimatedSeconds) : '--'}</td>
      </tr>`).join('') || '<tr class="empty-row"><td colspan="10">Sem stints</td></tr>';
  }

  function renderSummary(kart) {
    const config = App.currentConfig();
    const rules = config?.pitRules || [];

    const ruleItems = rules.map((rule, i) => {
      const done = kart.stops[i] ?? 0;
      const ok = done >= rule.requiredCount;
      return `
        <div class="summary-item">
          <span class="cell-label">Paradas (${Fmt.esc(rule.name)})</span>
          <span class="cell-value ${ok ? 'ok' : ''}">${ok ? '✓ ' : ''}${done} / ${rule.requiredCount}</span>
        </div>`;
    }).join('');

    const heavyItem = config?.hasHeavyStintRule ? (() => {
      const done = kart.stints.filter((s) => s.heavy).length;
      const ok = done >= config.heavyStintRequiredCount;
      return `
        <div class="summary-item">
          <span class="cell-label">Pesada</span>
          <span class="cell-value ${ok ? 'ok' : ''}">${ok ? '✓ ' : ''}${done} / ${config.heavyStintRequiredCount}</span>
        </div>`;
    })() : '';

    summary.innerHTML = `
      ${ruleItems}
      ${heavyItem}
      <div class="summary-item">
        <span class="cell-label">Stint Estimado</span>
        <span class="cell-value">${kart.estStintSeconds ? Fmt.clock(kart.estStintSeconds) : '--'}</span>
      </div>`;
  }

  return { render };
})();
