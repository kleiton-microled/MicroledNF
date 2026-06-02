# Microled NFe Local Agent — Instalação (IT)

Guia de 1 página para instalar o agente local no PC do cliente (Windows 10/11 x64).

## O que o instalador faz

- Copia o agente para `C:\Program Files\Microled\NfeLocalAgent\`
- Cria atalho no Menu Iniciar (execução **sem janela de console**)
- Opcional: iniciar automaticamente no logon do usuário
- Abre regra de firewall TCP **5278**
- Cria pastas em `%ProgramData%\Microled\Nfe\localagent\` (RpsOut, Validate, logs)
- Configuração da API e CNPJ já vêm **pré-definidas** no pacote (sem editar JSON)

## Pré-requisitos

1. Windows 10 ou 11 (64 bits)
2. Certificado digital (A1 ou A3) no repositório **Usuário atual → Pessoal**
3. Driver do token A3 instalado (se aplicável)
4. Usuário Windows que vai emitir NFS-e = mesmo que fará login no sistema web

## Instalação

1. Execute o instalador `Microled-NFe-LocalAgent-{cliente}-1.0.0.exe` **como Administrador**
2. Marque **“Iniciar automaticamente ao entrar no Windows”** (recomendado)
3. Conclua o assistente
4. Faça login com o usuário que usará o certificado
5. Na primeira emissão, **confirme o PIN** do token quando o Windows solicitar

## Verificação rápida

No PowerShell ou CMD:

```bat
curl.exe -s http://localhost:5278/api/local/health
```

Resposta esperada: JSON com `"status":"ok"`.

## Certificado

- Se o instalador foi gerado **com thumbprint**, o certificado já está indicado
- Caso contrário, no sistema web use uma vez: seleção de certificado (`POST /api/local/certificates/select`)
- Perfil salvo em: `%ProgramData%\Microled\Nfe\localagent\profiles.json`

## Persistência na nuvem

O agente envia os dados da prefeitura para a API principal (`MainApiBaseUrl` no pacote). Não é necessário configurar banco local.

## Desinstalação

Painel de Controle → Programas → **Microled NFe Local Agent** → Desinstalar.

## Download pelo sistema web

A API principal expõe o instalador para o frontend:

| Método | URL | Uso |
|--------|-----|-----|
| GET | `/api/v1/local-agent/installer` | Download do `.exe` |
| GET | `/api/v1/local-agent/installer/info` | Nome, tamanho e data do arquivo (JSON) |

No servidor de produção, copie o `Microled-NFe-LocalAgent-*.exe` para `App_Data/installers/` na pasta da API (ver `Microled.Nfe.Service.Api/App_Data/installers/README.md`).

## Suporte Microled — build do instalador

No repositório (máquina de build **Windows** com [Inno Setup 6](https://jrsoftware.org/isinfo.php)):

```powershell
# 1) Publicar + injetar config do cliente
.\scripts\Prepare-ClientPackage.ps1 -ClientConfigPath .\deploy\clients\seu-cliente.json

# 2) Gerar setup.exe
.\scripts\Build-LocalAgent-Installer.ps1 `
  -PublishDir .\dist\localagent-publish\seu-cliente `
  -ClientId seu-cliente
```

Artefato: `dist\installers\Microled-NFe-LocalAgent-{cliente}-1.0.0.exe`

Copie `deploy\clients\seu-cliente.json` a partir de `deploy\clients\microled.example.json`.

## Problemas comuns

| Sintoma | Ação |
|--------|------|
| Frontend não conecta | Verificar se o agente está rodando; testar `/api/local/health` |
| PIN não aparece | Agente deve rodar na sessão do usuário (não como Serviço Windows) |
| Firewall bloqueia | Reinstalar como admin ou liberar porta 5278 manualmente |
| Persistência falha | Confirmar `MainApiBaseUrl` no pacote aponta para a **API**, não o site Angular |

## Não usar

- Windows Service para o Local Agent (incompatível com PIN de certificado A3)
- Conta Windows diferente da que possui o certificado em `CurrentUser\My`
