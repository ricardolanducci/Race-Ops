// classification.js — tela Classificação com todas as colunas calculadas

const Classification = (() => {
  const head = document.getElementById('classification-head');
  const body = document.getElementById('classification-body');

  function render(view) {
    const ruleCols = view.stopRuleLabels.map((l) => `<th class="num">P(${Fmt.esc(l)})</th>`).join('');
    head.innerHTML = `
      <tr>
        <th>Real</th><th>Pista</th><th>Kart</th><th>Equipe</th><th>Piloto</th>
        <th class="num">Diferença</th><th class="num">TUV</th><th class="num">R-UV</th>
        <th class="num">Volta</th><th class="num">Méd. 5V</th><th class="num">T.Pista</th>
        <th class="num">Méd Stint</th><th class="num">R-St</th>${ruleCols}<th>Status</th>
      </tr>`;

    body.innerHTML = view.standings.map((r) => {
      const stops = r.stops.map((s) => `<td class="num">${s}</td>`).join('');
      return `
      <tr class="${r.isMonitored ? 'monitored' : ''}">
        <td>${Fmt.pos(r.real)}</td>
        <td>${r.pista || '--'}</td>
        <td>${Fmt.esc(r.kart)}</td>
        <td>${Fmt.esc(r.team)}</td>
        <td>${Fmt.esc(r.driver)}</td>
        <td class="num">${Fmt.esc(r.gap)}</td>
        <td class="num">${Fmt.lap(r.lastLapSeconds)}</td>
        <td class="num">${Fmt.pos(r.lastLapRank)}</td>
        <td class="num">${r.lap || '--'}</td>
        <td class="num">${Fmt.lap(r.avg5Seconds)}</td>
        <td class="num" data-tick="clock" data-base="${r.trackTimeSeconds}">${Fmt.clock(r.trackTimeSeconds)}</td>
        <td class="num">${Fmt.lap(r.stintAvgSeconds)}</td>
        <td class="num">${Fmt.pos(r.stintAvgRank)}</td>
        ${stops}
        <td><span class="status-dot status-${r.status}" title="${Fmt.statusLabel[r.status] || r.status}"></span></td>
      </tr>`;
    }).join('') || '<tr class="empty-row"><td colspan="20">Aguardando dados...</td></tr>';
  }

  return { render };
})();
