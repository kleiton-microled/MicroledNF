# Configuração da Solução Microled.Nfe.Service

## Visão Geral

A solução suporta diferentes ambientes (Development, Homologation, Production) e um modo fake para testes sem chamar o Web Service real.

## Configuração por Ambiente

### Development (appsettings.Development.json)

- **UseFakeGateway**: `true` - Usa `FakeNfeGateway` para testes locais
- **UseProduction**: `false`
- **Environment**: "Development"
- Não requer certificado configurado

### Homologation (appsettings.Homologation.json)

- **UseFakeGateway**: `false` - Usa `NfeSoapClient` real
- **UseProduction**: `false`
- **Environment**: "Homologation"
- Requer certificado configurado (Thumbprint)

### Production (appsettings.Production.json)

- **UseFakeGateway**: `false` - Usa `NfeSoapClient` real
- **UseProduction**: `true`
- **Environment**: "Production"
- Requer certificado configurado (Thumbprint)

## Estrutura de Configuração

### Features

```json
{
  "Features": {
    "UseFakeGateway": false  // true para usar FakeNfeGateway, false para NfeSoapClient real
  }
}
```

### NfeService

```json
{
  "NfeService": {
    "ProductionEndpoint": "https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "TestEndpoint": "https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "UseProduction": false,
    "Versao": "2",
    "TimeoutSeconds": 60,
    "DefaultCnpjRemetente": "00000000000000",
    "DefaultIssuerCnpj": "00000000000000",
    "DefaultIssuerIm": "00000000",
    "Environment": "Development",
    "SchemaVersion": "2.0",
    "Certificate": {
      "FilePath": "",           // Para certificado A1 (arquivo .pfx)
      "Password": "",            // Senha do certificado A1
      "StoreLocation": "CurrentUser",  // CurrentUser ou LocalMachine
      "StoreName": "My",        // Nome do repositório
      "Thumbprint": ""          // Thumbprint do certificado A3
    }
  }
}
```

## Certificado Digital

### Certificado A1 (Arquivo .pfx)

```json
{
  "Certificate": {
    "FilePath": "C:\\caminho\\para\\certificado.pfx",
    "Password": "senha_do_certificado"
  }
}
```

### Certificado A3 (Repositório do Windows)

```json
{
  "Certificate": {
    "StoreLocation": "CurrentUser",  // ou "LocalMachine"
    "StoreName": "My",
    "Thumbprint": "ABCDEF1234567890ABCDEF1234567890ABCDEF12"
  }
}
```

**Nota**: O thumbprint é normalizado automaticamente (espaços, hífens e dois pontos são removidos, convertido para maiúsculas).

## Modo Fake (FakeNfeGateway)

Quando `UseFakeGateway: true`, a solução usa `FakeNfeGateway` que:

- **SendRpsBatchAsync**: Retorna protocolo fictício e chaves de NF-e simuladas
- **ConsultNfeAsync**: Retorna NF-e fictícia com status "Autorizada"
- **CancelNfeAsync**: Retorna cancelamento fictício com sucesso

**Uso**: Ideal para desenvolvimento local sem necessidade de certificado ou conexão com o Web Service.

## Como Usar

### 1. Desenvolvimento Local (Fake)

1. Configure `appsettings.Development.json`:
   ```json
   {
     "Features": {
       "UseFakeGateway": true
     }
   }
   ```

2. Execute a API:
   ```bash
   dotnet run --environment Development
   ```

3. Teste via Swagger sem precisar de certificado ou conexão real.

### 2. Homologação

1. Configure `appsettings.Homologation.json`:
   ```json
   {
     "Features": {
       "UseFakeGateway": false
     },
     "NfeService": {
       "Certificate": {
         "Thumbprint": "SEU_THUMBPRINT_AQUI"
       }
     }
   }
   ```

2. Execute a API:
   ```bash
   dotnet run --environment Homologation
   ```

### 3. Produção

1. Configure `appsettings.Production.json`:
   ```json
   {
     "Features": {
       "UseFakeGateway": false
     },
     "NfeService": {
       "UseProduction": true,
       "Certificate": {
         "Thumbprint": "SEU_THUMBPRINT_AQUI"
       }
     }
   }
   ```

2. Execute a API:
   ```bash
   dotnet run --environment Production
   ```

## Validações

### CertificateProvider

- Verifica se o certificado tem chave privada
- Normaliza thumbprint (remove espaços, hífens, dois pontos)
- Lança exceções claras em caso de erro

### NfeSoapClient

- Valida se o endpoint está configurado
- Usa timeout configurado
- Trata erros HTTP e SOAP

## Exemplos no Swagger

Os controllers incluem exemplos de payload nos comentários XML:

- **POST /api/v1/rps/send**: Exemplo completo de envio de lote de RPS
- **POST /api/v1/nfe/consult**: Exemplos de consulta por ChaveNFe ou ChaveRps
- **POST /api/v1/nfe/cancel**: Exemplo de cancelamento de NF-e

## Troubleshooting

### Erro: "Certificate configuration not provided"

- Configure `FilePath` (A1) ou `Thumbprint` (A3) em `appsettings.json`

### Erro: "Certificate does not have a private key"

- O certificado deve ter chave privada para assinatura
- Verifique se o certificado está instalado corretamente

### Erro: "Certificate with thumbprint ... not found in store"

- Verifique o thumbprint (use `certlm.msc` no Windows)
- Confirme `StoreLocation` e `StoreName`
- O thumbprint é normalizado automaticamente

### Erro: "NfeService endpoint is not configured"

- Configure `ProductionEndpoint` ou `TestEndpoint` em `appsettings.json`

