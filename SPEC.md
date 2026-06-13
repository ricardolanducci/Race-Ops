# Race Ops — Especificação do Projeto

## Visão Geral

Race Ops é uma aplicação web de monitoramento de corridas em tempo real. Consome a API do Race Monitor para exibir sessões ao vivo, posicionamento, tempos de volta, status de bandeira e histórico de resultados. O sistema foi projetado para equipes de operações de pista que precisam acompanhar múltiplas categorias simultaneamente.

---

## Repositório GitHub

- **Organização/Usuário:** ricardolanducci
- **Nome do repositório:** `race-ops`
- **Branch principal:** `main`
- **Branches de feature:** `feature/<nome-curto>`
- **Estrutura de commits:** mensagens em inglês no formato `feat:`, `fix:`, `chore:`

---

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | C# .NET 8, ASP.NET Core |
| ORM | Entity Framework Core |
| Banco de Dados | SQLite (local/dev) → SQL Server (produção) |
| Real-time | SignalR (WebSocket do servidor para o browser) |
| Frontend | HTML + CSS + Vanilla JS (sem framework) |
| Testes | xUnit + Moq (backend) |

---

## Arquitetura

O projeto segue Clean Architecture com separação clara de responsabilidades. A regra principal: **dependências sempre apontam para dentro** — infraestrutura depende de aplicação, aplicação depende de domínio, domínio não depende de nada.

```
RaceOps/
├── src/
│   ├── RaceOps.Domain/          # Entidades e interfaces
│   ├── RaceOps.Application/     # Casos de uso e DTOs
│   ├── RaceOps.Infrastructure/  # EF Core, Race Monitor API, WebSocket
│   └── RaceOps.Web/             # ASP.NET Core API + SignalR + Frontend estático
└── tests/
    ├── RaceOps.Domain.Tests/
    ├── RaceOps.Application.Tests/
    └── RaceOps.Infrastructure.Tests/
```

---

## Domínio (`RaceOps.Domain`)

### Entidades

**Race**
```csharp
public class Race
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Track { get; init; }
    public bool IsLive { get; init; }
    public long StartDateEpoch { get; init; }
    public long EndDateEpoch { get; init; }
    public string TimeZoneId { get; init; }
}
```

**LiveSession**
```csharp
public class LiveSession
{
    public string RunNumber { get; init; }
    public string SessionName { get; init; }
    public string TrackName { get; init; }
    public string TrackLength { get; init; }
    public string CurrentTime { get; init; }
    public string SessionTime { get; init; }
    public string TimeToGo { get; init; }
    public string LapsToGo { get; init; }
    public FlagStatus FlagStatus { get; init; }
    public string SortMode { get; init; }
    public IReadOnlyDictionary<string, RaceClass> Classes { get; init; }
    public IReadOnlyDictionary<string, Competitor> Competitors { get; init; }
}
```

**Competitor**
```csharp
public class Competitor
{
    public string RacerId { get; init; }
    public string Number { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Nationality { get; init; }
    public string ClassId { get; init; }
    public string Position { get; init; }
    public string Laps { get; init; }
    public string TotalTime { get; init; }
    public string BestLap { get; init; }
    public string BestLapTime { get; init; }
    public string LastLapTime { get; init; }
}
```

**FlagStatus (enum)**
```csharp
public enum FlagStatus { Unknown, Green, Yellow, Red, Finish }
```

### Interfaces (contratos de saída do domínio)

```csharp
public interface IRaceMonitorClient
{
    Task<IReadOnlyList<Race>> GetCurrentRacesAsync(CancellationToken ct);
    Task<LiveSession> GetSessionAsync(int raceId, CancellationToken ct);
    Task<CompetitorDetail> GetRacerAsync(int raceId, string racerId, CancellationToken ct);
    Task<StreamingConnectionInfo> GetStreamingConnectionAsync(int raceId, CancellationToken ct);
}
```

---

## Aplicação (`RaceOps.Application`)

Cada caso de uso é uma classe com um único método público `ExecuteAsync`. Sem herança desnecessária.

### Casos de Uso

| Classe | Responsabilidade |
|---|---|
| `GetCurrentRacesUseCase` | Lista corridas ativas no momento |
| `GetLiveSessionUseCase` | Retorna estado atual da sessão (posições, tempos) |
| `GetRacerDetailUseCase` | Detalhes + histórico de voltas de um piloto |
| `GetStreamingConnectionUseCase` | Retorna URL WebSocket para conexão live |

### DTOs

Os casos de uso retornam DTOs simples (records), não entidades de domínio. Exemplo:

```csharp
public record LiveSessionDto(
    string SessionName,
    string FlagStatus,
    string TimeToGo,
    string LapsToGo,
    IReadOnlyList<CompetitorDto> Competitors
);

public record CompetitorDto(
    string Position,
    string Number,
    string FullName,
    string Class,
    string Laps,
    string BestLapTime,
    string LastLapTime,
    string TotalTime
);
```

---

## Infraestrutura (`RaceOps.Infrastructure`)

### Race Monitor API Client

- Implementa `IRaceMonitorClient`
- Base URL: `https://api.race-monitor.com/v2/`
- Todos os endpoints usam `POST` com `Content-Type: application/x-www-form-urlencoded`
- O `apiToken` é incluído em todo POST

**Configuração (appsettings.json):**
```json
{
  "RaceMonitor": {
    "ApiToken": "f79a145d-67e7-42f0-801e-02f85588f1e5",
    "BaseUrl": "https://api.race-monitor.com/v2/"
  }
}
```

**Endpoints consumidos:**

| Endpoint | Parâmetros | Uso |
|---|---|---|
| `POST /v2/Account/CurrentRaces` | apiToken, seriesID?, raceTypeID? | Lista corridas da conta |
| `POST /v2/Common/CurrentRaces` | apiToken, seriesID? | Lista corridas públicas |
| `POST /v2/Race/RaceDetails` | apiToken, raceID | Detalhes de uma corrida |
| `POST /v2/Live/GetSession` | apiToken, raceID | Estado atual da sessão (posições, tempos) |
| `POST /v2/Live/GetRacer` | apiToken, raceID, racerID | Detalhes + voltas de um piloto |
| `POST /v2/Live/GetRacerCount` | apiToken, raceID | Quantidade de pilotos na sessão |
| `POST /v2/Live/GetStreamingConnection` | apiToken, raceID | URL WebSocket para live stream |
| `POST /v2/Results/SessionsForRace` | apiToken, raceID | Sessões com resultados |
| `POST /v2/Results/SessionDetails` | apiToken, sessionID, includeLapTimes? | Resultados completos de uma sessão |
| `POST /v2/Results/CompetitorDetails` | apiToken, competitorID | Detalhes + voltas de um piloto nos results |

**Resposta padrão da API:**
```json
{ "Successful": true, ... }
{ "Successful": false, "Message": "Detalhes do erro" }
```

**Códigos HTTP relevantes:** `200 OK`, `403 Forbidden`, `404 Not Found`, `429 Rate Limit`, `500 Server Error`

### Live Streaming (WebSocket)

A atualização em tempo real usa o sistema de streaming do Race Monitor via WebSocket seguro.

**Fluxo:**
1. Backend chama `GetStreamingConnection` para obter a URL WSS
2. Backend abre conexão WebSocket com a URL retornada (`WebsocketURL`)
3. Race Monitor envia dados via protocolo RMonitor
4. Backend processa os comandos e re-transmite via **SignalR** para o frontend

**Comandos do protocolo (recebidos pelo backend):**

| Comando | Enviado quando | Conteúdo |
|---|---|---|
| `$A` | Conexão / mudança de piloto | Racer ID, Number, Transponder, Nome, Nacionalidade, Class ID |
| `$B` | Conexão / mudança de sessão | Run ID, Session Name |
| `$C` | Conexão / mudança de classe | Class ID, Description |
| `$COMP` | Conexão / mudança de piloto | Dados completos do competidor |
| `$E` | Mudança de configuração | TRACKNAME, TRACKLENGTH |
| `$F` | **A cada segundo** | Laps to go, Time to go, Time of day, Race time, Flag Status |
| `$G` | Mudança de posição | Race Position, Racer ID, Laps, Total Time |
| `$H` | Qualificação | Qualifying Position, Racer ID, Best Lap, Best Lap Time |
| `$I` | Reset de sessão | Sinaliza troca de grupo/reset |
| `$J` | Passagem no loop | Racer ID, Lap Time, Total Time |
| `$RMS` | Mudança de sort mode | "race" ou "qualifying" |
| `$RMLT` | Passagem (`$J`) | Racer ID, Epoch time da última passagem |

**Notas importantes:**
- Os tokens de conexão (`LiveTimingToken`) são renovados periodicamente — sempre buscar via `GetStreamingConnection`
- Usar WSS (wss://) — WS não funciona de forma confiável em redes móveis
- `$F` é o heartbeat enviado a cada segundo com status de bandeira e tempos

### Banco de Dados

O banco é usado para persistir preferências de configuração e cache de dados de corridas passadas. **Não é usado para dados live** (esses vêm direto da API).

**Tabelas:**

```sql
-- Configurações de usuário (qual corrida monitorar, preferências de UI)
CREATE TABLE UserPreferences (
    Id INTEGER PRIMARY KEY,
    SelectedRaceId INTEGER,
    Layout TEXT -- JSON com configuração dos painéis
);

-- Cache de corridas passadas (para acesso offline e histórico rápido)
CREATE TABLE RaceCache (
    RaceId INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Track TEXT,
    CachedAt TEXT NOT NULL
);
```

---

## API Web (`RaceOps.Web`)

### Endpoints REST

```
GET  /api/races/current          → lista corridas ao vivo
GET  /api/races/{raceId}         → detalhes de uma corrida
GET  /api/races/{raceId}/session → estado atual da sessão live
GET  /api/races/{raceId}/racer/{racerId} → detalhes de um piloto
GET  /api/races/{raceId}/streaming-connection → info de conexão WebSocket
GET  /api/races/{raceId}/sessions → sessões com resultados
GET  /api/sessions/{sessionId}   → resultados de uma sessão
```

### SignalR Hub

```
Hub URL: /hubs/race

Métodos que o servidor envia ao cliente:
- SessionUpdated(LiveSessionDto session)
- FlagChanged(string flagStatus, string timeToGo)
- CompetitorPassed(string racerId, string lapTime, string totalTime)
```

O backend mantém uma conexão WebSocket por corrida monitorada e distribui os eventos para todos os clientes conectados ao hub daquela corrida.

---

## Frontend

### Estrutura de Arquivos

```
RaceOps.Web/
└── wwwroot/
    ├── index.html
    ├── css/
    │   └── style.css
    └── js/
        ├── app.js          # Inicialização, roteamento simples
        ├── api.js          # Wrapper das chamadas REST
        ├── hub.js          # Conexão SignalR
        ├── leaderboard.js  # Renderização da tabela principal
        └── components/
            ├── header.js   # Timers e flag status
            ├── session.js  # Info da sessão
            └── racer.js    # Painel de detalhes do piloto
```

### Telas (baseado no protótipo Figma `[PW] Prototype` + planilha `RaceOps-Colunas[67]`)

**Tema:** Escuro (background `#101820`), tipografia condensada, indicadores coloridos por status.

**Estrutura geral:** Header superior (logo, navegação Dashboard/Análises/Eventos/Equipe, seletor de evento — ex: "Endurance KGV 2026 - Etapa 1") + sidebar esquerda com **Painéis** (Visão geral, Estratégia, Classificação), Meus painéis, Módulos, Pilotos, Alertas e Configurações evento.

O domínio das telas é **kart endurance**: a equipe monitora seus próprios karts (ex: #014, #015, #016) dentro do grid completo, com regras de parada (pit stop) e gestão de stints.

---

#### Painel 1 — Visão Geral (Dashboard)

Grade de módulos: um módulo **Cronometro** por kart monitorado, módulo **Alertas**, módulo **Classificação** (compacta) e a barra inferior **Dados de corrida**.

**Módulo Cronometro (por kart):**
- Cabeçalho: Kart # | Posição (Real/Pista) ex: `7º / 28º` | Volta atual
- Cronômetro grande da volta corrente (ex: `00:32.335`) + badge de status (`Em pista` verde / `Box` vermelho)
- Tempo de pista da stint atual + estimativa (`00:55:07 / est: 00:42:07`)
- Tabela de voltas recentes: Volta | Tempo | Delta (vs volta anterior, verde/vermelho) | Rank (rank do tempo na volta corrente do grid). Linha roxa = melhor volta pessoal.
- Cards: MV Stint Atual (melhor volta da stint) | Média 5V | Média 5V Rank
- **Relativo** com toggle REAL/PISTA: kart à frente e atrás (tempo de volta, nº, equipe) e diferenças (ex: `◀ +25.410 / -76.410 ▶`)
- **Histórico** (editável): Stint | Piloto | Tempo pista | Parada

**Módulo Alertas:** lista Alerta | Hora (ex: "Box - Em 4 voltas chance de 75% karts bons", "Kart #014 - Alerta de 50 minutos de pista").

**Barra Dados de corrida:** Início da corrida (editável) | Hora atual | Tempo decorrido | Tempo restante | Tempo médio pista | Tempo médio TOP10 | Previsão do tempo (agora / 0.5h / 1h / 2h).

---

#### Painel 2 — Classificação (tela cheia)

Tabela completa do grid, uma linha por kart (linhas dos karts da equipe destacadas). Colunas (aba "Cronometro" da planilha):

| Coluna | Abrev. | Origem | Descrição |
|---|---|---|---|
| Posição Real | Real | Calculado | Posição considerando quem está mais próximo de cumprir todas as regras de parada antes e com mais voltas |
| Posição Pista | Pista | Cronometragem | Posição registrada na cronometragem |
| Número Kart | Kart | Cronometragem | Número do kart |
| Nome Equipe | Equipe | Cronometragem | Nome da equipe |
| Nome Piloto | Piloto | Cronometragem | Piloto atualmente no kart |
| Diferença | Dif. | Calculado | Diferença para o kart da frente |
| Tempo Última Volta | TUV | Cronometragem | Tempo da última volta |
| Ranking Última Volta | R-UV | Calculado | Posição no ranking das últimas voltas de todos os karts |
| Volta Atual | Volta | Cronometragem | Volta atual do kart |
| Média Últimas 5 Voltas | Méd. 5V | Calculado | Média dos últimos 5 tempos de volta |
| Tempo de Pista | T.Pista | Calculado | Tempo na pista desde a última saída (parada detectada ou troca de piloto) |
| Média de Volta — Stint Atual | Méd Stint | Calculado | Média de todas as voltas da stint atual |
| Ranking — Stint Atual | R-St | Calculado | Ranking das médias de stint atual (todos comparados, independente do nº de voltas) |
| Nº de Paradas [Regra 1..N] | P(06m), P(10m)… | Calculado | Contador de paradas válidas por regra cadastrada (tempo da volta ≥ tempo mínimo da regra) |
| Status | Status | Calculado | Em pista = Verde · Próximo de parar = Amarelo (>50 min em pista) · Atenção = Laranja (volta >02:00.000) · Box = Vermelho (volta >03:00.000) |

---

#### Painel 3 — Estratégia

Tabs por kart monitorado (#014, #015, #016). Dois módulos:

**Gerenciador de stints** — header com contadores: `Paradas (6m) 4/9` · `Paradas (10m) 1/1 ✓` · `Pesada 1/1 ✓` · `Stint Estimado 01:26:00`. Tabela (aba "Gerenciador" da planilha) + botão `+ ADICIONAR`:

| Coluna | Editável | Descrição |
|---|---|---|
| Stint | Não | Contador de stints do kart |
| Piloto | Sim (dropdown) | Piloto da stint (input manual) |
| Hora de Entrada | Não | Largada ou Saída do Box. A partir da 2ª stint: Hora de Saída + Tempo de Parada |
| Volta Entrada | Não | A partir da 2ª stint: Volta Saída + 1 |
| Hora de Saída | Sim | Entrada no Box ou fim da corrida. Modo automático: Hora atual − Tempo de parada |
| Volta Saída | Sim | Detectável automaticamente pelo Tempo da Última Volta |
| Tempo de Parada | Sim | Tempo da volta em que contabilizou a parada (manual ou automático) |
| Pesada | Sim (checkbox) | Stint com lastro — coluna exibida só se o evento tem regra de stint pesada |
| Tempo de Pista | Não | Tempo na pista da stint (stint corrente mostra timer ao vivo) |
| Tempo Estimado | Não | Duração estimada da stint = duração da corrida / paradas mínimas cadastradas |

**Módulo Box** — análise da janela de parada: probabilidades do grid (Muito bons / Bons / Médios / Ruins / Muito ruins, em %), `Próx. janela` e `Tempo ref.`. Tabela: Kart | Dif. Média | R-Pot. | M. Volta | Méd Stint | Equipe | Piloto, com linhas coloridas pela categoria de probabilidade.

---

#### Configurações do evento e módulos

- **Configurações evento:** duração da corrida, regras de parada (nome, tempo mínimo, quantidade obrigatória — ex: 9 paradas de 6min, 1 de 10min), regra de stint pesada (opcional), karts monitorados pela equipe, thresholds de status (alerta de tempo de pista 50min, atenção 2min, box 3min).
- **Dialog de preferências por módulo** (engrenagem no header do módulo): abas Dados (toggles de quais campos/colunas exibir; campos obrigatórios bloqueados ex: Kart #, Cronometro) e Aparência. Botões: Utilizar padrão / Cancelar / Salvar.
- Módulos têm estados documentados de tamanho/scroll (Default, Smaller, Size Error, scroll horizontal/vertical).

### Comportamento

- Ao carregar, busca corridas ativas (`/api/races/current`)
- Usuário seleciona uma corrida → busca estado inicial (`/api/races/{id}/session`)
- Conecta ao SignalR hub para receber atualizações em tempo real
- Tabela atualiza em tempo real conforme eventos chegam
- Clicar em um piloto abre painel lateral com suas voltas
- Flag status muda a cor do header (verde/amarelo/vermelho)

---

## Testes

### O que testar

**Domain.Tests:** Lógica de parsing de FlagStatus, ordenação de competidores.

**Application.Tests:** Cada `UseCase` isolado com mock de `IRaceMonitorClient`. Verificar que retorna DTOs corretos e propaga erros adequadamente.

**Infrastructure.Tests:** Testar o parser do protocolo RMonitor (comandos `$A`, `$F`, `$J`, `$G`, `$H`). Não testar a rede real.

### Exemplos

```csharp
// Application test
[Fact]
public async Task GetLiveSession_ReturnsOrderedCompetitors()
{
    var mockClient = new Mock<IRaceMonitorClient>();
    mockClient.Setup(c => c.GetSessionAsync(123, default))
              .ReturnsAsync(FakeSession.WithThreeCompetitors());

    var useCase = new GetLiveSessionUseCase(mockClient.Object);
    var result = await useCase.ExecuteAsync(123);

    Assert.Equal(3, result.Competitors.Count);
    Assert.Equal("1", result.Competitors[0].Position);
}

// Infrastructure test
[Fact]
public void RMonitorParser_ParsesFlagCommand_Correctly()
{
    var line = "$F,5,\"00:05:00\",\"01:21:18\",\"00:30:00\",\"Green \"";
    var result = RMonitorParser.Parse(line);

    Assert.Equal("$F", result.Command);
    Assert.Equal(FlagStatus.Green, result.FlagStatus);
    Assert.Equal("01:21:18", result.TimeOfDay);
}
```

---

## Configuração Local (Getting Started)

```bash
# Clone
git clone https://github.com/ricardolanducci/race-ops.git
cd race-ops

# Backend
cd src/RaceOps.Web
dotnet run

# Acesse
http://localhost:5000
```

O frontend é servido como arquivos estáticos pelo próprio ASP.NET Core — sem necessidade de servidor separado para desenvolvimento local.

---

## Fluxo de Dados (Resumo)

```
Browser
  │  (1) GET /api/races/current
  │  (2) GET /api/races/{id}/session  (estado inicial)
  │  (3) Conecta SignalR /hubs/race
  ▼
ASP.NET Core (RaceOps.Web)
  │  Chama casos de uso da Application
  │  Mantém conexão WSS com Race Monitor por corrida monitorada
  ▼
RaceMonitorClient (Infrastructure)
  │  POST https://api.race-monitor.com/v2/...
  │  WSS wss://[retornado por GetStreamingConnection]
  ▼
Race Monitor API
```

---

## Decisões de Design

**Por que Vanilla JS no frontend?** O protótipo é uma aplicação de operações internas, não um produto público. Vanilla JS reduz dependências, facilita debug e é suficiente para a complexidade da UI.

**Por que polling + streaming?** O estado inicial vem via REST (simples, cacheável). Atualizações em tempo real vêm via WebSocket do Race Monitor → SignalR → Browser. Isso evita estado inconsistente e mantém o backend como fonte de verdade.

**Por que SQLite em dev?** Zero configuração, ideal para rodar localmente. A troca para SQL Server em produção é uma mudança de connection string no EF Core.

**Por que não usar o WebSocket do Race Monitor direto no browser?** O token de streaming e o API token não devem ser expostos no frontend. O backend funciona como proxy seguro.

---

## O que está fora do escopo (v1)

- Autenticação de usuários (o sistema é single-user por enquanto)
- Múltiplas corridas simultâneas no mesmo painel
- Mobile app
- Exportação de dados / relatórios
