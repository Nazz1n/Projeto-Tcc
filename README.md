# Projeto TCC — Captação de Caminhoneiros

Sistema de gestão de captação e atendimento de motoristas, com API ASP.NET Core, banco MySQL e painel React.

## Requisitos

- .NET SDK 10
- MySQL 8
- Node.js e npm

## Executar com a API

O painel usa a API real por padrão. Na máquina que executa o backend, inicie a API e o banco conforme [backend/README.md](backend/README.md). O proxy do Vite espera a API em `https://localhost:7033`.

Em outro terminal, inicie o painel:

```sh
cd frontend/Prototipo
npm ci
npm run dev
```

Abra `http://localhost:5173` e entre com uma conta cadastrada na API. O login, usuários, solicitações, documentos e demais dados vêm do backend; não há credenciais de demonstração preenchidas. Se a API estiver em outra porta, ajuste os destinos `/api` e `/hubs` em `frontend/Prototipo/vite.config.js`.

> A API deve estar acessível a partir da mesma máquina que executa o front. O endereço `localhost` sempre aponta para a própria máquina.

Para mais detalhes da configuração do banco, API, JWT e armazenamento seguro, consulte [backend/README.md](backend/README.md). A simulação sem backend fica isolada na branch `test/front-api-simulada`.

## Testes

```sh
dotnet test backend/Testes/Captacao.Testes/Captacao.Tests.csproj
cd frontend/Prototipo
npm run build
```

## Segurança

Não versione configurações locais (`appsettings.Development.json`, `.env`), arquivos de credenciais, nem a pasta `dados-seguranca/`.
