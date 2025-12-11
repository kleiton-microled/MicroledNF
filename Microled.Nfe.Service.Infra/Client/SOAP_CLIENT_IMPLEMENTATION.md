# Implementação do Cliente SOAP (NfeSoapClient)

## Resumo

Implementação completa do cliente SOAP para comunicação com o Web Service da Prefeitura de São Paulo (Nota do Milhão), incluindo mapeamento entre entidades de domínio e classes XML geradas a partir dos XSDs.

## Arquitetura

### Dependências
- `HttpClient` - Para chamadas HTTP ao Web Service
- `IXmlSerializerService` - Para serialização/desserialização XML
- `IOptions<NfeServiceOptions>` - Para configuração (endpoints, timeouts, etc.)
- `ILogger<NfeSoapClient>` - Para logging

### Fluxo de Operação

1. **Mapeamento Domínio → XML**: Converte entidades de domínio (`RpsBatch`, `NfeCancellation`, etc.) para classes XML (`PedidoEnvioLoteRPS`, `PedidoCancelamentoNFe`, etc.)
2. **Serialização XML**: Serializa o objeto XML em string
3. **Montagem do Envelope SOAP**: Cria o envelope SOAP com o XML no elemento `MensagemXML`
4. **Envio HTTP**: Envia requisição POST ao endpoint configurado
5. **Extração da Resposta**: Extrai o XML do envelope SOAP de resposta
6. **Desserialização**: Converte o XML de resposta em objetos tipados
7. **Mapeamento XML → Domínio**: Converte objetos XML para entidades de domínio

## Métodos Implementados

### 1. SendRpsBatchAsync

**Fluxo:**
- Recebe `RpsBatch` (domínio) com RPS já assinados
- Mapeia para `PedidoEnvioLoteRPS`
- Serializa e envia via SOAP
- Processa `RetornoEnvioLoteRPS`
- Retorna `RetornoEnvioLoteRpsResult`

**Mapeamentos:**
- `RpsBatch` → `PedidoEnvioLoteRPS`
- Cada `Rps` → `tpRPS` (incluindo assinatura em Base64 convertida para byte[])
- `RetornoEnvioLoteRPS` → `RetornoEnvioLoteRpsResult`

### 2. ConsultNfeAsync

**Fluxo:**
- Recebe `ConsultNfeCriteria` (domínio)
- Mapeia para `PedidoConsultaNFe`
- Serializa e envia via SOAP
- Processa `RetornoConsulta`
- Retorna `ConsultaNfeResult`

**Mapeamentos:**
- `ConsultNfeCriteria` → `PedidoConsultaNFe`
- `RetornoConsulta` → `ConsultaNfeResult`
- `tpNFe` → `Nfe` (domínio)

### 3. CancelNfeAsync

**Fluxo:**
- Recebe `NfeCancellation` (domínio) com assinatura já gerada
- Mapeia para `PedidoCancelamentoNFe`
- Serializa e envia via SOAP
- Processa `RetornoCancelamentoNFe`
- Retorna `CancelNfeResult`

**Mapeamentos:**
- `NfeCancellation` → `PedidoCancelamentoNFe`
- Assinatura Base64 → byte[]
- `RetornoCancelamentoNFe` → `CancelNfeResult`

## Métodos Auxiliares

### BuildSoapEnvelope
- Monta o envelope SOAP padrão
- Namespace: `http://schemas.xmlsoap.org/soap/envelope/`
- Elemento de operação com namespace `http://www.prefeitura.sp.gov.br/nfe`
- XML serializado dentro de `<MensagemXML><![CDATA[...]]></MensagemXML>`

### ExtractXmlFromSoapResponse
- Extrai o XML do envelope SOAP de resposta
- Detecta e trata SOAP Faults
- Suporta conteúdo CDATA
- Lança `NfeSoapException` em caso de erro

### Métodos de Mapeamento

#### Domínio → XML
- `MapRpsBatchToPedidoEnvioLoteRPS`
- `MapRpsToTpRPS`
- `MapConsultNfeCriteriaToPedidoConsultaNFe`
- `MapNfeCancellationToPedidoCancelamentoNFe`
- `MapCpfCnpjToTpCPFCNPJ`
- `MapCpfCnpjToTpCPFCNPJNIF`
- `MapAddressToTpEndereco`
- `MapTipoRpsToString`

#### XML → Domínio
- `MapRetornoEnvioLoteRPSToResult`
- `MapRetornoConsultaToResult`
- `MapRetornoCancelamentoNFeToResult`
- `MapTpEventoToEvento`
- `MapTpChaveRPSToRpsKey`
- `MapTpChaveNFeToNfeKey`
- `MapTpNFeToNfe`

## Tratamento de Erros

### NfeSoapException
- Exceção customizada para erros SOAP
- Propriedades:
  - `FaultCode`: Código do SOAP Fault
  - `FaultString`: Mensagem do SOAP Fault
  - `FaultDetail`: Detalhes do SOAP Fault
  - `HttpStatusCode`: Código HTTP em caso de erro HTTP

### Cenários Tratados
- SOAP Faults (detectados e convertidos em `NfeSoapException`)
- Erros HTTP (status codes não-2xx)
- Erros de serialização/desserialização
- Timeouts de rede
- XML malformado na resposta

## Logging

### Pontos de Log
- Início/fim de cada operação
- Serialização (tamanho do XML)
- Envio HTTP (endpoint, status code)
- Resposta recebida (tamanho)
- Erros (com stack trace, sem dados sensíveis)
- Sucesso (protocolo, contadores)

### Níveis
- `Information`: Operações principais, sucesso
- `Debug`: Detalhes técnicos (tamanhos, serialização)
- `Error`: Erros e exceções

## Configuração

### NfeServiceOptions
- `ProductionEndpoint`: URL do ambiente de produção
- `TestEndpoint`: URL do ambiente de teste
- `UseProduction`: Flag para escolher ambiente
- `TimeoutSeconds`: Timeout das requisições HTTP
- `Versao`: Versão do schema (ex: "2")
- `DefaultCnpjRemetente`: CNPJ padrão do remetente (para consultas e cancelamentos)

## Observações Importantes

### Assinaturas
- RPS devem estar assinados antes de chamar `SendRpsBatchAsync`
- Cancelamentos devem ter assinatura gerada antes de chamar `CancelNfeAsync`
- Assinaturas são convertidas de Base64 (string) para byte[] para o XML

### IBSCBS
- Estrutura IBSCBS é criada com valores padrão
- **TODO**: Configurar valores reais de IBSCBS conforme necessidade

### Namespaces XML
- Namespaces são definidos nos atributos `[XmlRoot]` e `[XmlType]` das classes
- O `XmlSerializerService` detecta automaticamente os namespaces

### SOAP Actions
- `EnvioLoteRPS`: `http://www.prefeitura.sp.gov.br/nfe/EnvioLoteRPS`
- `ConsultaNFe`: `http://www.prefeitura.sp.gov.br/nfe/ConsultaNFe`
- `CancelamentoNFe`: `http://www.prefeitura.sp.gov.br/nfe/CancelamentoNFe`

## Próximos Passos

- [ ] Testes unitários para mapeamentos
- [ ] Testes de integração com ambiente de teste
- [ ] Configuração adequada de IBSCBS
- [ ] Implementação de XML-DSig completo (elemento `<Signature>`) se necessário
- [ ] Tratamento de retentativas em caso de falha temporária
- [ ] Cache de certificados se necessário

