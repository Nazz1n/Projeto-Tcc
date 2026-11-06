# Projeto TCC — Captação de Caminhoneiros

Sistema de gestão de captação e atendimento de motoristas, com API ASP.NET Core, banco MySQL e painel React.

## Requisitos

- .NET SDK 10
- MySQL 8
- Node.js e npm

## Executar a simulação do front-end

Esta branch permite testar as telas sem iniciar API ou banco. Os dados de exemplo e as alterações ficam no `localStorage` do navegador.

```sh
cd frontend/Prototipo
npm ci
npm run dev:demo
```

Abra `http://localhost:5173` e use a conta de demonstração preenchida na tela de login: `demo@fretebot.local` / `demo123`.

Para testar contra a API real nesta mesma branch, inicie o backend e o banco conforme [backend/README.md](backend/README.md), depois rode:

```sh
npm run dev:api
```

O proxy do Vite espera a API em `https://localhost:7033`; se estiver em outra porta, ajuste os destinos `/api` e `/hubs` em `frontend/Prototipo/vite.config.js`. Consulte [backend/README.md](backend/README.md) para detalhes de configuração.

## Testes

```sh
dotnet test backend/Testes/Captacao.Testes/Captacao.Tests.csproj
cd frontend/Prototipo
npm run build
```

## Segurança

Não versione configurações locais (`appsettings.Development.json`, `.env`), arquivos de credenciais, nem a pasta `dados-seguranca/`.
