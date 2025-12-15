# Microled NFS-e Service

Solução .NET 8 Web API para emissão de NFS-e (Nota Fiscal de Serviços Eletrônica) da Prefeitura de São Paulo (Nota do Milhão), baseada na documentação oficial e XSDs da versão 2.0 (Reforma Tributária).

## 📋 Visão Geral

### Objetivo

A API **Microled.Nfe.Service** permite a integração com o Web Service da Prefeitura de São Paulo para:

- **Emitir NFS-e** através do envio de lotes de RPS (Recibo Provisório de Serviços)
- **Consultar NFS-e** emitidas por chave da NF-e ou chave do RPS
- **Cancelar NFS-e** emitidas

### Arquitetura

A solução segue os princípios de **Clean Architecture**, garantindo:

- **Separação de responsabilidades** entre camadas
- **Independência de frameworks** e infraestrutura
- **Testabilidade** através de injeção de dependências
- **Manutenibilidade** através de código organizado e documentado

```
┌─────────────────────────────────────┐
│         API (Controllers)           │
│    Microled.Nfe.Service.Api         │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│      Application (Use Cases)         │
│  Microled.Nfe.Service.Application   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│       Business (Services)            │
│   Microled.Nfe.Service.Business     │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│         Domain (Entities)            │
│    Microled.Nfe.Service.Domain      │
└─────────────────────────────────────┘
               ▲
┌──────────────┴──────────────────────┐
│      Infrastructure (SOAP/XML)       │
│     Microled.Nfe.Service.Infra      │
└─────────────────────────────────────┘
```

## 🏗️ Estrutura de Projetos

### 1. Microled.Nfe.Service.Domain

**Responsabilidade**: Entidades de domínio, value objects, enums e interfaces puras.

**Conteúdo**:
- **Entidades**: `Rps`, `RpsItem`, `RpsBatch`, `Nfe`, `NfeKey`, `RpsKey`, `ServiceProvider`, `ServiceCustomer`, `NfeCancellation`
- **Value Objects**: `Cnpj`, `Cpf`, `CpfCnpj`, `Money`, `Aliquota`, `Address`
- **Enums**: `IssRetido`, `TipoTributacao`, `TipoRps`, `StatusRps`
- **Interfaces**: `IRpsSignatureService`, `INfeCancellationSignatureService`, `ICertificateProvider`

**Características**:
- Sem dependências externas (apenas BCL - Base Class Library)
- Regras de negócio puras
- Imutabilidade onde aplicável

### 2. Microled.Nfe.Service.Business

**Responsabilidade**: Implementação de serviços de domínio e regras de negócio.

**Conteúdo**:
- **Serviços de Assinatura Digital**:
  - `RpsSignatureService`: Gera string de assinatura de 85 caracteres e assina RPS com SHA1+RSA
  - `NfeCancellationSignatureService`: Gera string de assinatura de 20 caracteres e assina cancelamento

**Características**:
- Depende apenas de `Domain`
- Implementa lógica de assinatura conforme especificação oficial (NFe_Web_Service-4.pdf)
- Não acessa infraestrutura diretamente

### 3. Microled.Nfe.Service.Application

**Responsabilidade**: Casos de uso, DTOs, validações e orquestração de aplicação.

**Conteúdo**:
- **DTOs de Request**: `SendRpsRequestDto`, `ConsultNfeRequestDto`, `CancelNfeRequestDto`
- **DTOs de Response**: `SendRpsResponseDto`, `ConsultNfeResponseDto`, `CancelNfeResponseDto`
- **Casos de Uso**: `SendRpsUseCase`, `ConsultNfeUseCase`, `CancelNfeUseCase`
- **Validadores**: FluentValidation para validação de entrada
- **Interfaces**: `INfeGateway`, `ISendRpsUseCase`, `IConsultNfeUseCase`, `ICancelNfeUseCase`

**Características**:
- Depende de `Domain` e `Business`
- Orquestra o fluxo de negócio
- Valida entrada e formata saída

### 4. Microled.Nfe.Service.Infra

**Responsabilidade**: Integração com Web Service da Prefeitura e infraestrutura.

**Conteúdo**:
- **Cliente SOAP**: `NfeSoapClient` (implementa `INfeGateway`)
  - Montagem de envelope SOAP
  - Serialização/desserialização XML
  - Chamadas HTTP ao Web Service
  - Tratamento de erros (SOAP Faults, HTTP errors)
- **Serialização XML**: `XmlSerializerService` usando `System.Xml.Serialization`
- **Certificados**: `CertificateProvider` (suporta A1 e A3)
- **Configuração**: `NfeServiceOptions`, `CertificateOptions`
- **XSD Classes**: Classes geradas a partir dos XSDs oficiais em `XmlSchemas/`
- **Fake Gateway**: `FakeNfeGateway` para desenvolvimento/testes

**Características**:
- Depende de `Domain`, `Business` e `Application`
- Implementa detalhes técnicos (SOAP, XML, HTTP)
- Isolado do domínio de negócio

### 5. Microled.Nfe.Service.Api

**Responsabilidade**: Camada de apresentação (REST API).

**Conteúdo**:
- **Controllers**: `RpsController`, `NfeController`, `SandboxNfeController`
- **Middleware**: `GlobalExceptionHandlerMiddleware` para tratamento global de erros
- **Health Checks**: `NfeHealthCheck` para verificar configuração e certificado
- **Swagger/OpenAPI**: Documentação automática da API
- **DI**: Configuração de injeção de dependências

**Características**:
- Depende de `Application`
- Expõe endpoints REST
- Configuração de ambiente (Development, Homologation, Production)

### 6. Microled.Nfe.Service.Tests

**Responsabilidade**: Testes unitários da solução.

**Conteúdo**:
- **Testes de Assinatura**: `RpsSignatureServiceTests`, `NfeCancellationSignatureServiceTests`
- **Testes de SOAP Client**: `NfeSoapClientTests` com `HttpMessageHandler` fake
- **Testes de Mapeamento**: `NfeMappingTests`
- **Helpers**: `TestCertificateHelper` para criar certificados de teste

**Cobertura**: 32 testes cobrindo assinaturas, mapeamentos e cliente SOAP.

## 🔄 Fluxos Principais Implementados

### 1. Envio de Lote de RPS

**Fluxo**:
1. API recebe `SendRpsRequestDto` via `POST /api/v1/rps/send`
2. `SendRpsUseCase` valida e processa:
   - Mapeia DTOs para entidades de domínio
   - Para cada RPS: gera assinatura digital usando `RpsSignatureService`
   - Cria `RpsBatch` com RPS assinados
3. `NfeSoapClient` (via `INfeGateway`):
   - Mapeia entidades para classes XSD (`PedidoEnvioLoteRPS`)
   - Serializa para XML
   - Monta envelope SOAP
   - Envia HTTP POST para Web Service
   - Extrai e desserializa resposta (`RetornoEnvioLoteRPS`)
   - Mapeia para `SendRpsResponseDto`
4. API retorna resposta com protocolo e chaves das NF-e geradas

**Limite**: Até 50 RPS por lote (conforme especificação da Prefeitura)

### 2. Consulta de NF-e

**Fluxo**:
1. API recebe `ConsultNfeRequestDto` via `POST /api/v1/nfe/consult`
2. `ConsultNfeUseCase` processa:
   - Valida que `ChaveNFe` ou `ChaveRps` foi informado
   - Cria `ConsultNfeCriteria`
3. `NfeSoapClient`:
   - Mapeia para `PedidoConsultaNFe`
   - Serializa, envia SOAP e recebe `RetornoConsulta`
   - Mapeia para `ConsultNfeResponseDto`
4. API retorna informações da NF-e

### 3. Cancelamento de NF-e

**Fluxo**:
1. API recebe `CancelNfeRequestDto` via `POST /api/v1/nfe/cancel`
2. `CancelNfeUseCase` processa:
   - Cria `NfeKey` a partir do DTO
   - Gera assinatura de cancelamento usando `NfeCancellationSignatureService`
   - Cria `NfeCancellation` com assinatura
3. `NfeSoapClient`:
   - Mapeia para `PedidoCancelamentoNFe`
   - Serializa, envia SOAP e recebe `RetornoCancelamentoNFe`
   - Mapeia para `CancelNfeResponseDto`
4. API retorna status do cancelamento

## 📦 Requisitos

### Software

- **.NET 8 SDK** ou superior
- **Certificado Digital A1** (arquivo PFX) ou **A3** (token/cartão)
- **Acesso à internet** para comunicação com Web Service da Prefeitura

### URLs do Web Service

- **Homologação**: `https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx`
- **Produção**: `https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx`

### Permissões

- **Certificado A1 (File)**: Acesso de leitura ao arquivo PFX
- **Certificado A3 (Store)**: Acesso ao Windows Certificate Store (CurrentUser ou LocalMachine)

### Dados Necessários

- **CNPJ do Prestador** (DefaultIssuerCnpj)
- **Inscrição Municipal** (DefaultIssuerIm)
- **CNPJ do Remetente** (DefaultCnpjRemetente) - geralmente igual ao CNPJ do prestador

## ⚙️ Configuração

### appsettings.json

```json
{
  "NfeService": {
    "BaseUrl": "https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "TimeoutSeconds": 60,
    "Environment": "Homologation",
    "DefaultIssuerCnpj": "12345678000190",
    "DefaultIssuerIm": "12345678",
    "DefaultCnpjRemetente": "12345678000190",
    "SchemaVersion": "2.0",
    "Versao": "2",
    "LogRawXml": false,
    "LogSensitiveData": false,
    "Certificate": {
      "Mode": "File",
      "FilePath": "C:\\certs\\nfe-sp-homolog.pfx",
      "Password": "SUA_SENHA",
      "StoreLocation": "CurrentUser",
      "StoreName": "My",
      "Thumbprint": ""
    }
  },
  "Features": {
    "UseFakeGateway": false
  }
}
```

### Propriedades de NfeServiceOptions

| Propriedade | Tipo | Descrição | Obrigatório |
|------------|------|-----------|-------------|
| `BaseUrl` | string | URL do Web Service (prioridade sobre ProductionEndpoint/TestEndpoint) | Sim* |
| `ProductionEndpoint` | string | URL de produção (fallback se BaseUrl não configurado) | Sim* |
| `TestEndpoint` | string | URL de homologação (fallback se BaseUrl não configurado) | Sim* |
| `UseProduction` | bool | Usa produção se BaseUrl não configurado | Não |
| `TimeoutSeconds` | int | Timeout em segundos para chamadas HTTP | Não (padrão: 60) |
| `Environment` | string | Ambiente atual (Development, Homologation, Production) | Não |
| `DefaultIssuerCnpj` | string | CNPJ do prestador | Sim |
| `DefaultIssuerIm` | string | Inscrição Municipal do prestador | Sim |
| `DefaultCnpjRemetente` | string | CNPJ do remetente | Sim |
| `SchemaVersion` | string | Versão do schema (ex: "2.0") | Não (padrão: "2.0") |
| `Versao` | string | Versão para campo Versao (ex: "2") | Não (padrão: "2") |
| `LogRawXml` | bool | Habilita logging de XML completo | Não (padrão: false) |
| `LogSensitiveData` | bool | Habilita logging de dados sensíveis (só funciona se LogRawXml=true) | Não (padrão: false) |

\* Pelo menos um dos três (BaseUrl, ProductionEndpoint ou TestEndpoint) deve estar configurado.

### CertificateOptions

#### Modo File (Certificado A1 - Arquivo PFX)

```json
"Certificate": {
  "Mode": "File",
  "FilePath": "C:\\certs\\nfe-sp-homolog.pfx",
  "Password": "SUA_SENHA"
}
```

#### Modo Store (Certificado A3 - Windows Certificate Store)

```json
"Certificate": {
  "Mode": "Store",
  "StoreLocation": "CurrentUser",
  "StoreName": "My",
  "Thumbprint": "ABCDEF1234567890ABCDEF1234567890ABCDEF12"
}
```

**StoreLocation**: `CurrentUser` ou `LocalMachine`  
**StoreName**: `My`, `TrustedPeople`, etc.

### Feature Flags

#### UseFakeGateway

```json
"Features": {
  "UseFakeGateway": true
}
```

Quando `true`, usa `FakeNfeGateway` que retorna respostas simuladas sem chamar o Web Service real. Útil para:
- Desenvolvimento local sem certificado
- Testes de integração
- Demonstrações

### Logging de XML

#### LogRawXml: false (padrão)
Apenas logs de tamanho, protocolos e identificadores (sem XML completo).

#### LogRawXml: true, LogSensitiveData: false
Logs de XML completo com dados sensíveis mascarados:
- CNPJ: `1234****5678`
- CPF: `123****45`
- Valores: `***.**`
- Assinaturas: `***BASE64_SIGNATURE***`

#### LogRawXml: true, LogSensitiveData: true
Logs de XML completo sem mascaramento (use com cuidado em produção).

## 🌐 Endpoints Principais

### POST /api/v1/rps/send

Envia um lote de RPS para conversão em NFS-e.

**Request Body** (`SendRpsRequestDto`):
```json
{
  "prestador": {
    "cpfCnpj": "12345678000190",
    "inscricaoMunicipal": 12345678,
    "razaoSocial": "Empresa Exemplo Ltda",
    "endereco": { ... },
    "email": "contato@exemplo.com.br"
  },
  "rpsList": [
    {
      "inscricaoPrestador": 12345678,
      "serieRps": "A",
      "numeroRps": 1,
      "tipoRPS": "RPS",
      "dataEmissao": "2024-01-15",
      "statusRPS": "N",
      "tributacaoRPS": "T",
      "item": {
        "codigoServico": 1234,
        "discriminacao": "Serviços de consultoria em TI",
        "valorServicos": 1000.00,
        "valorDeducoes": 0.00,
        "aliquotaServicos": 0.05,
        "issRetido": false
      },
      "tomador": { ... }
    }
  ],
  "dataInicio": "2024-01-01",
  "dataFim": "2024-01-31",
  "transacao": true
}
```

**Response** (`SendRpsResponseDto`):
```json
{
  "sucesso": true,
  "protocolo": "123456789012345",
  "chavesNFeRPS": [
    {
      "chaveNFe": {
        "inscricaoPrestador": 12345678,
        "numeroNFe": 987654321,
        "codigoVerificacao": "ABCD1234",
        "chaveNotaNacional": "..."
      },
      "chaveRPS": {
        "inscricaoPrestador": 12345678,
        "serieRps": "A",
        "numeroRps": 1
      }
    }
  ],
  "alertas": [],
  "erros": []
}
```

**Status Codes**:
- `200 OK`: Lote enviado com sucesso
- `400 Bad Request`: Dados inválidos (validação)
- `500 Internal Server Error`: Erro interno ou erro do Web Service

### POST /api/v1/nfe/consult

Consulta uma NF-e por chave da NF-e ou chave do RPS.

**Request Body** (`ConsultNfeRequestDto`):
```json
{
  "chaveNFe": {
    "inscricaoPrestador": 12345678,
    "numeroNFe": 987654321,
    "codigoVerificacao": "ABCD1234",
    "chaveNotaNacional": "..."
  }
}
```

Ou por chave do RPS:
```json
{
  "chaveRps": {
    "inscricaoPrestador": 12345678,
    "serieRps": "A",
    "numeroRps": 1
  }
}
```

**Response** (`ConsultNfeResponseDto`):
```json
{
  "sucesso": true,
  "nfeList": [
    {
      "chaveNFe": { ... },
      "dataEmissaoNFe": "2024-01-15T10:00:00",
      "dataFatoGeradorNFe": "2024-01-15T10:00:00",
      "statusNFe": "N",
      "valorServicos": 1000.00,
      "valorDeducoes": 0.00,
      "valorISS": 50.00,
      ...
    }
  ],
  "alertas": [],
  "erros": []
}
```

### POST /api/v1/nfe/cancel

Cancela uma NF-e emitida.

**Request Body** (`CancelNfeRequestDto`):
```json
{
  "chaveNFe": {
    "inscricaoPrestador": 12345678,
    "numeroNFe": 987654321,
    "codigoVerificacao": "ABCD1234",
    "chaveNotaNacional": "..."
  },
  "transacao": true
}
```

**Response** (`CancelNfeResponseDto`):
```json
{
  "sucesso": true,
  "alertas": [],
  "erros": []
}
```

### Endpoints de Sandbox

Para testes rápidos sem montar DTOs complexos:

- **POST /api/v1/sandbox/nfe/rps/send-sample**: Envia RPS de teste usando dados padrão
- **POST /api/v1/sandbox/nfe/consult-sample?numeroNFe=XXX**: Consulta NF-e de teste
- **POST /api/v1/sandbox/nfe/cancel-sample?numeroNFe=XXX**: Cancela NF-e de teste

### Health Check

- **GET /health/nfe**: Verifica configuração e certificado

Retorna `200 OK` se tudo estiver configurado corretamente, ou `503 Service Unavailable` com detalhes do problema.

## 🚀 Execução

### Rodar Localmente (com Fake Gateway)

1. Configure `appsettings.Development.json`:
```json
{
  "Features": {
    "UseFakeGateway": true
  }
}
```

2. Execute:
```bash
cd Microled.Nfe.Service.Api
dotnet run
```

3. Acesse Swagger: `http://localhost:5000` ou `https://localhost:5001`

### Apontar para Homologação

1. Configure `appsettings.Homologation.json` (veja seção Configuração)

2. Execute com ambiente Homologation:
```bash
ASPNETCORE_ENVIRONMENT=Homologation dotnet run
```

Ou configure no `launchSettings.json`:
```json
"Homologation": {
  "commandName": "Project",
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Homologation"
  }
}
```

3. Verifique o health check:
```bash
curl http://localhost:5000/health/nfe
```

4. Teste com sandbox:
```bash
curl -X POST http://localhost:5000/api/v1/sandbox/nfe/rps/send-sample
```

### Rodar Testes

```bash
# Todos os testes
dotnet test

# Testes de uma classe específica
dotnet test --filter "FullyQualifiedName~RpsSignatureServiceTests"

# Com output detalhado
dotnet test --logger "console;verbosity=detailed"
```

## 🧪 Testes

### Cobertura

A solução possui **32 testes unitários** cobrindo:

- **Assinatura Digital** (20 testes):
  - `RpsSignatureServiceTests`: 11 testes
  - `NfeCancellationSignatureServiceTests`: 9 testes
- **Cliente SOAP** (5 testes):
  - `NfeSoapClientTests`: Cenários de sucesso, SOAP Faults, erros HTTP
- **Mapeamentos** (7 testes):
  - `NfeMappingTests`: Documentação de mapeamentos

### Principais Cenários Cobertos

✅ String de assinatura RPS (85 caracteres)  
✅ String de assinatura cancelamento (20 caracteres)  
✅ Padding de campos (zeros à esquerda, espaços à direita)  
✅ Assinatura com certificado válido  
✅ Tratamento de certificado sem chave privada  
✅ SOAP client com respostas simuladas  
✅ Tratamento de SOAP Faults  
✅ Tratamento de erros HTTP  

### Interpretando Resultados

```bash
Test summary: total: 32, failed: 0, succeeded: 32, skipped: 0
```

- **succeeded**: Testes que passaram
- **failed**: Testes que falharam (ver stack trace)
- **skipped**: Testes ignorados (com `[Fact(Skip = "...")]`)

## 📝 TODO / Roadmap

### Implementação Pendente

1. **Assinatura XML-DSig Completa**
   - Implementar elemento `<Signature>` completo conforme `xmldsig-core-schema_v02.xsd`
   - Atualmente apenas assinatura de string (SHA1+RSA+Base64) está implementada

2. **Métodos Assíncronos do Web Service**
   - `EnvioLoteRpsAsync`: Envio assíncrono de lote
   - `ConsultaSituacaoLote`: Consulta situação do lote enviado
   - `ConsultaNFSePeriodo`: Consulta por período
   - Outros métodos assíncronos conforme documentação

3. **Persistência e Auditoria**
   - Banco de dados para armazenar NF-e emitidas
   - Logs de integração (XMLs enviados/recebidos)
   - Histórico de alterações (cancelamentos, etc.)

4. **Melhorias de Infraestrutura**
   - Retry policies para chamadas ao Web Service
   - Circuit breaker para resiliência
   - Cache de certificados
   - Métricas e monitoramento (Application Insights, Prometheus)

5. **Validações Adicionais**
   - Validação de CPF/CNPJ com dígitos verificadores
   - Validação de CEP
   - Validação de códigos de serviço (tabela oficial)

6. **Documentação**
   - Exemplos de integração com diferentes ERPs
   - Guia de troubleshooting avançado
   - Diagramas de sequência dos fluxos

## 📚 Documentação Adicional

- **Especificação Oficial**: `Documentacao/NFe_Web_Service-4.pdf`
- **XSDs**: `XSD/schemas-reformatributaria-v02-3/`
- **Configuração de Homologação**: `Microled.Nfe.Service.Api/HOMOLOGATION_SETUP.md`
- **Exemplos Práticos**: `EXAMPLES.md` (neste repositório)

## 🛠️ Tecnologias Utilizadas

- **.NET 8**: Framework principal
- **ASP.NET Core Web API**: API REST
- **FluentValidation**: Validação de DTOs
- **xUnit**: Framework de testes
- **Moq**: Mocking para testes
- **FluentAssertions**: Assertions legíveis
- **Swagger/OpenAPI**: Documentação da API
- **System.Security.Cryptography**: Assinatura digital (SHA1+RSA)
- **System.Xml.Serialization**: Serialização XML

## 📄 Licença

Este projeto é proprietário da Microled.

## 🤝 Suporte

Para dúvidas ou problemas:
1. Consulte a documentação oficial (`NFe_Web_Service-4.pdf`)
2. Verifique os logs da aplicação
3. Execute o health check (`GET /health/nfe`)
4. Consulte `EXAMPLES.md` para exemplos práticos
