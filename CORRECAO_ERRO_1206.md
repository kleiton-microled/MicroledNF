# Correção do Erro 1206 - Baseado em Retorno Real

## Análise do Retorno da Prefeitura

**String verificada pela prefeitura:**
```
000037684280A 00000000371220251201TNN00000000000000000000000000000002919300000000000000
```

**Tamanho total:** 87 caracteres

## Problemas Identificados

### 1. ❌ InscricaoPrestador: 8 vs 12 caracteres

**Especificação XSD:** 8 caracteres  
**Implementação Real da Prefeitura:** 12 caracteres

**String da prefeitura:** `000037684280` (12 caracteres)

**Correção aplicada:**
```csharp
// ANTES (8 caracteres):
var inscricaoMunicipal = inscricaoPrestadorStr.PadLeft(8, '0');

// DEPOIS (12 caracteres):
var inscricaoMunicipal = inscricaoPrestadorStr.PadLeft(12, '0');
```

### 2. ❌ SerieRPS: 5 caracteres fixos vs tamanho variável

**Especificação XSD:** 5 caracteres fixos com espaços à direita  
**Implementação Real da Prefeitura:** Tamanho variável (série + 1 espaço)

**String da prefeitura:** `A ` (2 caracteres: "A" + espaço)

**Correção aplicada:**
```csharp
// ANTES (5 caracteres fixos):
var serieRps = (rps.ChaveRPS.SerieRps ?? "").PadRight(5, ' ');

// DEPOIS (tamanho variável + 1 espaço):
var serieRps = (rps.ChaveRPS.SerieRps ?? "") + " ";
```

## Estrutura Corrigida da String

| Campo | Tamanho | Exemplo | Observação |
|-------|---------|---------|------------|
| InscricaoMunicipal | **12** | `000037684280` | ✅ Corrigido: 8 → 12 |
| SerieRPS | **Variável** | `A ` | ✅ Corrigido: 5 fixos → variável + espaço |
| NumeroRPS | 12 | `000000003712` | ✓ Já estava correto |
| DataEmissao | 8 | `20251201` | ✓ Já estava correto |
| TipoTributacao | 1 | `T` | ✓ Já estava correto |
| StatusRPS | 1 | `N` | ✓ Já estava correto |
| ISSRetido | 1 | `N` | ✓ Já estava correto |
| ValorServicos | 15 | `000000000000000` | ✓ Já estava correto |
| ValorDeducoes | 15 | `000000000000000` | ✓ Já estava correto |
| CodigoServico | 5 | `02919` | ✓ Já estava correto |
| IndicadorTomador | 1 | `3` | ✓ Já estava correto |
| CPF/CNPJTomador | 14 | `00000000000000` | ✓ Já estava correto |

**Tamanho total:** 12 + 2 + 12 + 8 + 1 + 1 + 1 + 15 + 15 + 5 + 1 + 14 = **87 caracteres** (para série "A")

## Validação

A string gerada agora deve bater exatamente com a string verificada pela prefeitura:

**Prefeitura:**
```
000037684280A 00000000371220251201TNN00000000000000000000000000000002919300000000000000
```

**Nossa implementação (após correção):**
```
000037684280A 00000000371220251201TNN00000000000000000000000000000002919300000000000000
```

✅ **Strings devem ser idênticas agora!**

## Observações Importantes

1. **Tamanho variável:** O tamanho total da string varia conforme a série RPS:
   - Série "A": 87 caracteres
   - Série "AB": 88 caracteres
   - Série "ABC": 89 caracteres
   - etc.

2. **Inconsistência com XSD:** A implementação real da prefeitura difere do XSD:
   - XSD especifica 8 caracteres para InscricaoMunicipal
   - XSD especifica 5 caracteres fixos para SerieRPS
   - **Prefeitura usa 12 caracteres e tamanho variável**

3. **Validação removida:** Removida a validação de tamanho fixo, pois o tamanho varia conforme a série RPS.

## Arquivos Modificados

- `Microled.Nfe.Service.Business/Services/RpsSignatureService.cs`
  - InscricaoMunicipal: 8 → 12 caracteres
  - SerieRPS: 5 fixos → variável + espaço
  - Validação de tamanho fixo removida
  - Logging adicionado para debug

## Próximos Passos

1. ✅ Correções aplicadas
2. ⏳ Testar com webservice real
3. ⏳ Verificar se erro 1206 foi resolvido
4. ⏳ Atualizar testes unitários se necessário

---

**Data da correção:** 2026-01-05  
**Baseado em:** Retorno real do webservice com erro 1206

