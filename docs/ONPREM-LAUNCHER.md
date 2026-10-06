# Microled NFe — Ambiente local (cliente final)

Painel gráfico Windows (WPF) que sobe o stack **sem Docker, sem `ng serve` e sem `dotnet run`**.

Não use Electron neste pacote: o launcher é `.NET 8` nativo; o front Angular é servido pela própria API em `http://localhost:5249`.

## O que o cliente faz

1. Instala `Microled-NFe-AmbienteLocal-1.0.0.exe` (Inno Setup)
2. Abre **Microled NFe Ambiente Local**
3. Indica o arquivo Access (`.mdb` / `.accdb`)
4. Clica em **Iniciar ambiente**
5. Escolhe o certificado digital na lista
6. Clica em **Abrir sistema**

O caminho do Access é gravado em `%ProgramData%\Microled\Nfe\localagent\settings.json` (o LocalAgent recarrega esse arquivo). Não é preciso editar `appsettings.json`.

## Componentes

| Serviço | Como sobe | URL / porta |
|---|---|---|
| PostgreSQL 16 portátil | `pgsql\bin\pg_ctl` (não é Docker). Sobe na primeira inicialização e **permanece ligado** ao parar o ambiente ou fechar o painel. | `127.0.0.1:5435` |
| API .NET self-contained | `api\Microled.Nfe.Service.Api.exe` + front em `wwwroot` | `http://localhost:5249` |
| LocalAgent | `agent\Microled.Nfe.LocalAgent.Api.exe` | `http://localhost:5278` |

Dados do Postgres: `%ProgramData%\Microled\Nfe\postgres\data`

Swagger da API (quando o front está no `wwwroot`): `http://localhost:5249/swagger`

## Pré-requisitos no PC do cliente

- Windows 10/11 64 bits
- Microsoft ACE OLEDB (Access)
- Certificado A1/A3 no repositório **Usuário atual → Pessoal**
- Driver do token A3, se for o caso
- Visual C++ Redistributable (PostgreSQL)

O LocalAgent **não** deve rodar como Windows Service (PIN do A3).

## Build (equipe Microled)

```bat
scripts\build-onprem-installer.cmd
```

Ou em etapas:

```powershell
.\scripts\Build-OnPrem-Package.ps1
.\scripts\Build-OnPrem-Installer.ps1 -SkipPackageBuild
```

O Angular recebe `public/runtime-config.onprem.json` copiado para `api\wwwroot\runtime-config.json` (`notasFiscaisApiUrl` na API localhost; `localAgentBaseUrl` vazio para chamadas same-origin `/api/local/...`, que a API encaminha ao agente na porta 5278). Em nuvem, o `runtime-config.json` padrão continua apontando para a API AMK e o agente em 5278.

Se `npm` não estiver no PATH, o script baixa **Node.js portátil** em `dist\tools\node` só para compilar o front (isso não entra no instalador do cliente). O script usa o certificado do Windows/`strict-ssl=false` se o registry npm falhar por proxy ou antivírus.

PostgreSQL: `Download-PortablePostgres.ps1` baixa o zip EDB de binários Windows e **copia só `bin`, `lib`, `share` e `include`** (sem pgAdmin). Se o download falhar, extraia manualmente essas pastas para `dist\onprem-package\pgsql` com `bin\pg_ctl.exe`.
