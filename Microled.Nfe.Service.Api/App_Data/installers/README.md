# Local Agent installer (download)

Coloque aqui o instalador Windows gerado pelo pipeline:

`Microled-NFe-LocalAgent-{cliente}-1.0.0.exe`

A API expõe:

- `GET /api/v1/local-agent/installer` — download do arquivo
- `GET /api/v1/local-agent/installer/info` — metadados (nome, tamanho, data)

Configuração: `LocalAgentInstaller` em `appsettings.Production.json`.

No deploy (VPS), copie o `.exe` para esta pasta junto com o publish da API.
