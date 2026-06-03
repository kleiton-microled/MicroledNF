# Gerar instalador Local Agent e publicar no servidor

## Pré-requisitos

| Onde | O quê |
|------|--------|
| **Windows** (obrigatório para o `.exe`) | [.NET 8 SDK](https://dotnet.microsoft.com/download), [Inno Setup 6](https://jrsoftware.org/isinfo.php) |
| **Servidor** | Acesso SSH ao VPS (`amktech@147.93.15.250`) |
| **Repo** | `deploy/clients/amktech.json` com `mainApiUrl` correto da API |

Confirme a URL da API em `deploy/clients/amktech.json` (`mainApiUrl`). Deve ser a **API** (ex.: `https://api.amktechsistemas.com.br`), **não** o frontend Angular.

---

## Parte 1 — Gerar o instalador (Windows)

No **Prompt de Comando** ou **PowerShell**, na raiz do repositório:

```bat
cd D:\git\kleiton\MicroledNF

scripts\build-localagent-installer.cmd deploy\clients\amktech.json
```

Saída esperada:

```text
dist\installers\Microled-NFe-LocalAgent-amktech-1.0.0.exe
```

### Se o comando falhar

**Passo a passo manual:**

```powershell
cd D:\git\kleiton\MicroledNF

.\scripts\Prepare-ClientPackage.ps1 -ClientConfigPath .\deploy\clients\amktech.json

.\scripts\Build-LocalAgent-Installer.ps1 `
  -PublishDir .\dist\localagent-publish\amktech `
  -ClientId amktech
```

Se `ISCC.exe` não for encontrado, instale Inno Setup 6 e use:

```powershell
.\scripts\Build-LocalAgent-Installer.ps1 `
  -PublishDir .\dist\localagent-publish\amktech `
  -ClientId amktech `
  -InnoSetupCompiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

---

## Parte 2 — Enviar o instalador para o servidor

Pasta da API no VPS (Jenkins): `/var/www/amktechsistemas/notafiscal-api`

O endpoint de download lê: `App_Data/installers/*.exe`

### Opção A — SCP (recomendado)

No Windows (PowerShell), a partir da pasta do repo:

```powershell
scp .\dist\installers\Microled-NFe-LocalAgent-amktech-1.0.0.exe `
  amktech@147.93.15.250:/var/www/amktechsistemas/notafiscal-api/App_Data/installers/
```

No Mac/Linux:

```bash
scp dist/installers/Microled-NFe-LocalAgent-amktech-1.0.0.exe \
  amktech@147.93.15.250:/var/www/amktechsistemas/notafiscal-api/App_Data/installers/
```

### Opção B — SSH + criar pasta + upload

```bash
ssh amktech@147.93.15.250 "mkdir -p /var/www/amktechsistemas/notafiscal-api/App_Data/installers"

scp dist/installers/Microled-NFe-LocalAgent-amktech-1.0.0.exe \
  amktech@147.93.15.250:/var/www/amktechsistemas/notafiscal-api/App_Data/installers/
```

### Opção C — SFTP (FileZilla / WinSCP)

1. Host: `147.93.15.250`, usuário: `amktech`
2. Navegue até `/var/www/amktechsistemas/notafiscal-api/App_Data/installers/`
3. Envie o arquivo `Microled-NFe-LocalAgent-amktech-1.0.0.exe`

Se a pasta `App_Data/installers` não existir, crie no servidor:

```bash
ssh amktech@147.93.15.250
sudo mkdir -p /var/www/amktechsistemas/notafiscal-api/App_Data/installers
sudo chown -R amktech:amktech /var/www/amktechsistemas/notafiscal-api/App_Data
```

---

## Parte 3 — Validar no servidor

```bash
ssh amktech@147.93.15.250

ls -lh /var/www/amktechsistemas/notafiscal-api/App_Data/installers/
```

Deve listar o `.exe` (tamanho típico: dezenas a ~100+ MB).

Teste o endpoint (substitua pela URL pública da API):

```bash
curl -sI https://api.amktechsistemas.com.br/api/v1/local-agent/installer/info
curl -sI https://api.amktechsistemas.com.br/api/v1/local-agent/installer
```

- `installer/info` → **200** + JSON com `fileName`, `sizeBytes`
- `installer` → **200** + `Content-Type: application/octet-stream`

Se **404**: o serviço não está lendo a pasta certa ou o arquivo não está no path acima. Confira `LocalAgentInstaller:Directory` em `appsettings.Production.json`.

Reinicie a API se necessário:

```bash
sudo systemctl restart notafiscal-api
sudo systemctl status notafiscal-api --no-pager
```

---

## Parte 4 — Frontend

Link de download:

```text
GET {API_BASE_URL}/api/v1/local-agent/installer
```

Metadados (tamanho para exibir no botão):

```text
GET {API_BASE_URL}/api/v1/local-agent/installer/info
```

---

## O que já foi preparado neste ambiente (Mac)

Foi feito apenas o **publish win-x64** (sem o `.exe` do Inno Setup — exige Windows):

- `dist/localagent-publish/amktech/` — binários + `appsettings.Client.json`
- `deploy/clients/amktech.json` — config do cliente

Para obter o instalador final, execute a **Parte 1** em uma máquina **Windows** com Inno Setup 6.

---

## Checklist rápido

1. [ ] Ajustar `mainApiUrl` em `deploy/clients/amktech.json`
2. [ ] Rodar `build-localagent-installer.cmd` no Windows
3. [ ] Copiar `.exe` para `.../notafiscal-api/App_Data/installers/` no VPS
4. [ ] Testar `/api/v1/local-agent/installer/info`
5. [ ] Botão de download no frontend apontando para `/api/v1/local-agent/installer`
