# Configuração para Ambiente de HOMOLOGAÇÃO

Este documento descreve como configurar a solução para rodar contra o ambiente de HOMOLOGAÇÃO da NFS-e São Paulo (Nota do Milhão).

## 1. Configuração do appsettings.Homologation.json

Edite o arquivo `appsettings.Homologation.json` e configure:

```json
{
  "NfeService": {
    "BaseUrl": "https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "TimeoutSeconds": 60,
    "Environment": "Homologation",
    "DefaultIssuerCnpj": "SEU_CNPJ_AQUI",
    "DefaultIssuerIm": "SUA_INSCRICAO_MUNICIPAL_AQUI",
    "DefaultCnpjRemetente": "SEU_CNPJ_AQUI",
    "SchemaVersion": "2.0",
    "Versao": "2",
    "LogRawXml": true,
    "LogSensitiveData": false,
    "Certificate": {
      "Mode": "File",
      "FilePath": "C:\\caminho\\para\\seu\\certificado.pfx",
      "Password": "SENHA_DO_CERTIFICADO"
    }
  }
}
```

### Opções de Certificado

#### Modo File (A1 - Arquivo PFX)
```json
"Certificate": {
  "Mode": "File",
  "FilePath": "C:\\certs\\nfe-sp-homolog.pfx",
  "Password": "SUA_SENHA"
}
```

#### Modo Store (A3 - Windows Certificate Store)
```json
"Certificate": {
  "Mode": "Store",
  "StoreLocation": "CurrentUser",
  "StoreName": "My",
  "Thumbprint": "THUMBPRINT_DO_CERTIFICADO"
}
```

## 2. Executando a API em Modo Homologação

### Opção 1: Variável de Ambiente
```bash
ASPNETCORE_ENVIRONMENT=Homologation dotnet run
```

### Opção 2: launchSettings.json
Adicione um perfil no `launchSettings.json`:
```json
"Homologation": {
  "commandName": "Project",
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Homologation"
  }
}
```

## 3. Verificando a Configuração

### Health Check
Acesse: `GET /health/nfe`

Retorna:
- `200 OK` se tudo estiver configurado corretamente
- `503 Service Unavailable` se houver problemas (certificado não encontrado, configuração faltando, etc.)

### Exemplo de Resposta (Sucesso)
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "nfe",
      "status": "Healthy",
      "description": "NFS-e service is properly configured",
      "data": {
        "Endpoint": "https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx",
        "CnpjRemetente": "1234****5678",
        "InscricaoMunicipal": "12345678",
        "CertificateSubject": "CN=...",
        "CertificateThumbprint": "...",
        "CertificateHasPrivateKey": true,
        "CertificateNotAfter": "2025-12-31T23:59:59Z",
        "Environment": "Homologation",
        "SchemaVersion": "2.0"
      }
    }
  ]
}
```

## 4. Testando com Sandbox Controller

### Enviar RPS de Teste
```bash
POST /api/v1/sandbox/nfe/rps/send-sample
```

Este endpoint:
- Cria automaticamente um RPS de teste usando os dados padrão configurados
- Envia para o ambiente de homologação
- Retorna o protocolo e chaves geradas

### Consultar NFe
```bash
POST /api/v1/sandbox/nfe/consult-sample?numeroNFe=123456789
```

### Cancelar NFe
```bash
POST /api/v1/sandbox/nfe/cancel-sample?numeroNFe=123456789
```

## 5. Logging

### Log de XML (Modo Debug)

Configure `LogRawXml: true` para ver os XMLs completos:

```json
"LogRawXml": true,
"LogSensitiveData": false
```

Com `LogSensitiveData: false`, os dados sensíveis serão mascarados:
- CNPJ: `1234****5678`
- CPF: `123****45`
- Valores monetários: `***.**`
- Assinaturas Base64: `***BASE64_SIGNATURE***`

### Níveis de Log

Configure no `appsettings.Homologation.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microled.Nfe.Service": "Debug"
  }
}
```

## 6. Troubleshooting

### Erro: "Certificate configuration not provided"
- Verifique se `Certificate:Mode` está configurado como "File" ou "Store"
- Se Mode = "File", verifique se `FilePath` está correto
- Se Mode = "Store", verifique se `Thumbprint` está correto

### Erro: "Certificate does not have a private key"
- O certificado deve ter a chave privada para assinar
- Certificados A1 (PFX) geralmente incluem a chave privada
- Certificados A3 podem precisar de configuração adicional

### Erro: "NFe service endpoint is not configured"
- Configure `BaseUrl` ou `TestEndpoint` no appsettings
- Verifique se a URL está correta para homologação

### Erro: "DefaultCnpjRemetente is not configured"
- Configure `DefaultCnpjRemetente` com o CNPJ do prestador
- Configure `DefaultIssuerIm` com a Inscrição Municipal

## 7. Próximos Passos

1. Configure o certificado real de homologação
2. Configure os dados do prestador (CNPJ, IM)
3. Execute o health check: `GET /health/nfe`
4. Teste com o sandbox: `POST /api/v1/sandbox/nfe/rps/send-sample`
5. Verifique os logs para depurar eventuais problemas

## 8. Referências

- Documentação oficial: `NFe_Web_Service-4.pdf`
- Endpoint de Homologação: `https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx`
- Endpoint de Produção: `https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx`

