# Como rodar o projeto local

Guia para quem acabou de clonar o repositório. Tudo roda localmente: banco MySQL, API ASP.NET Core e painel React.

## 1. O que instalar

| Programa | Versão | Para que serve |
|---|---|---|
| Git | qualquer | clonar o repositório |
| .NET SDK | 10 | compilar e rodar a API |
| MySQL Server | 8 | banco de dados |
| MySQL Workbench | opcional | executar o script do banco |
| Node.js | LTS (inclui o npm) | rodar o painel |
| VS Code | opcional | editar o código |

Confira as instalações:

```
git --version
dotnet --version
mysql --version
node --version
npm --version
```

## 2. Baixar o projeto

```
git clone https://github.com/Nazz1n/Projeto-Tcc.git
cd Projeto-Tcc
```

Se o repositório for privado, é preciso ter sido convidado como colaborador e estar logado no GitHub.

## 3. Criar o banco de dados

1. Abra o MySQL Workbench e conecte como `root`.
2. Abra o arquivo `database/criar_banco.sql` e execute o script inteiro.
3. Confira:

```sql
USE captacao_caminhoneiros;
SHOW TABLES;
```

Devem aparecer 10 tabelas. A API não cria o banco sozinha, então esse passo é obrigatório.

## 4. Gerar a configuração local

O arquivo `appsettings.Development.json` guarda senhas e chaves e **não vai para o Git**. Cada computador gera o seu com o script abaixo.

No PowerShell, a partir da raiz do projeto:

```powershell
cd backend
.\configurar.ps1
```

O script pergunta:

- a senha do usuário `root` do MySQL;
- o nome do administrador;
- o e-mail do administrador (será o seu login no painel);
- a senha do administrador (mínimo de 10 caracteres; ao digitar, nada aparece na tela).

Ele também gera sozinho a chave do JWT e a chave de hash do CPF, e cria o arquivo `backend/CodigoFonte/API/appsettings.Development.json`.

Atenção: os dados do administrador só valem na primeira execução da API em um banco novo. Se já existir um administrador ativo no banco, a API o mantém e ignora esses campos.

Se o Windows bloquear a execução do script, rode antes, na mesma janela:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

No Linux ou macOS:

```
cd backend
./configurar.sh
```

## 5. Rodar o back-end (API)

```powershell
cd CodigoFonte\API
dotnet run --launch-profile https
```

Deve aparecer `Now listening on: https://localhost:7033`. Deixe esse terminal aberto.

- Na primeira execução, a API cria o administrador e os campos padrão do formulário.
- Se o navegador reclamar do certificado, rode uma vez `dotnet dev-certs https --trust`.
- Documentação interativa da API: https://localhost:7033/swagger

Testes automatizados do back-end (opcional), a partir da raiz do projeto:

```
dotnet test backend/Testes/Captacao.Testes/Captacao.Tests.csproj
```

## 6. Rodar o front-end (painel)

Em **outro terminal**, a partir da raiz do projeto:

```
cd frontend/Prototipo
npm ci
npm run dev
```

Abra http://localhost:5173 e entre com o e-mail e a senha do administrador definidos no passo 4.

Para gerar a versão de produção (opcional): `npm run build`.

## 7. Fluxo para testar

1. Entre como administrador.
2. Em **Cargas**, cadastre uma carga.
3. Na **Central de atendimento**, clique em **Iniciar atendimento** e informe um telefone.
4. Copie o link gerado e abra em uma aba anônima. Preencha o formulário do motorista e envie os documentos.
5. Volte ao painel: a solicitação muda de coluna em tempo real.
6. Abra a solicitação, valide os documentos e avance o status até **Contratado**.

## 8. Regras importantes

- **Front e API na mesma máquina.** O front fala com a API por `localhost` (proxy do Vite para `https://localhost:7033`).
- **Cada computador tem o seu banco.** Os dados não são compartilhados entre máquinas.
- **Abrir o link de cadastro no celular.** Os links gerados apontam para `localhost`. Para abrir em outro aparelho:
  1. descubra o IP do computador na rede (`ipconfig`), por exemplo `192.168.0.10`;
  2. em `appsettings.Development.json`, troque `LinksAcesso:UrlBaseFormulario` para `http://192.168.0.10:5173/acesso`;
  3. adicione `http://192.168.0.10:5173` em `Cors:OrigensPermitidas`;
  4. reinicie a API. O celular precisa estar na mesma rede Wi-Fi.
- **Segredos.** Nunca versione o `appsettings.Development.json` nem a pasta `dados-seguranca/`, e não compartilhe zips da pasta do projeto com esses arquivos dentro. O modelo `appsettings.Development.example.json` pode ser versionado.

## 9. Problemas comuns

| Mensagem ou sintoma | Causa e solução |
|---|---|
| `Cors:OrigensPermitidas não configurado` | O `appsettings.Development.json` não existe nessa pasta. Rode o passo 4. |
| `Chave de hash do CPF não configurada` | O arquivo foi criado sem `Seguranca:ChaveHashCpf`. Apague-o e rode o passo 4 de novo. |
| `Unable to connect to any of the specified MySQL hosts` ou `Access denied` | MySQL desligado ou senha errada. Inicie o serviço do MySQL e confira a senha, refazendo o passo 4 se precisar. |
| `Unknown database 'captacao_caminhoneiros'` | O script do banco não foi executado. Faça o passo 3. |
| Login do painel mostra "API indisponível" | A API não está rodando ou não está na porta 7033. Faça o passo 5 com `--launch-profile https`. |
| "Sua sessão expirou" | O login vale 60 minutos. Entre novamente. |
| `npm ci` falha | Confira a versão do Node (`node --version`) e se está na pasta `frontend/Prototipo`. |
| Esqueci a senha do administrador | A API só cria o administrador quando **não existe nenhum ativo**, então mudar a senha no `appsettings.Development.json` não altera quem já existe. Em ambiente de testes, apague o banco (`DROP DATABASE captacao_caminhoneiros;`), execute o `criar_banco.sql` de novo e reinicie a API: o administrador é recriado com os dados do arquivo de configuração. Isso apaga todos os dados. |

## 10. Estrutura do repositório

```
database/    script SQL de criação do banco
backend/     API ASP.NET Core (CodigoFonte, Testes) e scripts de configuração
frontend/    painel React + Vite (frontend/Prototipo)
```

Mais detalhes sobre a API, o JWT e o armazenamento seguro estão em `backend/README.md`.