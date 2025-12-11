# Implementação de Assinatura Digital

## Resumo

Implementação completa dos serviços de assinatura digital para RPS e cancelamento de NF-e, conforme especificação da Prefeitura de São Paulo (NFe_Web_Service-4.pdf).

## Serviços Implementados

### 1. RpsSignatureService

**Localização:** `Microled.Nfe.Service.Business/Services/RpsSignatureService.cs`

**Funcionalidades:**
- `BuildSignatureString(Rps rps)`: Monta a string de assinatura de 86 caracteres conforme especificação
- `SignRps(Rps rps, X509Certificate2 certificate)`: Assina o RPS usando SHA1 + RSA e retorna Base64

**Formato da String de Assinatura (86 caracteres):**
1. Inscrição Municipal (8) - zeros à esquerda
2. Série RPS (5) - espaços à direita
3. Número RPS (12) - zeros à esquerda
4. Data Emissão (8) - formato AAAAMMDD
5. Tipo Tributação (1) - T, F, I ou J
6. Status RPS (1) - N, C ou E
7. ISS Retido (1) - S ou N
8. Valor Serviços (15) - centavos, sem separadores
9. Valor Deduções (15) - centavos, sem separadores
10. Código Serviço (5) - zeros à esquerda
11. CPF/CNPJ Tomador (14) - zeros à esquerda

**Referência:** NFe_Web_Service-4.pdf - Seção "Campos para assinatura do RPS – versão 2.0"

### 2. NfeCancellationSignatureService

**Localização:** `Microled.Nfe.Service.Business/Services/NfeCancellationSignatureService.cs`

**Funcionalidades:**
- `BuildSignatureString(NfeKey nfeKey)`: Monta a string de assinatura de 20 caracteres
- `SignCancellation(NfeKey nfeKey, X509Certificate2 certificate)`: Assina o cancelamento usando SHA1 + RSA e retorna Base64

**Formato da String de Assinatura (20 caracteres):**
1. Inscrição Municipal (8) - zeros à esquerda
2. Número NFe (12) - zeros à esquerda

**Referência:** NFe_Web_Service-4.pdf - Seção "Campos para assinatura de cancelamento"

## Algoritmo de Assinatura

- **Algoritmo:** SHA1 com RSA
- **Padding:** PKCS1
- **Encoding:** ASCII para a string de dados
- **Resultado:** Base64 da assinatura

**Implementação:**
```csharp
var rsa = certificate.GetRSAPrivateKey();
var dataToSign = Encoding.ASCII.GetBytes(signatureString);
var signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
var base64Signature = Convert.ToBase64String(signature);
```

## Integração com Casos de Uso

### SendRpsUseCase
- Obtém o certificado via `ICertificateProvider`
- Para cada RPS no lote:
  - Gera a assinatura usando `IRpsSignatureService.SignRps()`
  - Armazena a assinatura no RPS via `Rps.SetAssinatura()`
- O `NfeSoapClient` utilizará a assinatura ao mapear para XML

### CancelNfeUseCase
- Obtém o certificado via `ICertificateProvider`
- Gera a assinatura usando `INfeCancellationSignatureService.SignCancellation()`
- Cria `NfeCancellation` com a assinatura
- O `NfeSoapClient` utilizará a assinatura ao mapear para XML

## Validações e Tratamento de Erros

- **Certificado sem chave privada:** Lança `InvalidOperationException` com mensagem clara
- **Campos obrigatórios ausentes:** Validações em `BuildSignatureString()` com `ArgumentNullException` ou `ArgumentException`
- **Tamanho da string incorreto:** Validação para garantir 86 caracteres (RPS) ou 20 caracteres (Cancelamento)
- **Logging:** Logs de sucesso e erro (sem expor dados sensíveis)

## Arquitetura

- **Interfaces:** `IRpsSignatureService` e `INfeCancellationSignatureService` no Domain
- **Implementações:** `RpsSignatureService` e `NfeCancellationSignatureService` no Business
- **Certificado:** `ICertificateProvider` no Domain (implementado no Infra)
- **Dependências:** Business depende apenas de Domain (Clean Architecture)

## Próximos Passos

- [ ] Integrar assinaturas no mapeamento XML em `NfeSoapClient`
- [ ] Implementar assinatura XML-DSig completa (elemento `<Signature>`) se necessário
- [ ] Testes unitários para validação das strings de assinatura
- [ ] Testes de integração com certificados reais

