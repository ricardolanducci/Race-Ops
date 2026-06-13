# Race Ops

Aplicação web de monitoramento de corridas em tempo real, consumindo a API do [Race Monitor](https://www.race-monitor.com).

## Como rodar localmente

**Pré-requisito:** [.NET 8 SDK](https://dotnet.microsoft.com/download)

```bash
cd src/RaceOps.Web
dotnet run
```

Acesse: http://localhost:5000

## Como rodar os testes

```bash
dotnet test
```

## Estrutura

```
src/
  RaceOps.Domain/        # Entidades e interfaces
  RaceOps.Application/   # Casos de uso e DTOs
  RaceOps.Infrastructure/ # API client, WebSocket, banco de dados
  RaceOps.Web/           # ASP.NET Core API + SignalR + Frontend

tests/
  RaceOps.Application.Tests/
  RaceOps.Infrastructure.Tests/
```

## Configuração

O token da API está em `src/RaceOps.Web/appsettings.json`. Para produção, use variáveis de ambiente:

```bash
RaceMonitor__ApiToken=seu-token
```

## Documentação

Ver [SPEC.md](./SPEC.md) para a especificação completa do projeto.
