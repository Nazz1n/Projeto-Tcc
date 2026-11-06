# Projeto TCC — Captação de Caminhoneiros

Sistema de gestão de captação e atendimento de motoristas, com API ASP.NET Core, banco MySQL e painel React.

## Requisitos

- .NET SDK 10
- MySQL 8
- Node.js e npm

## Configuração e execução

1. Crie o banco executando `database/criar_banco.sql` em uma instância MySQL 8.
2. Na pasta `backend`, execute `.\configurar.ps1` no Windows ou `./configurar.sh` no Linux/macOS. O script cria a configuração local da API; ela contém segredos e não deve ser versionada.
3. Inicie a API:

   ```sh
   dotnet run --project backend/CodigoFonte/API --launch-profile https
   ```

4. Em outro terminal, instale e inicie o painel:

   ```sh
   cd frontend/Prototipo
   npm install
   npm run dev
   ```

5. Abra `http://localhost:5173`. O painel encaminha chamadas `/api` e `/hubs` para a API local em `https://localhost:7033`.

Para mais detalhes da configuração do banco, API, JWT e armazenamento seguro, consulte [backend/README.md](backend/README.md).

## Testes

```sh
dotnet test backend/Testes/Captacao.Testes/Captacao.Tests.csproj
cd frontend/Prototipo
npm run build
```

## Segurança

Não versione configurações locais (`appsettings.Development.json`, `.env`), arquivos de credenciais, nem a pasta `dados-seguranca/`.
