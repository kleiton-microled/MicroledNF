# Microled.Nfe.Service.Console

Aplicação console .NET 8 para processamento de RPS a partir de banco de dados Access (.MDB) e envio de NFS-e para a Prefeitura de São Paulo.

## 📋 Descrição

Este console lê registros de RPS pendentes de um arquivo Access (.MDB), monta lotes e envia para o Web Service da NFS-e usando os mesmos casos de uso da API Web.

## 🚀 Requisitos

- **.NET 8 Runtime** ou SDK
- **Windows** (para acesso ao Access via OleDb)
- **Certificado Digital A3** (token/pendrive) instalado no Windows Certificate Store
- **Microsoft Access Database Engine** (ACE.OLEDB.12.0) instalado
- Arquivo **Access (.MDB)** com tabela de RPS

## ⚙️ Configuração

### 1. appsettings.json

Edite o arquivo `appsettings.json` na raiz do projeto:

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
      "Thumbprint": "ABCDEF1234567890ABCDEF1234567890ABCDEF12"
    }
  },
  "AccessDatabase": {
    "DatabasePath": "C:\\Microled\\Nfe\\Dados\\NFS-e.mdb",
    "RpsTableName": "TB_RPS",
    "BatchSize": 50,
    "PendingStatus": "P",
    "SentStatus": "E",
    "PrimaryKeyColumn": "Id",
    "StatusColumn": "Status"
  }
}
```

### 2. Certificado A3 (Token/Pendrive)

1. **Instale o certificado** no Windows Certificate Store:
   - Conecte o token/pendrive
   - Importe o certificado para `CurrentUser > Personal > Certificates`
   - Anote o **Thumbprint** do certificado

2. **Configure o Thumbprint** no `appsettings.json`:
   ```json
   "Certificate": {
     "Mode": "Store",
     "StoreLocation": "CurrentUser",
     "StoreName": "My",
     "Thumbprint": "SEU_THUMBPRINT_AQUI"
   }
   ```

### 3. Estrutura do Banco Access

O console espera uma tabela com as seguintes colunas (ajuste conforme necessário):

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| `Id` | Inteiro | Chave primária |
| `Status` | Texto | Status do RPS ("P" = Pendente, "E" = Enviado) |
| `NumeroRps` | Inteiro | Número do RPS |
| `Serie` | Texto | Série do RPS (ex: "A") |
| `DataEmissao` | Data | Data de emissão |
| `CnpjPrestador` | Texto | CNPJ do prestador (14 dígitos) |
| `ImPrestador` | Inteiro | Inscrição Municipal do prestador |
| `RazaoSocialPrestador` | Texto | Razão social do prestador |
| `CpfCnpjTomador` | Texto | CPF (11) ou CNPJ (14) do tomador |
| `NomeTomador` | Texto | Nome/Razão social do tomador |
| `ValorServico` | Decimal | Valor dos serviços |
| `ValorDeducao` | Decimal | Valor das deduções |
| `CodigoServico` | Inteiro | Código do serviço |
| `Discriminacao` | Texto | Descrição dos serviços |
| `AliquotaISS` | Decimal | Alíquota do ISS (ex: 0.05 = 5%) |
| `ISSRetido` | Boolean | Se ISS foi retido |
| `TipoRPS` | Texto | Tipo do RPS ("RPS", "RPS-M", "RPS-C") |
| `StatusRPS` | Texto | Status ("N" = Normal, "C" = Cancelado, "E" = Extraviado) |
| `TributacaoRPS` | Texto | Tributação ("T", "F", "I", "J") |

**Nota**: Se os nomes das colunas forem diferentes, ajuste o método `MapToRps` em `AccessRpsRepository.cs`.

## 🏃 Execução

### Compilar

```bash
dotnet build Microled.Nfe.Service.Console
```

### Executar

```bash
cd Microled.Nfe.Service.Console
dotnet run
```

Ou publicar como executável:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

O executável estará em `bin/Release/net8.0/win-x64/publish/`.

## 📊 Fluxo de Execução

1. **Lê RPS pendentes** do Access (.MDB)
   - Filtra por `Status = "P"` (configurável)
   - Limite de `BatchSize` RPS por execução

2. **Monta lote** de RPS
   - Converte registros do Access para entidades de domínio
   - Agrupa em um `RpsBatch`

3. **Assina cada RPS**
   - Usa certificado do token A3
   - Gera assinatura SHA1+RSA conforme especificação

4. **Envia para Web Service**
   - Usa `SendRpsUseCase` (mesmo caso de uso da API)
   - Retorna protocolo e chaves das NF-e geradas

5. **Atualiza Access**
   - Marca RPS como enviado (`Status = "E"`)
   - Apenas se o envio foi bem-sucedido

## 📝 Logs

O console exibe logs detalhados:

```
================================================
Iniciando processamento de RPS a partir do Access...
Banco de dados: C:\Microled\Nfe\Dados\NFS-e.mdb
Tabela: TB_RPS
Tamanho do lote: 50
================================================
Encontrados 25 RPS pendentes para processamento
Enviando lote de 25 RPS para o Web Service...
================================================
Resultado do envio:
  Sucesso: True
  Protocolo: 123456789012345
  Quantidade de RPS: 25
  NF-e geradas: 25
Atualizando status dos RPS no Access database...
RPS atualizados como enviados no MDB.
================================================
Processamento concluído.
```

## ⚠️ Tratamento de Erros

- **Certificado não encontrado**: Verifique o Thumbprint e se o token está conectado
- **Banco Access não encontrado**: Verifique o caminho em `AccessDatabase:DatabasePath`
- **Erros do Web Service**: Os RPS não serão marcados como enviados, permitindo nova tentativa
- **Erros de mapeamento**: Logs detalhados indicam qual RPS falhou

## 🔧 Personalização

### Ajustar Nomes de Colunas

Edite `AccessRpsRepository.cs`, método `MapToRps`, para ajustar os nomes das colunas conforme seu banco:

```csharp
var numeroRps = Convert.ToInt64(reader["NumeroRps"]); // Ajuste o nome aqui
```

### Ajustar Status

Configure `PendingStatus` e `SentStatus` no `appsettings.json` conforme sua convenção:

```json
"AccessDatabase": {
  "PendingStatus": "PENDENTE",
  "SentStatus": "ENVIADO"
}
```

## 📚 Dependências

- **Microled.Nfe.Service.Domain**: Entidades e value objects
- **Microled.Nfe.Service.Business**: Serviços de assinatura
- **Microled.Nfe.Service.Application**: Casos de uso
- **Microled.Nfe.Service.Infra**: Cliente SOAP, repositório Access

## 🔐 Segurança

- **Certificado A3**: A chave privada nunca sai do token
- **PIN do Token**: Será solicitado automaticamente pelo driver do token durante a assinatura
- **Logs**: Não incluem dados sensíveis (CNPJ, CPF, valores) por padrão

## 📞 Suporte

Para problemas:
1. Verifique os logs do console
2. Confirme que o certificado está no Store e o token conectado
3. Valide a estrutura do banco Access
4. Consulte a documentação principal em `README.md`

