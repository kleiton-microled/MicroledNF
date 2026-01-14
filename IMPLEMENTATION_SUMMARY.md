# Resumo da Implementação - Correção Assinatura RPS (Erro 1206)

## Implementações Realizadas

### ✅ 1. Adicionado Indicador Tomador na String de Assinatura

**Arquivo:** `Microled.Nfe.Service.Business/Services/RpsSignatureService.cs`

- Adicionado campo "Indicador Tomador" (1 char) antes do CPF/CNPJ Tomador
- Valores:
  - `1` = CPF
  - `2` = CNPJ  
  - `3` = Não informado
- String de assinatura agora tem **86 caracteres** (conforme XSD)

**Código:**
```csharp
// Indicador Tomador com 1 posição
char indicadorTomador;
if (rps.Tomador?.CpfCnpj == null)
{
    indicadorTomador = '3'; // Não informado
}
else if (rps.Tomador.CpfCnpj.IsCpf)
{
    indicadorTomador = '1'; // CPF
}
else
{
    indicadorTomador = '2'; // CNPJ
}
```

---

### ✅ 2. Ajustada Validação de Tamanho (85 → 86 caracteres)

**Arquivo:** `Microled.Nfe.Service.Business/Services/RpsSignatureService.cs`

- Validação atualizada de 85 para 86 caracteres
- Comentários atualizados na documentação

**Código:**
```csharp
if (signatureString.Length != 86)
{
    throw new InvalidOperationException($"Signature string must have exactly 86 characters, but has {signatureString.Length}");
}
```

---

### ✅ 3. Adicionado Logging Detalhado

**Arquivo:** `Microled.Nfe.Service.Business/Services/RpsSignatureService.cs`

- Método `LogSignatureString()` adicionado
- Mostra espaços como caracteres visíveis (·) para facilitar debug
- Loga o tamanho da string e a versão com espaços visíveis

**Código:**
```csharp
private void LogSignatureString(string signatureString, Rps rps)
{
    var visibleString = signatureString.Replace(' ', '·');
    _logger.LogDebug(
        "RPS Signature String for {InscricaoPrestador}-{NumeroRps}: Length={Length}, String={SignatureString}",
        rps.ChaveRPS.InscricaoPrestador,
        rps.ChaveRPS.NumeroRps,
        signatureString.Length,
        visibleString
    );
}
```

---

### ✅ 4. Implementada Comparação Automática para Erro 1206

**Arquivos:**
- `Microled.Nfe.Service.Business/Services/RpsSignatureService.cs`
- `Microled.Nfe.Service.Domain/Interfaces/IRpsSignatureService.cs`
- `Microled.Nfe.Service.Infra/Client/NfeSoapClient.cs`

#### 4.1. Método de Comparação

**Método:** `CompareSignatureStrings()`
- Compara nossa string com a string retornada pela prefeitura
- Mostra diferenças caractere a caractere
- Loga posição, caractere esperado vs atual, e valores hexadecimais

**Código:**
```csharp
public bool CompareSignatureStrings(string ourString, string prefeituraString, Rps rps)
{
    // Compara strings e mostra diferenças detalhadas
    // Loga posição a posição quando há divergências
}
```

#### 4.2. Extração de String do Erro

**Método:** `ExtractSignatureStringFromError()`
- Extrai a string de assinatura da mensagem de erro 1206
- Suporta múltiplos padrões de regex
- Procura por "String verificada (...)" ou padrões similares

**Código:**
```csharp
public string? ExtractSignatureStringFromError(string errorMessage)
{
    // Extrai string usando regex patterns
    // Retorna null se não encontrar
}
```

#### 4.3. Integração no NfeSoapClient

**Método:** `CheckAndCompareSignatureErrors()`
- Detecta automaticamente erros 1206 na resposta
- Para cada erro 1206:
  1. Localiza o RPS correspondente no batch
  2. Extrai a string da mensagem de erro
  3. Gera nossa string de assinatura
  4. Compara e loga diferenças

**Código:**
```csharp
private void CheckAndCompareSignatureErrors(List<Evento> erros, DomainEntities.RpsBatch batch)
{
    const int ErrorCode1206 = 1206;
    var signatureErrors = erros.Where(e => e.Codigo == ErrorCode1206).ToList();
    
    foreach (var erro in signatureErrors)
    {
        // Encontra RPS, extrai string, compara
    }
}
```

---

### ✅ 5. Atualizados Testes Unitários

**Arquivo:** `Microled.Nfe.Service.Tests/Business/RpsSignatureServiceTests.cs`

- Todos os testes atualizados para 86 caracteres
- Novos testes adicionados:
  - `BuildSignatureString_ShouldIncludeIndicadorTomador_WhenTomadorHasCpf()`
  - `BuildSignatureString_ShouldIncludeIndicadorTomador_WhenTomadorHasCnpj()`
  - `BuildSignatureString_ShouldIncludeIndicadorTomador_WhenTomadorIsNull()`
  - `CompareSignatureStrings_ShouldReturnTrue_WhenStringsMatch()`
  - `CompareSignatureStrings_ShouldReturnFalse_WhenStringsDiffer()`
  - `ExtractSignatureStringFromError_ShouldExtractString_WhenPatternMatches()`
  - `ExtractSignatureStringFromError_ShouldReturnNull_WhenPatternNotFound()`

---

### ✅ 6. Atualizada Interface

**Arquivo:** `Microled.Nfe.Service.Domain/Interfaces/IRpsSignatureService.cs`

- Adicionados métodos públicos:
  - `CompareSignatureStrings()`
  - `ExtractSignatureStringFromError()`

---

### ✅ 7. Atualizado Registro de Dependências

**Arquivos:**
- `Microled.Nfe.Service.Api/Program.cs`
- `Microled.Nfe.Service.Console/Program.cs`

- `IRpsSignatureService` agora é injetado no `NfeSoapClient`
- Permite comparação automática quando erro 1206 ocorrer

---

## Estrutura da String de Assinatura (86 caracteres)

| Campo | Posição | Tamanho | Formato | Exemplo |
|-------|---------|---------|---------|---------|
| InscricaoMunicipal | 0-7 | 8 | Zeros à esquerda | `12345678` |
| SerieRPS | 8-12 | 5 | Espaços à direita | `A    ` |
| NumeroRPS | 13-24 | 12 | Zeros à esquerda | `000000000001` |
| DataEmissao | 25-32 | 8 | yyyyMMdd | `20240115` |
| TipoTributacao | 33 | 1 | T/F/I/J | `T` |
| StatusRPS | 34 | 1 | N/C/E | `N` |
| ISSRetido | 35 | 1 | S/N | `N` |
| ValorServicos | 36-50 | 15 | Centavos, zeros à esquerda | `000000000010000` |
| ValorDeducoes | 51-65 | 15 | Centavos, zeros à esquerda | `000000000000000` |
| CodigoServico | 66-70 | 5 | Zeros à esquerda | `01234` |
| **IndicadorTomador** | **71** | **1** | **1/2/3** | **`2`** ← NOVO |
| CPF/CNPJTomador | 72-85 | 14 | Zeros à esquerda | `00000000000000` |

---

## Como Usar

### Comparação Automática

Quando o erro 1206 ocorrer, a comparação é feita automaticamente. Os logs mostrarão:

```
[Warning] Error 1206 detected: 1 signature error(s) found
[Warning] Signature string mismatch for RPS 12345678-1. Our length: 86, Prefeitura length: 86
[Warning] Our string:     12345678A····00000000000120240115TNN00000000001000000000000000012342100000000000000
[Warning] Prefeitura:     12345678A····00000000000120240115TNN00000000001000000000000000012342100000000000000
[Warning] Differences found: 1
[Warning]   Position 71: Expected '2' (0x32), Got '1' (0x31)
```

### Comparação Manual

```csharp
var signatureService = serviceProvider.GetRequiredService<IRpsSignatureService>();
var rps = // ... seu RPS

// Gerar string
var ourString = signatureService.BuildSignatureString(rps);

// Extrair do erro (se tiver)
var errorMessage = "Erro 1206: String verificada (12345678...)";
var prefeituraString = signatureService.ExtractSignatureStringFromError(errorMessage);

// Comparar
if (prefeituraString != null)
{
    signatureService.CompareSignatureStrings(ourString, prefeituraString, rps);
}
```

---

## Próximos Passos

1. ✅ Indicador Tomador implementado
2. ✅ Tamanho ajustado para 86 caracteres
3. ✅ Logging detalhado implementado
4. ✅ Comparação automática implementada
5. ✅ Testes atualizados

**Próximo:** Testar com webservice real e verificar se o erro 1206 foi resolvido.

---

## Arquivos Modificados

1. `Microled.Nfe.Service.Business/Services/RpsSignatureService.cs`
2. `Microled.Nfe.Service.Domain/Interfaces/IRpsSignatureService.cs`
3. `Microled.Nfe.Service.Infra/Client/NfeSoapClient.cs`
4. `Microled.Nfe.Service.Tests/Business/RpsSignatureServiceTests.cs`
5. `Microled.Nfe.Service.Api/Program.cs`
6. `Microled.Nfe.Service.Console/Program.cs`

---

## Referências

- **XSD:** `XSD/schemas-reformatributaria-v02-3/TiposNFe_v02.xsd` (linha 17-36)
- **Validação:** `PROMPT_VALIDATION.md`
- **Documentação Original:** `Microled.Nfe.Service.Business/SIGNATURE_IMPLEMENTATION.md`

