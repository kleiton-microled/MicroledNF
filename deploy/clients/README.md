# Configuração por cliente (Local Agent)

Cada arquivo `*.json` descreve um pacote de instalação pré-configurado.

1. Copie `microled.example.json` para `{clientId}.json`
2. Ajuste `mainApiUrl`, `cnpj`, `inscricaoMunicipal`, `allowedOrigins`, etc.
3. Build (Windows + Inno Setup 6):

```powershell
.\scripts\Prepare-ClientPackage.ps1 -ClientConfigPath .\deploy\clients\{clientId}.json
.\scripts\Build-LocalAgent-Installer.ps1 -PublishDir .\dist\localagent-publish\{clientId} -ClientId {clientId}
```

Ou: `scripts\build-localagent-installer.cmd deploy\clients\{clientId}.json`

Entregue ao cliente: `dist\installers\Microled-NFe-LocalAgent-{clientId}-1.0.0.exe`

Guia de instalação: [docs/LOCALAGENT-INSTALACAO-IT.md](../../docs/LOCALAGENT-INSTALACAO-IT.md)
