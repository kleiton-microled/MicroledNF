---
name: localagent-publish
description: >-
  Publica, implanta e reinicia o Microled NFe Local Agent no Windows (win-x64,
  Program Files, health check, CORS/PNA). Use quando o usuário pedir publicar,
  atualizar, deploy, reiniciar ou restart do Local Agent, localagent, porta 5278,
  ou após alterar código/config do Microled.Nfe.LocalAgent.Api.
---

# Local Agent — publicar e reiniciar

## Escopo

Apenas **Windows**. Repositório: raiz `MicroledNF`. Instalação padrão: `C:\Program Files\Microled\NfeLocalAgent`.

## Comandos (executar na raiz do repo)

| Objetivo | Comando |
|----------|---------|
| **Publicar + implantar + reiniciar** (fluxo completo) | `.\scripts\Publish-And-Restart-LocalAgent.ps1` |
| Outro cliente | `.\scripts\Publish-And-Restart-LocalAgent.ps1 -ClientConfigPath .\deploy\clients\SEU.json` |
| **Só reiniciar** (sem copiar DLLs) | `.\scripts\Publish-And-Restart-LocalAgent.ps1 -RestartOnly` ou `.\scripts\Restart-LocalAgent.ps1` |
| **Só implantar** (publish já feito) | `.\scripts\Publish-And-Restart-LocalAgent.ps1 -DeployOnly` |

Atalho equivalente ao instalador completo (Inno Setup):

```bat
scripts\build-localagent-installer.cmd deploy\clients\amktech.json
```

## O que o agente DEVE fazer

1. Rodar o script adequado via PowerShell (`-ExecutionPolicy Bypass`).
2. **Aceitar UAC** quando `Deploy-LocalAgent.ps1` pedir elevação (cópia em Program Files).
3. Confirmar sucesso com:

```powershell
curl.exe -s http://localhost:5278/api/local/health
```

4. Reportar ao usuário: health JSON, horário do DLL em Program Files se relevante.

## Ordem interna (não inverter)

1. **Parar** `Microled.Nfe.LocalAgent.Api.exe` (senão robocopy erro 32 — arquivo em uso).
2. **Publicar** (`Prepare-ClientPackage.ps1` → `dist\localagent-publish\{clientId}`).
3. **Copiar** com admin (`Deploy-LocalAgent.ps1` — não sobrescreve `appsettings.Client.json`).
4. **Iniciar** via `wscript.exe` + `StartLocalAgent.vbs`.

## Logs em arquivo

`%ProgramData%\Microled\Nfe\localagent\logs\localagent-YYYYMMDD.log` — inclui persistência na API (`persist/send-result`, etc.).

## Config sem admin

Edits operacionais em:

`C:\ProgramData\Microled\Nfe\localagent\settings.json`

Depois **reiniciar** (`-RestartOnly`). Mudanças em `Program Files` exigem admin ou redeploy.

## Pré-requisitos

- .NET 8 SDK
- Perfil `Microled.Nfe.LocalAgent.Api/Properties/PublishProfiles/LocalAgent-win-x64.pubxml` (self-contained)
- Inno Setup 6 ou 7 apenas para gerar `.exe` instalador

## Erros comuns

| Sintoma | Causa | Ação |
|---------|-------|------|
| robocopy erro 32 | Agente rodando | Parar processo; usar `Deploy-LocalAgent.ps1` |
| robocopy erro 5 | Sem admin | UAC / executar deploy elevado |
| CORS no Chrome (`Provisional headers`) | Private Network Access | Código em `Program.cs` deve enviar `Access-Control-Allow-Private-Network: true`; redeploy |
| `isSentToWebService: false` | `ValidateXmlAndRps` bloqueava envio | Corrigido em `LocalRpsProcessingService` — redeploy |

## Cliente padrão

`deploy/clients/amktech.json` — `mainApiUrl` da API (`https://api.amktechsistemas.com.br`), não o frontend Angular.
