# Testes Unitários - Microled.Nfe.Service

## Estrutura do Projeto de Testes

O projeto `Microled.Nfe.Service.Tests` contém testes unitários para a solução, organizados por camada:

- **Business**: Testes dos serviços de assinatura digital
- **Infra/Client**: Testes do cliente SOAP com HttpMessageHandler fake
- **Infra/Mapping**: Testes de mapeamento (documentação)
- **Helpers**: Helpers para criação de certificados de teste

## Frameworks e Bibliotecas

- **xUnit**: Framework de testes
- **Moq**: Mocking de dependências
- **FluentAssertions**: Assertions mais legíveis

## Testes Implementados

### 1. RpsSignatureServiceTests

**Localização**: `Business/RpsSignatureServiceTests.cs`

**Cenários testados**:
- ✅ `BuildSignatureString_ShouldReturn85Characters_WhenValidRps` - Valida tamanho de 85 caracteres
- ✅ `BuildSignatureString_ShouldHaveCorrectFormat_WhenValidRps` - Valida formato e posições dos campos
- ✅ `BuildSignatureString_ShouldPadInscricaoMunicipalWithZeros` - Padding de Inscrição Municipal
- ✅ `BuildSignatureString_ShouldPadNumeroRpsWithZeros` - Padding de Número RPS
- ✅ `BuildSignatureString_ShouldHandleTomadorWithoutCpfCnpj` - Tomador sem CPF/CNPJ
- ✅ `SignRps_ShouldReturnBase64Signature_WhenValidCertificate` - Assinatura com certificado válido
- ✅ `SignRps_ShouldThrowException_WhenCertificateHasNoPrivateKey` - Certificado sem chave privada
- ✅ `SignRps_ShouldThrowException_WhenCertificateIsNull` - Certificado nulo
- ✅ `BuildSignatureString_ShouldThrowException_WhenRpsIsNull` - RPS nulo
- ✅ `BuildSignatureString_ShouldThrowException_WhenPrestadorIsNull` - Prestador nulo (validação no construtor)
- ✅ `BuildSignatureString_ShouldThrowException_WhenItemIsNull` - Item nulo (validação no construtor)

**Nota**: A string de assinatura tem **85 caracteres**, não 86. O cálculo correto é:
8 + 5 + 12 + 8 + 1 + 1 + 1 + 15 + 15 + 5 + 14 = 85

### 2. NfeCancellationSignatureServiceTests

**Localização**: `Business/NfeCancellationSignatureServiceTests.cs`

**Cenários testados**:
- ✅ `BuildSignatureString_ShouldReturn20Characters_WhenValidNfeKey` - Valida tamanho de 20 caracteres
- ✅ `BuildSignatureString_ShouldHaveCorrectFormat_WhenValidNfeKey` - Valida formato
- ✅ `BuildSignatureString_ShouldPadInscricaoMunicipalWithZeros` - Padding de Inscrição Municipal
- ✅ `BuildSignatureString_ShouldPadNumeroNFeWithZeros` - Padding de Número NFe
- ✅ `SignCancellation_ShouldReturnBase64Signature_WhenValidCertificate` - Assinatura com certificado válido
- ✅ `SignCancellation_ShouldThrowException_WhenCertificateHasNoPrivateKey` - Certificado sem chave privada
- ✅ `SignCancellation_ShouldThrowException_WhenCertificateIsNull` - Certificado nulo
- ✅ `BuildSignatureString_ShouldThrowException_WhenNfeKeyIsNull` - NfeKey nulo
- ✅ `SignCancellation_ShouldGenerateDifferentSignatures_ForDifferentNfeKeys` - Assinaturas diferentes para chaves diferentes

### 3. NfeSoapClientTests

**Localização**: `Infra/Client/NfeSoapClientTests.cs`

**Cenários testados**:
- ✅ `SendRpsBatchAsync_ShouldReturnSuccessResult_WhenSoapResponseIsSuccess` - Envio bem-sucedido
- ✅ `SendRpsBatchAsync_ShouldThrowNfeSoapException_WhenSoapFault` - SOAP Fault
- ✅ `SendRpsBatchAsync_ShouldThrowNfeSoapException_WhenHttpError` - Erro HTTP (500)
- ✅ `ConsultNfeAsync_ShouldReturnSuccessResult_WhenSoapResponseIsSuccess` - Consulta bem-sucedida
- ✅ `CancelNfeAsync_ShouldReturnSuccessResult_WhenSoapResponseIsSuccess` - Cancelamento bem-sucedido

**FakeHttpMessageHandler**: Handler customizado para simular respostas HTTP sem chamar o serviço real.

### 4. NfeMappingTests

**Localização**: `Infra/Mapping/NfeMappingTests.cs`

**Nota**: Os métodos de mapeamento são privados no `NfeSoapClient`, então estes testes servem principalmente como documentação. Os mapeamentos são testados indiretamente através dos testes de integração do `NfeSoapClient`.

## Helpers

### TestCertificateHelper

**Localização**: `Helpers/TestCertificateHelper.cs`

- `CreateTestCertificateWithPrivateKey()`: Cria certificado auto-assinado com chave privada
- `CreateTestCertificateWithoutPrivateKey()`: Cria certificado sem chave privada (apenas chave pública)

## Executando os Testes

```bash
# Executar todos os testes
dotnet test

# Executar testes de uma classe específica
dotnet test --filter "FullyQualifiedName~RpsSignatureServiceTests"

# Executar com output detalhado
dotnet test --logger "console;verbosity=detailed"
```

## Cobertura de Testes

### Serviços de Assinatura
- ✅ Cobertura completa de `RpsSignatureService`
- ✅ Cobertura completa de `NfeCancellationSignatureService`
- ✅ Validação de formatos e padding
- ✅ Tratamento de erros

### Cliente SOAP
- ✅ Cenários de sucesso
- ✅ Tratamento de SOAP Faults
- ✅ Tratamento de erros HTTP
- ✅ Mapeamento de respostas

### Mapeamentos
- ⚠️ Testes indiretos através de integração (métodos são privados)

## Próximos Passos

- [ ] Adicionar testes de integração end-to-end
- [ ] Extrair métodos de mapeamento para classe pública para testes diretos
- [ ] Adicionar testes de performance
- [ ] Adicionar testes de edge cases (valores muito grandes, caracteres especiais, etc.)

