# Exemplos Práticos - Microled NFS-e Service

Este documento contém exemplos práticos de uso da API Microled.Nfe.Service para integração com o Web Service da NFS-e São Paulo.

## 📋 Índice

1. [Envio de RPS](#envio-de-rps)
2. [Consulta de NF-e](#consulta-de-nf-e)
3. [Cancelamento de NF-e](#cancelamento-de-nf-e)
4. [Caso de Uso Completo: ERP Integrado](#caso-de-uso-completo-erp-integrado)
5. [Tratamento de Erros](#tratamento-de-erros)
6. [Exemplos com cURL](#exemplos-com-curl)
7. [Dicas e Boas Práticas](#dicas-e-boas-práticas)
8. [Certificado A3 com Pendrive (Token USB)](#certificado-a3-com-pendrive-token-usb)
9. [Referências](#referências)

---

## 1. Envio de RPS

### Request Completo

```json
POST /api/v1/rps/send
Content-Type: application/json

{
  "prestador": {
    "cpfCnpj": "12345678000190",
    "inscricaoMunicipal": 12345678,
    "razaoSocial": "EMPRESA PRESTADORA DE SERVIÇOS LTDA",
    "endereco": {
      "tipoLogradouro": "R",
      "logradouro": "Rua das Flores",
      "numero": "123",
      "complemento": "Sala 45",
      "bairro": "Centro",
      "codigoMunicipio": 3550308,
      "uf": "SP",
      "cep": 01000000
    },
    "email": "contato@prestador.com.br"
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
        "discriminacao": "Serviços de consultoria em tecnologia da informação, desenvolvimento de software e análise de sistemas",
        "valorServicos": 5000.00,
        "valorDeducoes": 0.00,
        "aliquotaServicos": 0.05,
        "issRetido": false
      },
      "tomador": {
        "cpfCnpj": "98765432000111",
        "inscricaoMunicipal": 98765432,
        "inscricaoEstadual": "123456789012",
        "razaoSocial": "TOMADOR DE SERVIÇOS S.A.",
        "endereco": {
          "tipoLogradouro": "AV",
          "logradouro": "Avenida Paulista",
          "numero": "1000",
          "complemento": "10º andar",
          "bairro": "Bela Vista",
          "codigoMunicipio": 3550308,
          "uf": "SP",
          "cep": 01310100
        },
        "email": "financeiro@tomador.com.br"
      }
    }
  ],
  "dataInicio": "2024-01-01",
  "dataFim": "2024-01-31",
  "transacao": true
}
```

### Campos Principais

#### Prestador
- **cpfCnpj**: CNPJ do prestador (14 dígitos, sem formatação)
- **inscricaoMunicipal**: Inscrição Municipal (CCM) do prestador
- **razaoSocial**: Razão social completa
- **endereco**: Endereço completo (obrigatório)
- **email**: Email para recebimento da NF-e

#### RPS (Recibo Provisório de Serviços)
- **inscricaoPrestador**: Mesma Inscrição Municipal do prestador
- **serieRps**: Série do RPS (ex: "A", "B", "1")
- **numeroRps**: Número sequencial do RPS (único por série)
- **tipoRPS**: `"RPS"`, `"RPS-M"` ou `"RPS-C"`
- **dataEmissao**: Data de emissão no formato `YYYY-MM-DD`
- **statusRPS**: `"N"` (Normal), `"C"` (Cancelado) ou `"E"` (Extraviado)
- **tributacaoRPS**: `"T"` (Tributação no município), `"F"` (Fora do município), `"I"` (Isento) ou `"J"` (Suspenso judicialmente)

#### Item do RPS
- **codigoServico**: Código do serviço conforme tabela da Prefeitura
- **discriminacao**: Descrição detalhada dos serviços prestados
- **valorServicos**: Valor total dos serviços (decimal, 2 casas)
- **valorDeducoes**: Valor de deduções (se houver)
- **aliquotaServicos**: Alíquota do ISS (ex: 0.05 = 5%)
- **issRetido**: `true` se ISS foi retido na fonte, `false` caso contrário

#### Tomador
- **cpfCnpj**: CPF (11 dígitos) ou CNPJ (14 dígitos) do tomador
- **inscricaoMunicipal**: Inscrição Municipal do tomador (opcional)
- **razaoSocial**: Razão social ou nome completo
- **endereco**: Endereço completo (opcional, mas recomendado)
- **email**: Email do tomador (opcional)

### Response de Sucesso

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "sucesso": true,
  "protocolo": "123456789012345",
  "chavesNFeRPS": [
    {
      "chaveNFe": {
        "inscricaoPrestador": 12345678,
        "numeroNFe": 987654321,
        "codigoVerificacao": "ABCD1234",
        "chaveNotaNacional": "12345678901234567890123456789012345678901234"
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

### Response com Erros

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "sucesso": false,
  "protocolo": null,
  "chavesNFeRPS": [],
  "alertas": [
    {
      "codigo": "E001",
      "descricao": "Alerta: Valor do serviço abaixo do mínimo permitido"
    }
  ],
  "erros": [
    {
      "codigo": "E100",
      "descricao": "Erro: Código de serviço inválido",
      "chaveRPS": {
        "inscricaoPrestador": 12345678,
        "serieRps": "A",
        "numeroRps": 1
      }
    }
  ]
}
```

---

## 2. Consulta de NF-e

### Consulta por Chave da NF-e

```json
POST /api/v1/nfe/consult
Content-Type: application/json

{
  "chaveNFe": {
    "inscricaoPrestador": 12345678,
    "numeroNFe": 987654321,
    "codigoVerificacao": "ABCD1234",
    "chaveNotaNacional": "12345678901234567890123456789012345678901234"
  }
}
```

### Consulta por Chave do RPS

```json
POST /api/v1/nfe/consult
Content-Type: application/json

{
  "chaveRps": {
    "inscricaoPrestador": 12345678,
    "serieRps": "A",
    "numeroRps": 1
  }
}
```

### Response de Sucesso

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "sucesso": true,
  "nfeList": [
    {
      "chaveNFe": {
        "inscricaoPrestador": 12345678,
        "numeroNFe": 987654321,
        "codigoVerificacao": "ABCD1234",
        "chaveNotaNacional": "12345678901234567890123456789012345678901234"
      },
      "dataEmissaoNFe": "2024-01-15T10:30:00",
      "dataFatoGeradorNFe": "2024-01-15T10:00:00",
      "statusNFe": "N",
      "valorServicos": 5000.00,
      "valorDeducoes": 0.00,
      "valorISS": 250.00,
      "codigoServico": 1234,
      "aliquotaServicos": 0.05,
      "issRetido": false,
      "discriminacao": "Serviços de consultoria em tecnologia da informação...",
      "cpfCnpjPrestador": "12345678000190",
      "razaoSocialPrestador": "EMPRESA PRESTADORA DE SERVIÇOS LTDA",
      "enderecoPrestador": { ... },
      "cpfCnpjTomador": "98765432000111",
      "razaoSocialTomador": "TOMADOR DE SERVIÇOS S.A.",
      "enderecoTomador": { ... }
    }
  ],
  "alertas": [],
  "erros": []
}
```

---

## 3. Cancelamento de NF-e

### Request

```json
POST /api/v1/nfe/cancel
Content-Type: application/json

{
  "chaveNFe": {
    "inscricaoPrestador": 12345678,
    "numeroNFe": 987654321,
    "codigoVerificacao": "ABCD1234",
    "chaveNotaNacional": "12345678901234567890123456789012345678901234"
  },
  "transacao": true
}
```

### Response de Sucesso

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "sucesso": true,
  "alertas": [],
  "erros": []
}
```

### Response com Erro

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "sucesso": false,
  "alertas": [],
  "erros": [
    {
      "codigo": "E200",
      "descricao": "NF-e não pode ser cancelada: prazo de cancelamento expirado",
      "chaveNFe": {
        "inscricaoPrestador": 12345678,
        "numeroNFe": 987654321,
        "codigoVerificacao": "ABCD1234",
        "chaveNotaNacional": "..."
      }
    }
  ]
}
```

---

## 4. Caso de Uso Completo: ERP Integrado

### Cenário

Um sistema ERP precisa:
1. Emitir uma NFS-e quando uma ordem de serviço é finalizada
2. Armazenar a chave da NF-e gerada
3. Consultar a NF-e posteriormente para verificar status
4. Cancelar a NF-e se necessário (ex: erro no faturamento)

### Implementação em C#

```csharp
using System.Net.Http.Json;
using System.Text.Json;

public class NfeServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public NfeServiceClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    // 1. Emitir NFS-e
    public async Task<NfeEmitida> EmitirNfseAsync(OrdemServico ordemServico)
    {
        var request = new SendRpsRequestDto
        {
            Prestador = new ServiceProviderDto
            {
                CpfCnpj = "12345678000190",
                InscricaoMunicipal = 12345678,
                RazaoSocial = "EMPRESA PRESTADORA LTDA",
                Endereco = new AddressDto
                {
                    TipoLogradouro = "R",
                    Logradouro = ordemServico.Prestador.Endereco.Logradouro,
                    Numero = ordemServico.Prestador.Endereco.Numero,
                    Bairro = ordemServico.Prestador.Endereco.Bairro,
                    CodigoMunicipio = 3550308,
                    UF = "SP",
                    CEP = ordemServico.Prestador.Endereco.CEP
                },
                Email = "contato@prestador.com.br"
            },
            RpsList = new List<RpsDto>
            {
                new RpsDto
                {
                    InscricaoPrestador = 12345678,
                    SerieRps = "A",
                    NumeroRps = ordemServico.Numero,
                    TipoRPS = "RPS",
                    DataEmissao = DateOnly.FromDateTime(DateTime.Now),
                    StatusRPS = "N",
                    TributacaoRPS = "T",
                    Item = new RpsItemDto
                    {
                        CodigoServico = ordemServico.CodigoServico,
                        Discriminacao = ordemServico.DescricaoServico,
                        ValorServicos = ordemServico.ValorTotal,
                        ValorDeducoes = ordemServico.ValorDesconto,
                        AliquotaServicos = ordemServico.AliquotaISS,
                        IssRetido = ordemServico.ISSRetido
                    },
                    Tomador = new ServiceCustomerDto
                    {
                        CpfCnpj = ordemServico.Cliente.CpfCnpj,
                        RazaoSocial = ordemServico.Cliente.Nome,
                        Endereco = new AddressDto
                        {
                            TipoLogradouro = ordemServico.Cliente.Endereco.TipoLogradouro,
                            Logradouro = ordemServico.Cliente.Endereco.Logradouro,
                            Numero = ordemServico.Cliente.Endereco.Numero,
                            Bairro = ordemServico.Cliente.Endereco.Bairro,
                            CodigoMunicipio = ordemServico.Cliente.Endereco.CodigoMunicipio,
                            UF = ordemServico.Cliente.Endereco.UF,
                            CEP = ordemServico.Cliente.Endereco.CEP
                        },
                        Email = ordemServico.Cliente.Email
                    }
                }
            },
            DataInicio = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
            DataFim = DateOnly.FromDateTime(DateTime.Now),
            Transacao = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/v1/rps/send", 
            request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro ao emitir NFS-e: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<SendRpsResponseDto>();

        if (!result.Sucesso)
        {
            var erros = string.Join(", ", result.Erros.Select(e => e.Descricao));
            throw new Exception($"Falha ao emitir NFS-e: {erros}");
        }

        // Armazenar chave da NF-e no banco de dados
        var nfeEmitida = new NfeEmitida
        {
            OrdemServicoId = ordemServico.Id,
            NumeroNFe = result.ChavesNFeRPS[0].ChaveNFe.NumeroNFe,
            CodigoVerificacao = result.ChavesNFeRPS[0].ChaveNFe.CodigoVerificacao,
            ChaveNotaNacional = result.ChavesNFeRPS[0].ChaveNFe.ChaveNotaNacional,
            Protocolo = result.Protocolo,
            DataEmissao = DateTime.Now
        };

        // Salvar no banco de dados
        await SalvarNfeEmitidaAsync(nfeEmitida);

        return nfeEmitida;
    }

    // 2. Consultar NF-e
    public async Task<ConsultNfeResponseDto> ConsultarNfseAsync(long numeroNFe)
    {
        var request = new ConsultNfeRequestDto
        {
            ChaveNFe = new NfeKeyDto
            {
                InscricaoPrestador = 12345678,
                NumeroNFe = numeroNFe,
                CodigoVerificacao = null, // Pode ser null na consulta
                ChaveNotaNacional = null
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/v1/nfe/consult", 
            request);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ConsultNfeResponseDto>();
    }

    // 3. Cancelar NF-e
    public async Task<bool> CancelarNfseAsync(NfeEmitida nfeEmitida)
    {
        var request = new CancelNfeRequestDto
        {
            ChaveNFe = new NfeKeyDto
            {
                InscricaoPrestador = 12345678,
                NumeroNFe = nfeEmitida.NumeroNFe,
                CodigoVerificacao = nfeEmitida.CodigoVerificacao,
                ChaveNotaNacional = nfeEmitida.ChaveNotaNacional
            },
            Transacao = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/v1/nfe/cancel", 
            request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CancelNfeResponseDto>();

        if (result.Sucesso)
        {
            // Atualizar status no banco de dados
            await AtualizarStatusNfeAsync(nfeEmitida.Id, "Cancelada");
        }

        return result.Sucesso;
    }

    private async Task SalvarNfeEmitidaAsync(NfeEmitida nfe) { /* ... */ }
    private async Task AtualizarStatusNfeAsync(int id, string status) { /* ... */ }
}
```

### Uso no ERP

```csharp
// Quando uma ordem de serviço é finalizada
var ordemServico = await ObterOrdemServicoAsync(ordemServicoId);

try
{
    // Emitir NFS-e
    var nfeEmitida = await _nfeServiceClient.EmitirNfseAsync(ordemServico);
    
    Console.WriteLine($"NFS-e emitida com sucesso!");
    Console.WriteLine($"Número: {nfeEmitida.NumeroNFe}");
    Console.WriteLine($"Código de Verificação: {nfeEmitida.CodigoVerificacao}");
    Console.WriteLine($"Protocolo: {nfeEmitida.Protocolo}");
    
    // Enviar email para o cliente com a NF-e
    await EnviarEmailNfseAsync(ordemServico.Cliente.Email, nfeEmitida);
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao emitir NFS-e: {ex.Message}");
    // Log do erro e notificação
}

// Consultar NF-e posteriormente
var consulta = await _nfeServiceClient.ConsultarNfseAsync(nfeEmitida.NumeroNFe);
if (consulta.Sucesso && consulta.NfeList.Any())
{
    var nfe = consulta.NfeList[0];
    Console.WriteLine($"Status da NF-e: {nfe.StatusNFe}");
    Console.WriteLine($"Valor do ISS: R$ {nfe.ValorISS}");
}

// Cancelar NF-e se necessário
if (precisaCancelar)
{
    var cancelado = await _nfeServiceClient.CancelarNfseAsync(nfeEmitida);
    if (cancelado)
    {
        Console.WriteLine("NF-e cancelada com sucesso!");
    }
}
```

---

## 5. Tratamento de Erros

### Erros de Validação (400 Bad Request)

```json
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "prestador.cpfCnpj": [
      "CPF/CNPJ é obrigatório"
    ],
    "rpsList[0].item.valorServicos": [
      "Valor dos serviços deve ser maior que zero"
    ]
  }
}
```

### Erros do Web Service (200 OK com erros)

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "sucesso": false,
  "protocolo": null,
  "chavesNFeRPS": [],
  "alertas": [],
  "erros": [
    {
      "codigo": "E100",
      "descricao": "Código de serviço não encontrado na tabela de serviços",
      "chaveRPS": {
        "inscricaoPrestador": 12345678,
        "serieRps": "A",
        "numeroRps": 1
      }
    }
  ]
}
```

### Erros de Infraestrutura (500 Internal Server Error)

```json
HTTP/1.1 500 Internal Server Error
Content-Type: application/json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Certificate:Mode is set to 'File' but Certificate:FilePath is not configured."
}
```

### Tratamento em C#

```csharp
try
{
    var response = await _httpClient.PostAsJsonAsync(url, request);
    
    if (response.StatusCode == HttpStatusCode.BadRequest)
    {
        var validationErrors = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        // Tratar erros de validação
        foreach (var error in validationErrors.Errors)
        {
            Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
        }
    }
    else if (response.IsSuccessStatusCode)
    {
        var result = await response.Content.ReadFromJsonAsync<SendRpsResponseDto>();
        
        if (!result.Sucesso)
        {
            // Erros retornados pelo Web Service da Prefeitura
            foreach (var erro in result.Erros)
            {
                Console.WriteLine($"Erro {erro.Codigo}: {erro.Descricao}");
                if (erro.ChaveRPS != null)
                {
                    Console.WriteLine($"RPS: {erro.ChaveRPS.SerieRps}-{erro.ChaveRPS.NumeroRps}");
                }
            }
        }
    }
    else
    {
        // Erro HTTP (500, 503, etc.)
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"HTTP {response.StatusCode}: {errorContent}");
    }
}
catch (HttpRequestException ex)
{
    // Erro de rede ou HTTP
    Console.WriteLine($"Erro de comunicação: {ex.Message}");
}
catch (JsonException ex)
{
    // Erro ao deserializar resposta
    Console.WriteLine($"Erro ao processar resposta: {ex.Message}");
}
catch (Exception ex)
{
    // Outros erros
    Console.WriteLine($"Erro inesperado: {ex.Message}");
}
```

---

## 6. Exemplos com cURL

### Enviar RPS

```bash
curl -X POST http://localhost:5000/api/v1/rps/send \
  -H "Content-Type: application/json" \
  -d '{
    "prestador": {
      "cpfCnpj": "12345678000190",
      "inscricaoMunicipal": 12345678,
      "razaoSocial": "EMPRESA TESTE LTDA",
      "endereco": {
        "tipoLogradouro": "R",
        "logradouro": "Rua Teste",
        "numero": "123",
        "bairro": "Centro",
        "codigoMunicipio": 3550308,
        "uf": "SP",
        "cep": 01000000
      },
      "email": "teste@empresa.com.br"
    },
    "rpsList": [{
      "inscricaoPrestador": 12345678,
      "serieRps": "A",
      "numeroRps": 1,
      "tipoRPS": "RPS",
      "dataEmissao": "2024-01-15",
      "statusRPS": "N",
      "tributacaoRPS": "T",
      "item": {
        "codigoServico": 1234,
        "discriminacao": "Serviços de teste",
        "valorServicos": 100.00,
        "valorDeducoes": 0.00,
        "aliquotaServicos": 0.05,
        "issRetido": false
      }
    }],
    "dataInicio": "2024-01-01",
    "dataFim": "2024-01-31",
    "transacao": true
  }'
```

### Consultar NF-e

```bash
curl -X POST http://localhost:5000/api/v1/nfe/consult \
  -H "Content-Type: application/json" \
  -d '{
    "chaveNFe": {
      "inscricaoPrestador": 12345678,
      "numeroNFe": 987654321,
      "codigoVerificacao": "ABCD1234"
    }
  }'
```

### Cancelar NF-e

```bash
curl -X POST http://localhost:5000/api/v1/nfe/cancel \
  -H "Content-Type: application/json" \
  -d '{
    "chaveNFe": {
      "inscricaoPrestador": 12345678,
      "numeroNFe": 987654321,
      "codigoVerificacao": "ABCD1234"
    },
    "transacao": true
  }'
```

### Health Check

```bash
curl http://localhost:5000/health/nfe
```

---

## 7. Dicas e Boas Práticas

### 1. Validação Antes do Envio

Sempre valide os dados antes de enviar:
- CNPJ/CPF válidos (formato)
- Valores monetários positivos
- Datas válidas
- Códigos de serviço existentes

### 2. Tratamento de Retry

Para erros temporários (timeout, 503), implemente retry:

```csharp
var maxRetries = 3;
var delay = TimeSpan.FromSeconds(2);

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        var response = await _httpClient.PostAsJsonAsync(url, request);
        if (response.IsSuccessStatusCode)
            break;
    }
    catch (HttpRequestException) when (i < maxRetries - 1)
    {
        await Task.Delay(delay * (i + 1)); // Backoff exponencial
    }
}
```

### 3. Armazenamento de Chaves

Sempre armazene:
- Número da NF-e
- Código de verificação
- Chave nacional (se disponível)
- Protocolo do lote
- Data de emissão

Isso facilita consultas e cancelamentos posteriores.

### 4. Logging

Configure logging adequado para rastreabilidade:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microled.Nfe.Service": "Debug"
    }
  }
}
```

### 5. Monitoramento

Use o health check para monitoramento:

```bash
# Verificar saúde do serviço
curl http://localhost:5000/health/nfe

# Integrar com sistemas de monitoramento (Prometheus, etc.)
```

---

## 8. Certificado A3 com Pendrive (Token USB)

### Visão Geral

Certificados A3 são certificados digitais armazenados em dispositivos físicos como pendrives ou tokens USB. Diferente dos certificados A1 (arquivos .pfx), os certificados A3 requerem que o dispositivo esteja conectado ao computador durante o uso.

### Pré-requisitos

1. **Certificado A3 instalado no pendrive/token**
2. **Drivers do dispositivo instalados** (geralmente instalados automaticamente ao conectar o pendrive)
3. **Windows Certificate Store** acessível
4. **Pendrive/token conectado** durante a execução da aplicação

### Passo a Passo

#### 1. Conectar o Pendrive/Token

- Conecte o pendrive USB ou token ao computador
- Aguarde o Windows reconhecer o dispositivo
- Se necessário, instale os drivers fornecidos pelo fabricante do certificado

#### 2. Importar o Certificado para o Windows Certificate Store

##### Opção A: Usando o Internet Explorer (método tradicional)

1. Abra o **Internet Explorer** (ou Edge no modo IE)
2. Vá em **Ferramentas > Opções da Internet > Conteúdo > Certificados**
3. Clique em **Importar...**
4. Conecte o pendrive se ainda não estiver conectado
5. Selecione o arquivo do certificado no pendrive ou use o assistente de importação
6. Escolha o repositório: **Repositório de Certificados Atuais do Usuário > Pessoal**
7. Complete o assistente de importação

##### Opção B: Usando o Certificado via Software do Fabricante

Muitos fabricantes (SafeNet, Gemalto, etc.) fornecem software específico que importa automaticamente o certificado para o Windows Certificate Store quando o dispositivo é conectado.

##### Opção C: Usando PowerShell (avançado)

```powershell
# Conecte o pendrive primeiro
# O certificado geralmente é importado automaticamente pelo Windows quando o dispositivo é reconhecido
```

#### 3. Obter o Thumbprint do Certificado

O thumbprint é uma identificação única do certificado. Existem várias formas de obtê-lo:

##### Método 1: Usando o Windows Certificate Manager (certmgr.msc)

1. Pressione `Win + R` e digite `certmgr.msc`, depois Enter
2. Navegue até **Pessoal > Certificados**
3. Localize seu certificado digital
4. Clique duas vezes no certificado para abrir
5. Vá na aba **Detalhes**
6. Role até encontrar o campo **Impressão Digital** (Thumbprint)
7. Copie o valor (exemplo: `a1 b2 c3 d4 e5 f6 ...`)
8. Remova os espaços: `a1b2c3d4e5f6...`

##### Método 2: Usando PowerShell

```powershell
# Listar todos os certificados no repositório pessoal do usuário atual
Get-ChildItem -Path Cert:\CurrentUser\My | Format-Table Subject, Thumbprint, NotAfter

# Para buscar um certificado específico por CNPJ ou Razão Social
Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*SEU_CNPJ*" } | Format-Table Subject, Thumbprint, NotAfter
```

##### Método 3: Usando o Internet Explorer

1. Abra o Internet Explorer
2. Vá em **Ferramentas > Opções da Internet > Conteúdo > Certificados**
3. Na aba **Pessoal**, selecione seu certificado
4. Clique em **Exibir > Detalhes**
5. Role até **Impressão Digital** e copie o valor

#### 4. Configurar o appsettings.json

Edite o arquivo `appsettings.json` (ou `appsettings.Production.json` / `appsettings.Homologation.json`) e configure:

```json
{
  "NfeService": {
    "Certificate": {
      "Mode": "Store",
      "StoreLocation": "CurrentUser",
      "StoreName": "My",
      "Thumbprint": "A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4"
    }
  }
}
```

**Parâmetros importantes:**

- **Mode**: Deve ser `"Store"` para certificados A3
- **StoreLocation**: 
  - `"CurrentUser"` - Repositório do usuário atual (recomendado)
  - `"LocalMachine"` - Repositório do computador (requer privilégios administrativos)
- **StoreName**: Geralmente `"My"` (equivalente a "Pessoal")
- **Thumbprint**: O thumbprint do certificado (pode incluir ou não espaços, hífens ou dois pontos - será normalizado automaticamente)

**Exemplo completo para Homologação:**

```json
{
  "NfeService": {
    "BaseUrl": "https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "TimeoutSeconds": 60,
    "Environment": "Homologation",
    "DefaultIssuerCnpj": "12345678000190",
    "DefaultIssuerIm": "12345678",
    "DefaultCnpjRemetente": "12345678000190",
    "Certificate": {
      "Mode": "Store",
      "StoreLocation": "CurrentUser",
      "StoreName": "My",
      "Thumbprint": "A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4"
    }
  }
}
```

#### 5. Verificar a Configuração

Execute a aplicação e verifique o health check:

```bash
# Para API
curl http://localhost:5000/health/nfe

# Para Console
dotnet run
```

Se houver erro relacionado ao certificado, verifique:
- Se o pendrive/token está conectado
- Se o thumbprint está correto
- Se o certificado está no repositório correto (CurrentUser > My)
- Se o certificado tem chave privada (certificados A3 sempre têm)

### Dicas Importantes

#### 1. Pendrive/Token Deve Estar Conectado

⚠️ **Importante**: O pendrive ou token USB deve estar **conectado ao computador** durante toda a execução da aplicação. Se o dispositivo for desconectado, a aplicação não conseguirá acessar a chave privada para assinar as requisições.

#### 2. PIN/Senha do Certificado

Certificados A3 geralmente requerem um PIN ou senha quando são utilizados. O Windows pode solicitar esse PIN automaticamente quando o certificado é acessado, ou você pode precisar digitar manualmente quando solicitado.

#### 3. Múltiplos Certificados

Se você tiver múltiplos certificados no repositório, certifique-se de usar o thumbprint correto. O thumbprint é único para cada certificado.

#### 4. Normalização do Thumbprint

O sistema normaliza automaticamente o thumbprint, removendo espaços, hífens (`-`) e dois pontos (`:`), e convertendo para maiúsculas. Portanto, você pode usar qualquer um destes formatos:

```
A1B2C3D4E5F6...
a1:b2:c3:d4:e5:f6:...
a1-b2-c3-d4-e5-f6-...
a1 b2 c3 d4 e5 f6 ...
```

Todos serão tratados da mesma forma.

#### 5. Erros Comuns

**Erro: "Certificate with thumbprint ... not found in store"**
- Verifique se o thumbprint está correto
- Verifique se o StoreLocation e StoreName estão corretos
- Certifique-se de que o certificado está importado no repositório correto

**Erro: "Certificate ... does not have a private key"**
- Certificados A3 sempre têm chave privada, mas ela está no dispositivo físico
- Verifique se o pendrive/token está conectado
- Verifique se os drivers do dispositivo estão instalados corretamente

**Erro: "The specified network password is not correct" ou solicitação de PIN**
- Digite o PIN correto do certificado quando solicitado
- O PIN geralmente é definido quando o certificado é emitido

### Diferença entre A1 e A3

| Característica | A1 (Arquivo .pfx) | A3 (Pendrive/Token) |
|----------------|-------------------|---------------------|
| Armazenamento | Arquivo no disco | Dispositivo físico |
| Configuração | `Mode: "File"` + `FilePath` + `Password` | `Mode: "Store"` + `Thumbprint` |
| Disponibilidade | Sempre disponível | Requer dispositivo conectado |
| Segurança | Depende da senha do arquivo | Depende do dispositivo físico + PIN |
| Mobilidade | Pode ser copiado | Dispositivo físico único |

### Exemplo de Configuração Completa

```json
{
  "NfeService": {
    "ProductionEndpoint": "https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "TestEndpoint": "https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx",
    "UseProduction": false,
    "Environment": "Homologation",
    "TimeoutSeconds": 60,
    "DefaultIssuerCnpj": "12345678000190",
    "DefaultIssuerIm": "12345678",
    "DefaultCnpjRemetente": "12345678000190",
    "Certificate": {
      "Mode": "Store",
      "StoreLocation": "CurrentUser",
      "StoreName": "My",
      "Thumbprint": "ABCDEF1234567890ABCDEF1234567890ABCDEF12"
    }
  }
}
```

---

## 9. Referências

- **Documentação Oficial**: `Documentacao/NFe_Web_Service-4.pdf`
- **README Principal**: `README.md`
- **Configuração de Homologação**: `Microled.Nfe.Service.Api/HOMOLOGATION_SETUP.md`
- **Guia de Configuração**: `Microled.Nfe.Service.Api/CONFIGURATION.md`

