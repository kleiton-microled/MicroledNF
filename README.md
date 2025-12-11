# Microled NFS-e Service

Solução .NET 8 Web API para emissão de NFS-e da Prefeitura de São Paulo (Nota do Milhão), baseada na documentação e XSDs oficiais.

## Estrutura da Solução

A solução segue os princípios de Clean Architecture e está organizada em 5 projetos:

### 1. Microled.Nfe.Service.Domain
**Responsabilidade**: Entidades de domínio, value objects e regras puras.

**Conteúdo**:
- **Value Objects**: `Cnpj`, `Cpf`, `CpfCnpj`, `Money`, `Aliquota`
- **Entidades**: `Rps`, `RpsItem`, `Nfe`, `NfeKey`, `RpsKey`, `ServiceProvider`, `ServiceCustomer`, `NfeCancellation`, `RpsBatch`
- **Enums**: `IssRetido`, `TipoTributacao`, `TipoRps`, `StatusRps`
- **Interfaces**: `IRpsSignatureService`, `INfeCancellationSignatureService`

### 2. Microled.Nfe.Service.Business
**Responsabilidade**: Regras de negócio da NFS-e, orquestração de domínio.

**Conteúdo**:
- **Serviços de Assinatura**: `RpsSignatureService`, `NfeCancellationSignatureService`
- Implementação da montagem da string de assinatura conforme especificação oficial

### 3. Microled.Nfe.Service.Application
**Responsabilidade**: Casos de uso e orquestração de aplicação.

**Conteúdo**:
- **DTOs**: `SendRpsRequestDto`, `SendRpsResponseDto`, `ConsultNfeRequestDto`, `ConsultNfeResponseDto`, `CancelNfeRequestDto`, `CancelNfeResponseDto`
- **Casos de Uso**: `SendRpsUseCase`, `ConsultNfeUseCase`, `CancelNfeUseCase`
- **Validadores**: FluentValidation para validação de DTOs
- **Interfaces**: `INfeGateway`, `ISendRpsUseCase`, `IConsultNfeUseCase`, `ICancelNfeUseCase`

### 4. Microled.Nfe.Service.Infra
**Responsabilidade**: Integração com o Web Service da Prefeitura e infraestrutura.

**Conteúdo**:
- **Cliente SOAP**: `NfeSoapClient` (implementa `INfeGateway`)
- **Serialização XML**: `XmlSerializerService`
- **Certificados**: `CertificateProvider`
- **Configuração**: `NfeServiceOptions`
- **XSD Classes**: Pasta `XmlSchemas` para classes geradas a partir dos XSDs

### 5. Microled.Nfe.Service.Api
**Responsabilidade**: Camada de apresentação (REST API).

**Conteúdo**:
- **Controllers**: `RpsController`, `NfeController`
- **Middleware**: `GlobalExceptionHandlerMiddleware`
- **Swagger/OpenAPI**: Configuração completa
- **DI**: Configuração de injeção de dependências

## Funcionalidades Implementadas

### Serviços Síncronos

1. **Envio de Lote de RPS** (`POST /api/v1/rps/send`)
   - Envia um lote de até 50 RPS para conversão em NFS-e
   - Retorna protocolo e chaves das NF-e geradas

2. **Consulta de NF-e** (`POST /api/v1/nfe/consult`)
   - Consulta NF-e por chave da NF-e ou chave do RPS
   - Retorna informações completas da NF-e

3. **Cancelamento de NF-e** (`POST /api/v1/nfe/cancel`)
   - Cancela uma NF-e emitida
   - Retorna status do cancelamento

## Configuração

### appsettings.json

```json
{
  "NfeService": {
    "ProductionEndpoint": "https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "TestEndpoint": "https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "UseProduction": false,
    "Versao": "2",
    "TimeoutSeconds": 60,
    "DefaultCnpjRemetente": "",
    "Certificate": {
      "FilePath": "caminho/para/certificado.pfx",
      "Password": "senha_do_certificado",
      "StoreLocation": "CurrentUser",
      "StoreName": "My",
      "Thumbprint": ""
    }
  }
}
```

### Certificado Digital

O sistema suporta dois tipos de certificado:

1. **Certificado A1 (Arquivo)**: Configure `Certificate.FilePath` e `Certificate.Password`
2. **Certificado A3 (Token/Store)**: Configure `Certificate.Thumbprint`, `Certificate.StoreLocation` e `Certificate.StoreName`

## Geração de Classes a partir dos XSDs

As classes C# a partir dos XSDs devem ser geradas e colocadas na pasta `Microled.Nfe.Service.Infra/XmlSchemas`.

### Opção 1: Usando xsd.exe (Windows SDK)

```bash
cd XSD/schemas-reformatributaria-v02-3
xsd.exe TiposNFe_v02.xsd /c /n:Microled.Nfe.Service.Infra.XmlSchemas
xsd.exe PedidoEnvioLoteRPS_v02.xsd /c /n:Microled.Nfe.Service.Infra.XmlSchemas
xsd.exe RetornoEnvioLoteRPS_v02.xsd /c /n:Microled.Nfe.Service.Infra.XmlSchemas
xsd.exe PedidoConsultaNFe_v02.xsd /c /n:Microled.Nfe.Service.Infra.XmlSchemas
xsd.exe RetornoConsulta_v02.xsd /c /n:Microled.Nfe.Service.Infra.XmlSchemas
xsd.exe PedidoCancelamentoNFe_v02.xsd /c /n:Microled.Nfe.Service.Infra.XmlSchemas
xsd.exe RetornoCancelamentoNFe_v02.xsd /c /n:Microled.Nfe.Service.Infra.XmlSchemas
```

### Opção 2: Usando dotnet-xscgen

```bash
dotnet tool install -g dotnet-xscgen
cd XSD/schemas-reformatributaria-v02-3
dotnet xscgen -n:Microled.Nfe.Service.Infra.XmlSchemas -o:../../Microled.Nfe.Service.Infra/XmlSchemas *.xsd
```

## TODOs e Próximos Passos

### Implementação Pendente

1. **Geração de Classes XSD**: Gerar classes C# a partir dos XSDs oficiais
2. **Assinatura Digital**: Implementar assinatura SHA1 + RSA com certificado digital
3. **Cliente SOAP**: Completar implementação do `NfeSoapClient` com:
   - Montagem do envelope SOAP
   - Serialização do XML do pedido
   - Chamada HTTP ao Web Service
   - Desserialização do XML de retorno
4. **Mapeamento Completo**: Mapear todos os campos entre entidades de domínio e classes XSD

### Funcionalidades Futuras

- Suporte a métodos assíncronos (`EnvioLoteRpsAsync`, `ConsultaSituacaoLote`, etc.)
- Armazenamento local de logs de XML
- Auditoria e persistência de NF-e no banco de dados
- Cache de certificados
- Retry policies para chamadas ao Web Service

## Executando a Aplicação

```bash
cd Microled.Nfe.Service.Api
dotnet run
```

A API estará disponível em:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `https://localhost:5001/swagger`

## Documentação

- **PDF**: `Documentacao/NFe_Web_Service-4.pdf`
- **XSDs**: `XSD/schemas-reformatributaria-v02-3/`

## Tecnologias Utilizadas

- .NET 8
- ASP.NET Core Web API
- FluentValidation
- System.Security.Cryptography.Xml
- Swagger/OpenAPI

## Arquitetura

A solução segue os princípios de Clean Architecture:

```
Domain (sem dependências)
    ↑
Business (depende apenas de Domain)
    ↑
Application (depende de Domain e Business)
    ↑
Infra (depende de Domain, Business e Application)
    ↑
Api (depende de Application)
```

## Licença

Este projeto é proprietário da Microled.

