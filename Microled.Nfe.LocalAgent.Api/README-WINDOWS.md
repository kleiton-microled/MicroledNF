# Microled NFE LocalAgent on Windows

## Recommended execution mode

For A3 certificates that require PIN entry, run the LocalAgent as a normal desktop process in the logged-in user's session.

Do not run it as:

- a Windows Service
- a scheduled task running without interactive desktop access
- another Windows user account

This is the most reliable mode because the token middleware can open the PIN dialog when the private key is used.

## Instalação plug-and-play (recomendado para clientes)

Use o instalador Windows gerado pela Microled (Inno Setup). O IT **não** edita JSON manualmente.

1. Execute `Microled-NFe-LocalAgent-{cliente}-1.0.0.exe` como Administrador
2. Marque iniciar no logon do Windows
3. Use o atalho **Microled NFe Local Agent** (sem console) ou aguarde o Startup

Guia completo: [docs/LOCALAGENT-INSTALACAO-IT.md](../docs/LOCALAGENT-INSTALACAO-IT.md)

Build do instalador (equipe Microled, Windows + Inno Setup 6):

```bat
scripts\build-localagent-installer.cmd deploy\clients\seu-cliente.json
```

## Desenvolvimento local (manual)

1. Log in to Windows with the same user that has access to the certificate/token.
2. Ensure the token middleware/driver is installed and the token is connected.
3. Publish the LocalAgent for Windows:

```bat
publish-win-x64.cmd
```

4. Go to the publish folder:

```text
bin\Release\net8.0\publish\win-x64\
```

5. Adjust `appsettings.json` or add `appsettings.Client.json` if needed.
6. Start without console:

```bat
StartLocalAgent.vbs
```

Or with console: `run-localagent.cmd` or `StartLocalAgent.cmd`

## Why this mode is preferred

- Keeps the LocalAgent in the interactive desktop session
- Allows A3 middleware to prompt for PIN
- Avoids service-account access issues with `CurrentUser\My`
- Avoids requiring the .NET runtime on the client machine because the publish profile is self-contained

## Quick health check

After starting the LocalAgent, test:

```bat
curl http://localhost:5278/api/local/health
```

### POST JSON (NFS-e SP cálculo, etc.)

No **PowerShell**, o comando `curl` é um alias de `Invoke-WebRequest` e **não** se comporta como o curl real. Para POST com corpo JSON, use **`curl.exe`** (binário do Windows) ou **`Invoke-RestMethod`**.

Exemplo com `curl.exe`:

```bat
curl.exe -s -X POST "http://localhost:5278/api/local/nfse-sp/calculate-taxes" -H "Content-Type: application/json" -d "{\"valorServico\":1000,\"codigoServico\":10101,\"aliquotaIss\":0.05,\"regimeTributario\":\"LucroPresumido\"}"
```

Teste rápido do grupo de rotas: `curl http://localhost:5278/api/local/nfse-sp/ping`

## Notes

- The selected certificate profile is stored under `%ProgramData%\Microled\Nfe\localagent\profiles.json`
- If the certificate changes, restart the LocalAgent if the token middleware keeps stale state
- If the token requires a PIN dialog, approve it on the Windows desktop when prompted

## Persistência na API principal (PostgreSQL)

O LocalAgent executa SOAP com certificado local e envia os resultados para a **Microled.Nfe.Service.Api** persistir no banco.

Configure em `appsettings.json`:

```json
"NfeIntegration": {
  "SendToWebService": true,
  "MainApiBaseUrl": "https://SUA-API-AQUI"
}
```

**Importante:** `MainApiBaseUrl` deve apontar para a **API** (`Microled.Nfe.Service.Api`), **não** para o frontend Angular. Se apontar para o site (`app.amktechsistemas.com.br`), a resposta será HTML e a persistência falhará.

Teste se a API responde JSON antes de enviar RPS:

```bat
curl.exe -s -X POST "https://SUA-API-AQUI/api/v1/notas-fiscais/persist/send-result" ^
  -H "Content-Type: application/json" ^
  -d "{\"criadoPor\":\"test\",\"sucesso\":true,\"itens\":[]}"
```

Resposta esperada: JSON com `"success": true` ou `"success": false` — **nunca** HTML (`<!DOCTYPE` ou `<html`).

Em desenvolvimento local, com a API em `http://localhost:5249`:

```json
"MainApiBaseUrl": "http://localhost:5249"
```
