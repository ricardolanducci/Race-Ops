// config.js — modal de Configurações do evento

const ConfigView = (() => {
  const modal = document.getElementById('config-modal');
  const form = document.getElementById('config-form');
  const rulesContainer = document.getElementById('cfg-rules');

  document.getElementById('open-config').addEventListener('click', open);
  document.getElementById('cfg-cancel').addEventListener('click', () => modal.close());
  document.getElementById('cfg-add-rule').addEventListener('click', () => addRuleRow());

  rulesContainer.addEventListener('click', (e) => {
    if (e.target.closest('.remove-rule')) e.target.closest('.rule-row').remove();
  });

  async function open() {
    const raceId = App.currentRaceId();
    if (!raceId) {
      alert('Selecione uma corrida primeiro.');
      return;
    }

    const config = await Api.getConfig(raceId);
    document.getElementById('cfg-duration').value = config.raceDurationMinutes;
    document.getElementById('cfg-karts').value = config.monitoredKarts.join(', ');
    document.getElementById('cfg-alert').value = config.trackTimeAlertMinutes;
    document.getElementById('cfg-attention').value = config.attentionLapSeconds;
    document.getElementById('cfg-box').value = config.boxLapSeconds;
    document.getElementById('cfg-heavy').checked = config.hasHeavyStintRule;
    document.getElementById('cfg-heavy-count').value = config.heavyStintRequiredCount;

    rulesContainer.innerHTML = '';
    config.pitRules.forEach(addRuleRow);

    modal.showModal();
  }

  function addRuleRow(rule) {
    const row = document.createElement('div');
    row.className = 'rule-row';
    row.innerHTML = `
      <input type="text" class="rule-name" placeholder="Nome (ex: 06m)" value="${Fmt.esc(rule?.name ?? '')}" />
      <input type="number" class="rule-min" placeholder="Tempo mín. (s)" min="0" value="${rule?.minStopSeconds ?? ''}" />
      <input type="number" class="rule-count" placeholder="Qtd. obrigatória" min="0" value="${rule?.requiredCount ?? ''}" />
      <button type="button" class="remove-rule" title="Remover regra">✕</button>`;
    rulesContainer.appendChild(row);
  }

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const raceId = App.currentRaceId();
    if (!raceId) return;

    const pitRules = [...rulesContainer.querySelectorAll('.rule-row')].map((row) => ({
      name: row.querySelector('.rule-name').value.trim(),
      minStopSeconds: Number(row.querySelector('.rule-min').value) || 0,
      requiredCount: Number(row.querySelector('.rule-count').value) || 0,
    })).filter((r) => r.name && r.minStopSeconds > 0);

    const config = {
      raceId,
      eventName: '',
      raceDurationMinutes: Number(document.getElementById('cfg-duration').value) || 0,
      pitRules,
      monitoredKarts: document.getElementById('cfg-karts').value
        .split(',').map((k) => k.trim()).filter(Boolean),
      hasHeavyStintRule: document.getElementById('cfg-heavy').checked,
      heavyStintRequiredCount: Number(document.getElementById('cfg-heavy-count').value) || 0,
      trackTimeAlertMinutes: Number(document.getElementById('cfg-alert').value) || 50,
      attentionLapSeconds: Number(document.getElementById('cfg-attention').value) || 120,
      boxLapSeconds: Number(document.getElementById('cfg-box').value) || 180,
    };

    await Api.saveConfig(raceId, config);
    modal.close();
    App.setConfig(config);
    App.refresh();
  });

  return { open };
})();
