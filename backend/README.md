# Sistema de Gestão de Captação e Atendimento de Caminhoneiros

Back-end em ASP.NET Core (net10.0) com MySQL 8. Autores: Gabriel Braga e Vitor de Souza Matos (FATEC Araraquara).

## Pré-requisitos
- .NET SDK 10
- MySQL 8.0 ou superior (a collation `utf8mb4_0900_ai_ci` não existe no MariaDB)

## Como executar
1. **Criar o banco** (a partir da pasta raiz do projeto):
   ```
   mysql -u root -p < database/criar_banco.sql
   ```
   O banco é criado por este script; a API não cria nem altera tabelas.
2. **Configurar** (na pasta `backend`), informando a senha do MySQL e os dados do administrador inicial:
   - Windows: `.\configurar.ps1`
   - Linux/macOS: `./configurar.sh`

   O script cria `CodigoFonte/API/appsettings.Development.json` com uma chave JWT aleatória.
   Esse arquivo contém segredos e é ignorado pelo Git. Se preferir, copie
   `appsettings.Development.example.json` para `appsettings.Development.json` e preencha à mão
   (a chave `Jwt:Key` deve ser um valor base64 de pelo menos 32 bytes).
3. **Testar e executar** (na pasta `backend`):
   ```
   dotnet build
   dotnet test
   dotnet run --project CodigoFonte/API --launch-profile https
   ```
4. Abra `https://localhost:7033/swagger`, faça login em `POST /api/Autenticacao/login` com o e-mail e a senha do
   administrador informados no passo 2 e use o token no botão **Authorize**.

Na primeira execução a API cria o administrador inicial e os campos predefinidos do formulário.

## Front-end
Com a API em execução, abra outro terminal na pasta `frontend/Prototipo` e execute:
```
npm install
npm run dev
```
Abra `http://localhost:5173`. O servidor Vite encaminha as chamadas `/api` e
`/hubs` para `https://localhost:7033`; para links do formulário local, configure
`LinksAcesso:UrlBaseFormulario` como `http://localhost:5173/acesso`.

## Configurações opcionais
- `Armazenamento:PastaChaves` e `Armazenamento:PastaDocumentos`: pastas das chaves de proteção do CPF e dos
  documentos enviados. Sem elas, usa-se a pasta `dados-seguranca` na raiz do projeto. Faça backup dessa pasta:
  sem as chaves, os CPFs cifrados não podem ser lidos. Nunca a versione nem a compartilhe.
- `LinksAcesso:ValidadeHoras` e `LinksAcesso:UrlBaseFormulario`: validade e endereço base do link do motorista.
- `Cors:OrigensPermitidas`: origens do front-end autorizadas a chamar a API.

## Segurança
Não versione `appsettings.Development.json`, `secrets.json` nem `dados-seguranca/`.
