# Validação do Prompt - Correção Assinatura RPS (Erro 1206)

## Análise Comparativa: Prompt vs Implementação Atual vs XSD

### 1. Tamanho da String de Assinatura

| Fonte | Tamanho | Observação |
|-------|---------|------------|
| **Prompt** | Não especifica explicitamente | Menciona campos individuais |
| **XSD (TiposNFe_v02.xsd)** | **86 posições** | Documentação oficial: "A cadeia de caracteres a ser assinada deverá conter 86 posições" |
| **Implementação Atual** | **85 caracteres** | ❌ **DISCREPÂNCIA** |
| **Cálculo Manual** | 8+5+12+8+1+1+1+15+15+5+14 = **85** | Sem Indicador Tomador |
| **Cálculo com Indicador** | 8+5+12+8+1+1+1+15+15+5+1+14 = **86** | Com Indicador Tomador (1 char) |

**Conclusão:** O XSD especifica 86 posições, mas a implementação atual gera 85. O prompt menciona "Indicador Tomador" que não está sendo usado atualmente.

---

### 2. InscricaoPrestador / Inscrição Municipal

| Fonte | Tamanho | Formato |
|-------|---------|---------|
| **Prompt** | **12 posições** | Zero à esquerda |
| **XSD** | **8 caracteres** | Zero à esquerda |
| **Implementação Atual** | **8 caracteres** | Zero à esquerda (usa últimos 8 dígitos se > 8) |

**Exemplo do Prompt:**
- `37684280` => `000037684280` (12 chars)

**Exemplo da Implementação:**
- `37684280` => `37684280` (8 chars, já tem 8 dígitos)
- `123456789012` => `456789012` (últimos 8 dígitos)

**Conclusão:** ❌ **DISCREPÂNCIA CRÍTICA** - O prompt pede 12 posições, mas o XSD e a implementação atual usam 8. O prompt pode estar incorreto ou se referindo a outro campo.

---

### 3. Indicador Tomador

| Fonte | Campo | Tamanho | Valores |
|-------|-------|---------|---------|
| **Prompt** | ✅ **Sim** | **1 char** | `1`=CPF, `2`=CNPJ, `3`=não informado |
| **XSD** | ❌ **Não mencionado** | - | - |
| **Implementação Atual** | ❌ **Não usado** | - | - |

**Observação do XSD:**
> "Se o Indicador do CPF/CNPJ for 3 (não-informado), preencher com 14 zeros."

O XSD menciona o "Indicador" mas **não o inclui na string de assinatura**. Apenas menciona que se for 3, usar 14 zeros no CPF/CNPJ.

**Conclusão:** ⚠️ **INCERTEZA** - O prompt pede incluir o Indicador Tomador na string, mas o XSD não menciona isso explicitamente. Pode ser necessário validar com a prefeitura ou testar.

---

### 4. Série RPS

| Fonte | Tamanho | Formato |
|-------|---------|---------|
| **Prompt** | 5 posições | Espaços à direita |
| **XSD** | 5 posições | Espaços à direita |
| **Implementação Atual** | 5 posições | Espaços à direita ✅ |

**Conclusão:** ✅ **CORRETO**

---

### 5. Numero RPS

| Fonte | Tamanho | Formato |
|-------|---------|---------|
| **Prompt** | 12 posições | Zero à esquerda |
| **XSD** | 12 posições | Zero à esquerda |
| **Implementação Atual** | 12 posições | Zero à esquerda ✅ |

**Conclusão:** ✅ **CORRETO**

---

### 6. Data Emissão

| Fonte | Formato |
|-------|---------|
| **Prompt** | `yyyyMMdd` |
| **XSD** | `AAAAMMDD` |
| **Implementação Atual** | `yyyyMMdd` ✅ |

**Conclusão:** ✅ **CORRETO**

---

### 7. ISS Retido

| Fonte | Formato |
|-------|---------|
| **Prompt** | `S` ou `N` (não boolean) |
| **XSD** | `S` ou `N` |
| **Implementação Atual** | `S` ou `N` ✅ |

**Conclusão:** ✅ **CORRETO**

---

### 8. Valor Serviços / Deduções

| Fonte | Tamanho | Formato |
|-------|---------|---------|
| **Prompt** | 15 posições | 2 decimais, sem separador, zero à esquerda |
| **XSD** | 15 posições | Sem separador de milhar e decimal |
| **Implementação Atual** | 15 posições | Converte para centavos (multiplica por 100) ✅ |

**Exemplo:**
- Valor: `0.00` => `000000000000000` (15 zeros)
- Valor: `123.45` => `000000000012345` (centavos)

**Conclusão:** ✅ **CORRETO** - A implementação já converte corretamente para centavos.

---

### 9. Código Serviço

| Fonte | Tamanho | Formato |
|-------|---------|---------|
| **Prompt** | 5 posições | Zero à esquerda |
| **XSD** | 5 posições | - |
| **Implementação Atual** | 5 posições | Zero à esquerda ✅ |

**Conclusão:** ✅ **CORRETO**

---

### 10. CPF/CNPJ Tomador

| Fonte | Tamanho | Formato |
|-------|---------|---------|
| **Prompt** | 14 posições | Zero à esquerda (CPF: 11→14, CNPJ: 14 direto, não informado: 14 zeros) |
| **XSD** | 14 posições | Zero à esquerda, 14 zeros se não informado |
| **Implementação Atual** | 14 posições | Zero à esquerda, 14 zeros se não informado ✅ |

**Conclusão:** ✅ **CORRETO**

---

### 11. Algoritmo de Assinatura

| Fonte | Algoritmo | Encoding | Padding |
|-------|-----------|----------|---------|
| **Prompt** | SHA1 + RSA | ASCII | PKCS#1 v1.5 |
| **XSD** | SHA1 com RSA | ASCII | - |
| **Implementação Atual** | SHA1 + RSA | ASCII | PKCS1 ✅ |

**Conclusão:** ✅ **CORRETO**

---

## Problemas Identificados

### 🔴 CRÍTICO: Tamanho da String

**Problema:** 
- XSD especifica **86 posições**
- Implementação atual gera **85 caracteres**
- Prompt não especifica explicitamente, mas menciona "Indicador Tomador" que pode ser o campo faltante

**Solução Proposta:**
1. Adicionar campo "Indicador Tomador" (1 char) antes do CPF/CNPJ Tomador
2. Determinar o indicador baseado no tipo do documento:
   - `1` = CPF (11 dígitos)
   - `2` = CNPJ (14 dígitos)
   - `3` = Não informado

### 🟡 ATENÇÃO: InscricaoPrestador

**Problema:**
- Prompt pede **12 posições**
- XSD e implementação atual usam **8 posições**

**Análise:**
- O prompt pode estar se referindo ao campo `InscricaoPrestador` da `ChaveRPS` (que pode ter mais de 8 dígitos)
- Mas na string de assinatura, o XSD especifica claramente **8 caracteres** (Inscrição Municipal)
- A implementação atual já trata isso corretamente (usa últimos 8 dígitos se necessário)

**Recomendação:**
- Manter **8 posições** conforme XSD
- Se o erro 1206 persistir, validar se a prefeitura está usando 12 posições internamente

### 🟡 ATENÇÃO: Indicador Tomador

**Problema:**
- Prompt pede incluir "Indicador Tomador" na string
- XSD menciona o indicador mas não o inclui explicitamente na string de assinatura

**Recomendação:**
- Implementar o Indicador Tomador para atingir 86 caracteres
- Adicionar logging detalhado para comparar com a string retornada no erro 1206

---

## Recomendações de Implementação

### 1. Adicionar Indicador Tomador

```csharp
// Determinar indicador baseado no tipo de documento
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

### 2. Ajustar Ordem dos Campos

**Ordem atual (85 chars):**
1. InscricaoMunicipal (8)
2. SerieRPS (5)
3. NumeroRPS (12)
4. DataEmissao (8)
5. TipoTributacao (1)
6. StatusRPS (1)
7. ISSRetido (1)
8. ValorServicos (15)
9. ValorDeducoes (15)
10. CodigoServico (5)
11. CPF/CNPJTomador (14)

**Ordem proposta (86 chars):**
1. InscricaoMunicipal (8)
2. SerieRPS (5)
3. NumeroRPS (12)
4. DataEmissao (8)
5. TipoTributacao (1)
6. StatusRPS (1)
7. ISSRetido (1)
8. ValorServicos (15)
9. ValorDeducoes (15)
10. CodigoServico (5)
11. **IndicadorTomador (1)** ← NOVO
12. CPF/CNPJTomador (14)

### 3. Melhorar Logging e Debug

Adicionar método para comparar strings caractere a caractere quando erro 1206 ocorrer:

```csharp
public void CompareSignatureStrings(string ourString, string prefeituraString)
{
    // Log detalhado com espaços visíveis
    // Comparação posição a posição
    // Diff visual
}
```

### 4. Validação do Tamanho

Ajustar validação de 85 para 86 caracteres:

```csharp
if (signatureString.Length != 86)
{
    throw new InvalidOperationException($"Signature string must have exactly 86 characters, but has {signatureString.Length}");
}
```

---

## Checklist de Validação do Prompt

- [x] Algoritmo de assinatura (SHA1 + RSA + PKCS1) ✅
- [x] Encoding ASCII ✅
- [x] Base64 na tag `<Assinatura>` ✅
- [x] Série RPS (5 posições, espaços à direita) ✅
- [x] Numero RPS (12 posições, zeros à esquerda) ✅
- [x] Data Emissão (yyyyMMdd) ✅
- [x] ISS Retido (S/N) ✅
- [x] Valores (15 posições, centavos) ✅
- [x] Código Serviço (5 posições) ✅
- [x] CPF/CNPJ Tomador (14 posições) ✅
- [ ] **Indicador Tomador (1 char)** ⚠️ Precisa implementar
- [ ] **Tamanho total (86 chars)** ⚠️ Precisa ajustar
- [ ] **InscricaoPrestador (12 vs 8)** ⚠️ Precisa validar com prefeitura
- [ ] **Logging detalhado para debug** ⚠️ Precisa implementar
- [ ] **Validação automática do erro 1206** ⚠️ Precisa implementar

---

## Próximos Passos

1. **Implementar Indicador Tomador** na string de assinatura
2. **Ajustar validação** de 85 para 86 caracteres
3. **Adicionar logging detalhado** com espaços visíveis
4. **Implementar comparação automática** quando erro 1206 ocorrer
5. **Testar com webservice** e comparar string retornada no erro
6. **Validar InscricaoPrestador** (8 vs 12) com a prefeitura se necessário

---

## Resumo Executivo

### ✅ Pontos Corretos do Prompt

1. **Algoritmo de assinatura** (SHA1 + RSA + PKCS1) - ✅ Já implementado
2. **Encoding ASCII** - ✅ Já implementado
3. **Formato dos campos** (Série, Número, Data, etc.) - ✅ Já implementado corretamente
4. **Valores em centavos** - ✅ Já implementado
5. **Base64 na tag `<Assinatura>`** - ✅ Já implementado

### ⚠️ Pontos que Precisam de Ajuste

1. **Tamanho da string**: XSD especifica 86, implementação atual usa 85
   - **Solução**: Adicionar campo "Indicador Tomador" (1 char)

2. **InscricaoPrestador**: Prompt pede 12 posições, mas XSD e implementação usam 8
   - **Recomendação**: Manter 8 posições conforme XSD, mas validar se erro 1206 persistir

3. **Indicador Tomador**: Não está sendo usado atualmente
   - **Solução**: Implementar conforme prompt (1=CPF, 2=CNPJ, 3=não informado)

4. **Logging e Debug**: Não há comparação automática quando erro 1206 ocorre
   - **Solução**: Implementar método de comparação caractere a caractere

### 🔴 Discrepâncias Críticas

1. **Tamanho da String (85 vs 86)**
   - Implementação atual: 85 caracteres
   - XSD oficial: 86 caracteres
   - **Impacto**: Pode ser a causa do erro 1206

2. **Indicador Tomador Ausente**
   - Prompt pede incluir na string
   - Implementação atual não inclui
   - **Impacto**: String não bate com a esperada pela prefeitura

### 📋 Validação Final do Prompt

| Item | Status | Observação |
|------|--------|------------|
| Algoritmo SHA1+RSA | ✅ | Correto |
| Encoding ASCII | ✅ | Correto |
| Padding PKCS1 | ✅ | Correto |
| Base64 | ✅ | Correto |
| Formato dos campos | ✅ | Correto |
| Indicador Tomador | ⚠️ | Precisa implementar |
| Tamanho 86 chars | ⚠️ | Precisa ajustar |
| InscricaoPrestador 12 | ❌ | XSD especifica 8 |
| Logging detalhado | ⚠️ | Precisa implementar |
| Validação erro 1206 | ⚠️ | Precisa implementar |

**Conclusão Geral:** O prompt está **parcialmente correto**. A maioria dos requisitos já está implementada, mas há **2 pontos críticos** que precisam ser ajustados:
1. Adicionar Indicador Tomador para atingir 86 caracteres
2. Implementar logging e validação para debug do erro 1206

O ponto sobre InscricaoPrestador com 12 posições parece estar incorreto (XSD especifica 8), mas pode ser validado com a prefeitura se o erro persistir.

---

## Referências

- **XSD:** `XSD/schemas-reformatributaria-v02-3/TiposNFe_v02.xsd` (linha 17-36)
- **Implementação Atual:** `Microled.Nfe.Service.Business/Services/RpsSignatureService.cs`
- **Documentação:** `Microled.Nfe.Service.Business/SIGNATURE_IMPLEMENTATION.md`
- **Exemplo XML:** `Documentacao/Arquivos-de-exemplo/Arquivos de exemplo/PedidoEnvioLoteRPS_exemplo.txt`

