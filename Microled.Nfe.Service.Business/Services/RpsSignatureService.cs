using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Business.Services;

/// <summary>
/// Service for generating RPS digital signatures according to São Paulo City Hall specification
/// Reference: NFe_Web_Service-4.pdf - Seção "Campos para assinatura do RPS – versão 2.0"
/// </summary>
public class RpsSignatureService : IRpsSignatureService
{
    private readonly ILogger<RpsSignatureService> _logger;

    public RpsSignatureService(ILogger<RpsSignatureService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds the signature string for RPS according to the actual webservice implementation
    /// Formato validado pelo webservice (erro 1206 - "String verificada"):
    /// InscricaoMunicipal(12) + SerieRPS(5) + NumeroRPS(12) + DataEmissao(8) +
    /// Tributacao(1) + Status(1) + ISSRetido(1) +
    /// ValorServicos(15) + ValorDeducoes(15) + CodigoServico(5) +
    /// IndicadorTomador(1) + CPFCNPJTomador(14)
    /// Total: 90 caracteres.
    /// </summary>
    public string BuildSignatureString(Rps rps)
    {
        if (rps == null)
            throw new ArgumentNullException(nameof(rps));

        if (rps.Prestador == null)
            throw new ArgumentException("RPS must have a Prestador", nameof(rps));

        if (rps.Item == null)
            throw new ArgumentException("RPS must have an Item", nameof(rps));

        // Inscrição Municipal (CCM) do Prestador com 12 caracteres (zeros à esquerda)
        // CORREÇÃO: A prefeitura usa 12 caracteres, não 8 como especificado no XSD
        // Referência: Retorno real do webservice mostra "000037684280" (12 chars)
        // IMPORTANTE: Deve usar o mesmo valor de InscricaoPrestador que está na ChaveRPS
        var inscricaoPrestadorStr = rps.ChaveRPS.InscricaoPrestador.ToString();
        var inscricaoMunicipal = inscricaoPrestadorStr.PadLeft(12, '0');

        // Série do RPS - FIXA com 5 caracteres (padding à direita com espaços)
        // Referência (erro 1206): "A    " (A + 4 espaços) => 5 chars
        var serieRps = (rps.ChaveRPS.SerieRps ?? "").PadRight(5, ' ');

        // Número do RPS com 12 posições (zeros à esquerda)
        // Referência: PDF - "Número do RPS com 12 posições. Caso o Número do RPS tenha menos de 12 caracteres, o mesmo deverá ser completado com zeros à esquerda."
        var numeroRps = rps.ChaveRPS.NumeroRps.ToString().PadLeft(12, '0');

        // Data da emissão do RPS no formato AAAAMMDD
        // Referência: PDF - "Data da emissão do RPS no formato AAAAMMDD."
        var dataEmissao = rps.DataEmissao.ToString("yyyyMMdd");

        // Tipo de Tributação do RPS com uma posição
        // Referência: PDF - "Tipo de Tributação do RPS com uma posição (sendo T: para Tributação no municipio de São Paulo; F: para Tributação fora do municipio de São Paulo; I: para Isento; J: para ISS Suspenso por Decisão Judicial)."
        var tipoTributacao = ((char)rps.TributacaoRPS).ToString();

        // Status do RPS com uma posição
        // Referência: PDF - "Status do RPS com uma posição (sendo N: Normal, C: Cancelado; E: Extraviado)."
        var statusRps = ((char)rps.StatusRPS).ToString();

        // ISS Retido com uma posição (S: Sim, N: Não)
        // Referência: PDF - "ISS Retido com uma posição (sendo S: ISS Retido; N: Nota Fiscal sem ISS Retido)."
        var issRetido = rps.Item.IssRetido == IssRetido.Sim ? "S" : "N";

        // Valor dos Serviços (15 posições, centavos) — deve coincidir com ValorFinalCobrado no XML quando tributos informa valorFinalCobrado.
        // Ver Rps.GetValorParaAssinaturaDigital() e MapRpsToTpRPS (erro 1206 se assinar com Item.ValorServicos e XML com outro valor).
        var valorAssinatura = rps.GetValorParaAssinaturaDigital();
        var valorServicosCentavos = (long)Math.Round(valorAssinatura * 100m, 0, MidpointRounding.AwayFromZero);
        var valorServicos = valorServicosCentavos.ToString().PadLeft(15, '0');

        // Valor das Deduções com 15 posições e sem separador de milhar e decimal
        // Referência: PDF - "Valor das Deduções com 15 posições e sem separador de milhar e decimal."
        var valorDeducoesCentavos = (long)Math.Round(rps.Item.ValorDeducoes.Value * 100m, 0, MidpointRounding.AwayFromZero);
        var valorDeducoes = valorDeducoesCentavos.ToString().PadLeft(15, '0');

        // Código do Serviço com 5 posições
        // Referência: PDF - "Código do Serviço com 5 posições."
        var codigoServico = rps.Item.CodigoServico.ToString().PadLeft(5, '0');

        // Indicador + CPF/CNPJ do tomador:
        // 1 = CPF, 2 = CNPJ, 3 = Não informado
        // CPF/CNPJ sempre com 14 posições (zeros à esquerda). Se indicador=3, preencher com 14 zeros.
        var indicadorTomador = "3";
        var cpfCnpjTomador = "00000000000000";
        if (rps.Tomador?.CpfCnpj != null)
        {
            indicadorTomador = rps.Tomador.CpfCnpj.IsCpf ? "1" : "2";
            cpfCnpjTomador = rps.Tomador.CpfCnpj.GetValue().PadLeft(14, '0');
        }

        // Concatena todos os campos (90 chars)
        // 12 (InscricaoMunicipal) + 5 (SerieRPS) + 12 (NumeroRPS) + 8 (DataEmissao) +
        // 1 (Tributacao) + 1 (Status) + 1 (ISSRetido) +
        // 15 (ValorServicos) + 15 (ValorDeducoes) + 5 (CodigoServico) +
        // 1 (IndicadorTomador) + 14 (CPF/CNPJTomador)
        var signatureString = inscricaoMunicipal + serieRps + numeroRps + dataEmissao + tipoTributacao + 
               statusRps + issRetido + valorServicos + valorDeducoes + codigoServico + indicadorTomador + cpfCnpjTomador;

        // Log estruturado por fatias (índices) + valores humanos (útil para diffs do 1206)
        LogSignatureSlices(signatureString, rps, valorServicosCentavos, valorDeducoesCentavos);

        // Log para debug
        _logger.LogDebug(
            "Signature string built: Length={Length}, InscricaoMunicipal={IM} ({IMLen}), SerieRPS={Serie} ({SerieLen}), IndicadorTomador={Indicador}",
            signatureString.Length,
            inscricaoMunicipal,
            inscricaoMunicipal.Length,
            serieRps.Replace(" ", "·"),
            serieRps.Length,
            indicadorTomador
        );

        // Log detalhado da string de assinatura para debug
        LogSignatureString(signatureString, rps);
        
        // Log em nível Information para garantir visibilidade
        var visibleString = signatureString.Replace(' ', '·');
        _logger.LogInformation(
            "RPS Signature String Generated - RPS {InscricaoPrestador}-{NumeroRps}: Length={Length}, String={SignatureString}",
            rps.ChaveRPS.InscricaoPrestador,
            rps.ChaveRPS.NumeroRps,
            signatureString.Length,
            visibleString
        );

        return signatureString;
    }

    /// <summary>
    /// Signs the RPS using SHA1 + RSA and returns Base64 encoded signature
    /// Reference: NFe_Web_Service-4.pdf - Algoritmo: SHA1 com RSA, resultado em Base64
    /// </summary>
    public string SignRps(Rps rps, X509Certificate2 certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));

        var signatureString = BuildSignatureString(rps);

        try
        {
            // Get RSA private key from certificate
            var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new InvalidOperationException("Certificate does not contain a private key. The certificate must have a private key for signing.");
            }

            // Convert signature string to bytes (ASCII encoding)
            var dataToSign = Encoding.ASCII.GetBytes(signatureString);

            // Sign using SHA1 with RSA and PKCS1 padding
            // Reference: PDF - Algoritmo SHA1 com RSA
            var signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

            // Convert to Base64
            var base64Signature = Convert.ToBase64String(signature);

            _logger.LogInformation("RPS signature generated successfully for RPS {InscricaoPrestador}-{NumeroRps}",
                rps.ChaveRPS.InscricaoPrestador, rps.ChaveRPS.NumeroRps);

            return base64Signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing RPS {InscricaoPrestador}-{NumeroRps}",
                rps.ChaveRPS.InscricaoPrestador, rps.ChaveRPS.NumeroRps);
            throw;
        }
    }

    /// <summary>
    /// Logs the signature string with detailed information for debugging
    /// Shows spaces as visible characters (·) and provides length information
    /// </summary>
    private void LogSignatureString(string signatureString, Rps rps)
    {
        // Replace spaces with visible character for logging
        var visibleString = signatureString.Replace(' ', '·');
        
        _logger.LogDebug(
            "RPS Signature String for {InscricaoPrestador}-{NumeroRps}: Length={Length}, String={SignatureString}",
            rps.ChaveRPS.InscricaoPrestador,
            rps.ChaveRPS.NumeroRps,
            signatureString.Length,
            visibleString
        );
    }

    /// <summary>
    /// Compares our signature string with the one returned by the prefeitura in error 1206
    /// Extracts the string from error message and provides detailed diff
    /// </summary>
    /// <param name="ourString">The signature string we generated</param>
    /// <param name="prefeituraString">The string returned by prefeitura in error message</param>
    /// <param name="rps">The RPS being signed (for logging context)</param>
    /// <returns>True if strings match, false otherwise</returns>
    public bool CompareSignatureStrings(string ourString, string prefeituraString, Rps rps)
    {
        // Required log format: length + first mismatching index (char by char)
        if (ourString == prefeituraString)
        {
            _logger.LogInformation(
                "RPS signature string MATCH for {InscricaoPrestador}-{NumeroRps}. Length={Length}",
                rps.ChaveRPS.InscricaoPrestador,
                rps.ChaveRPS.NumeroRps,
                ourString.Length);
            return true;
        }

        var ourLen = ourString?.Length ?? 0;
        var prefLen = prefeituraString?.Length ?? 0;
        var minLen = Math.Min(ourLen, prefLen);

        var mismatchIndex = -1;
        for (var i = 0; i < minLen; i++)
        {
            if (ourString[i] != prefeituraString[i])
            {
                mismatchIndex = i;
                break;
            }
        }

        // If all common chars match, the first mismatch is at minLen (length differs)
        if (mismatchIndex == -1)
            mismatchIndex = minLen;

        char ourCharAt = mismatchIndex < ourLen ? ourString[mismatchIndex] : '\0';
        char prefCharAt = mismatchIndex < prefLen ? prefeituraString[mismatchIndex] : '\0';

        string DisplayChar(char c) => c switch
        {
            ' ' => "·",
            '\0' => "∅",
            _ => c.ToString()
        };

        _logger.LogError(
            "RPS signature string MISMATCH for {InscricaoPrestador}-{NumeroRps}. OurLength={OurLength}, VerifiedLength={VerifiedLength}, FirstDiffIndex={Index}, OurChar='{OurChar}', VerifiedChar='{VerifiedChar}'",
            rps.ChaveRPS.InscricaoPrestador,
            rps.ChaveRPS.NumeroRps,
            ourLen,
            prefLen,
            mismatchIndex,
            DisplayChar(ourCharAt),
            DisplayChar(prefCharAt));

        // Log por fatias do nosso lado para localizar rapidamente qual campo divergiu
        try
        {
            var valorServicosCentavos = (long)Math.Round(rps.Item.ValorServicos.Value * 100m, 0, MidpointRounding.AwayFromZero);
            var valorDeducoesCentavos = (long)Math.Round(rps.Item.ValorDeducoes.Value * 100m, 0, MidpointRounding.AwayFromZero);
            LogSignatureSlices(ourString, rps, valorServicosCentavos, valorDeducoesCentavos);
        }
        catch
        {
            // ignore: best-effort logging
        }

        // Keep full strings at Debug to avoid noisy logs in production
        _logger.LogDebug("Our string:      {Our}", (ourString ?? "").Replace(' ', '·'));
        _logger.LogDebug("Verified string: {Ver}", (prefeituraString ?? "").Replace(' ', '·'));

        return false;
    }

    /// <summary>
    /// Logs signature string slices with fixed indices (for troubleshooting error 1206).
    /// Also logs the "human" values used to build money fields.
    /// </summary>
    private void LogSignatureSlices(string signatureString, Rps rps, long valorServicosCentavos, long valorDeducoesCentavos)
    {
        if (string.IsNullOrEmpty(signatureString))
            return;

        // Indices based on the 90-char layout
        // IM [0..11] (12)
        // Serie [12..16] (5)
        // Numero [17..28] (12)
        // Data [29..36] (8)
        // Trib/Status/ISS [37..39] (3)
        // ValorServicos [40..54] (15)
        // ValorDeducoes [55..69] (15)
        // CodigoServico [70..74] (5)
        // Rest [75..89] (15) => IndicadorTomador(1)+CPFCNPJTomador(14)
        string Slice(int start, int length)
        {
            if (signatureString.Length < start)
                return "";
            var maxLen = Math.Min(length, signatureString.Length - start);
            return signatureString.Substring(start, maxLen);
        }

        var im = Slice(0, 12);
        var serie = Slice(12, 5);
        var numero = Slice(17, 12);
        var data = Slice(29, 8);
        var flags = Slice(37, 3);
        var valServ = Slice(40, 15);
        var valDed = Slice(55, 15);
        var codServ = Slice(70, 5);
        var rest = Slice(75, 15);

        string V(string s) => (s ?? "").Replace(' ', '·');

        _logger.LogInformation(
            "RPS Signature slices {InscricaoPrestador}-{NumeroRps}: Len={Len} | IM[0..11]={IM} | Serie[12..16]={Serie} | Num[17..28]={Num} | Data[29..36]={Data} | F[37..39]={Flags} | ValServ[40..54]={ValServ} | ValDed[55..69]={ValDed} | CodServ[70..74]={CodServ} | Rest[75..89]={Rest}",
            rps.ChaveRPS.InscricaoPrestador,
            rps.ChaveRPS.NumeroRps,
            signatureString.Length,
            V(im), V(serie), V(numero), V(data), V(flags), V(valServ), V(valDed), V(codServ), V(rest));

        _logger.LogInformation(
            "RPS Signature money inputs {InscricaoPrestador}-{NumeroRps}: ValorServicos={ValorServicos} ValorDeducoes={ValorDeducoes} | valorServicosCentavos={ValorServicosCentavos} valorDeducoesCentavos={ValorDeducoesCentavos}",
            rps.ChaveRPS.InscricaoPrestador,
            rps.ChaveRPS.NumeroRps,
            rps.Item.ValorServicos.Value,
            rps.Item.ValorDeducoes.Value,
            valorServicosCentavos,
            valorDeducoesCentavos);
    }

    /// <summary>
    /// Extracts the signature string from error 1206 message
    /// Looks for pattern "String verificada (...)" in the error description
    /// Improved to handle spaces and special characters robustly
    /// </summary>
    /// <param name="errorMessage">The error message from prefeitura</param>
    /// <returns>The extracted string, or null if not found</returns>
    public string? ExtractSignatureStringFromError(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return null;

        // Pattern: "String verificada (....)" or similar
        // Try to extract content between parentheses, including spaces
        var patterns = new[]
        {
            @"String verificada\s*\(([^)]+)\)",  // "String verificada (....)"
            @"String verificada:\s*([^\n\r]+)", // "String verificada: ...."
            @"String verificada\s+([^\n\r]+)",   // "String verificada ...."
            @"\(([^)]+)\)",                       // Fallback: any content in parentheses
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(errorMessage, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var extracted = match.Groups[1].Value;
                // Don't trim - preserve spaces as they are part of the signature string
                if (!string.IsNullOrEmpty(extracted))
                {
                    _logger.LogDebug("Extracted signature string from error message: {Length} characters", extracted.Length);
                    return extracted;
                }
            }
        }

        _logger.LogWarning("Could not extract signature string from error message");
        return null;
    }

    /// <summary>
    /// Detects the SeriePaddingStrategy used in the verified string
    /// Analyzes the position after InscricaoMunicipal (12 chars) to determine padding
    /// </summary>
    /// <param name="verifiedString">The signature string returned by prefeitura</param>
    /// <param name="serieRps">The actual serie RPS value (e.g., "A")</param>
    /// <returns>The detected strategy</returns>
    public SeriePaddingStrategy DetectSerieStrategy(string verifiedString, string serieRps)
    {
        if (string.IsNullOrEmpty(verifiedString) || string.IsNullOrEmpty(serieRps))
        {
            _logger.LogWarning("Cannot detect serie strategy: verifiedString or serieRps is empty");
            return SeriePaddingStrategy.VariablePlus1Space; // Default
        }

        // InscricaoMunicipal is 12 chars, so serie starts at position 12
        const int inscricaoMunicipalLength = 12;
        if (verifiedString.Length < inscricaoMunicipalLength + 1)
        {
            _logger.LogWarning("Verified string too short to detect serie strategy");
            return SeriePaddingStrategy.VariablePlus1Space; // Default
        }

        // Find where the serie ends (start of NumeroRPS which is 12 digits)
        var serieStart = inscricaoMunicipalLength;
        var serieEnd = serieStart;
        
        // Find the end of serie by looking for the start of NumeroRPS (12 consecutive digits)
        for (int i = serieStart; i < verifiedString.Length - 12; i++)
        {
            var next12Chars = verifiedString.Substring(i, Math.Min(12, verifiedString.Length - i));
            if (next12Chars.All(char.IsDigit) && next12Chars.Length == 12)
            {
                serieEnd = i;
                break;
            }
        }

        if (serieEnd == serieStart)
        {
            _logger.LogWarning("Could not find NumeroRPS in verified string, using default strategy");
            return SeriePaddingStrategy.VariablePlus1Space;
        }

        var seriePart = verifiedString.Substring(serieStart, serieEnd - serieStart);
        var serieLength = seriePart.Length;
        var serieBaseLength = serieRps.Length;

        _logger.LogDebug(
            "Detecting serie strategy: SerieRPS='{SerieRps}', SeriePart='{SeriePart}' (length={Length})",
            serieRps,
            seriePart.Replace(' ', '·'),
            serieLength
        );

        // Count spaces after the serie base
        var spacesAfterSerie = serieLength - serieBaseLength;

        if (spacesAfterSerie == 4 && serieLength == 5)
        {
            _logger.LogInformation("Detected strategy: Fixed5Chars (serie length: {Length})", serieLength);
            return SeriePaddingStrategy.Fixed5Chars;
        }
        else if (spacesAfterSerie == 1)
        {
            _logger.LogInformation("Detected strategy: VariablePlus1Space (serie length: {Length})", serieLength);
            return SeriePaddingStrategy.VariablePlus1Space;
        }
        else if (spacesAfterSerie == 0 && serieLength == serieBaseLength)
        {
            _logger.LogInformation("Detected strategy: VariableNoSpace (serie length: {Length})", serieLength);
            return SeriePaddingStrategy.VariableNoSpace;
        }
        else
        {
            _logger.LogWarning(
                "Could not determine strategy (spaces={Spaces}, length={Length}), using default",
                spacesAfterSerie,
                serieLength
            );
            return SeriePaddingStrategy.VariablePlus1Space;
        }
    }

    /// <summary>
    /// Builds signature string with a specific SeriePaddingStrategy
    /// </summary>
    /// <param name="rps">The RPS entity</param>
    /// <param name="strategy">The padding strategy for SerieRPS</param>
    /// <returns>The signature string</returns>
    private string BuildSignatureStringWithStrategy(Rps rps, SeriePaddingStrategy strategy)
    {
        if (rps == null)
            throw new ArgumentNullException(nameof(rps));

        if (rps.Prestador == null)
            throw new ArgumentException("RPS must have a Prestador", nameof(rps));

        if (rps.Item == null)
            throw new ArgumentException("RPS must have an Item", nameof(rps));

        // Inscrição Municipal (CCM) do Prestador com 12 caracteres (zeros à esquerda)
        var inscricaoPrestadorStr = rps.ChaveRPS.InscricaoPrestador.ToString();
        var inscricaoMunicipal = inscricaoPrestadorStr.PadLeft(12, '0');

        // Série do RPS - aplicar estratégia selecionada
        var serieRpsBase = rps.ChaveRPS.SerieRps ?? "";
        var serieRps = strategy switch
        {
            SeriePaddingStrategy.Fixed5Chars => serieRpsBase.PadRight(5, ' '),
            SeriePaddingStrategy.VariablePlus1Space => serieRpsBase + " ",
            SeriePaddingStrategy.VariableNoSpace => serieRpsBase,
            _ => serieRpsBase + " " // Default
        };

        // Número do RPS com 12 posições (zeros à esquerda)
        var numeroRps = rps.ChaveRPS.NumeroRps.ToString().PadLeft(12, '0');

        // Data da emissão do RPS no formato AAAAMMDD
        var dataEmissao = rps.DataEmissao.ToString("yyyyMMdd");

        // Tipo de Tributação do RPS com uma posição
        var tipoTributacao = ((char)rps.TributacaoRPS).ToString();

        // Status do RPS com uma posição
        var statusRps = ((char)rps.StatusRPS).ToString();

        // ISS Retido com uma posição (S: Sim, N: Não)
        var issRetido = rps.Item.IssRetido == IssRetido.Sim ? "S" : "N";

        // Valor dos Serviços com 15 posições e sem separador de milhar e decimal
        var valorServicos = ((long)(rps.Item.ValorServicos.Value * 100)).ToString().PadLeft(15, '0');

        // Valor das Deduções com 15 posições e sem separador de milhar e decimal
        var valorDeducoes = ((long)(rps.Item.ValorDeducoes.Value * 100)).ToString().PadLeft(15, '0');

        // Código do Serviço com 5 posições
        var codigoServico = rps.Item.CodigoServico.ToString().PadLeft(5, '0');

        // Indicador + CPF/CNPJ do tomador (1 + 14)
        var indicadorTomador = "3";
        var cpfCnpjTomador = "00000000000000";
        if (rps.Tomador?.CpfCnpj != null)
        {
            indicadorTomador = rps.Tomador.CpfCnpj.IsCpf ? "1" : "2";
            cpfCnpjTomador = rps.Tomador.CpfCnpj.GetValue().PadLeft(14, '0');
        }

        // Concatena todos os campos
        var signatureString = inscricaoMunicipal + serieRps + numeroRps + dataEmissao + tipoTributacao + 
               statusRps + issRetido + valorServicos + valorDeducoes + codigoServico + indicadorTomador + cpfCnpjTomador;

        return signatureString;
    }

    /// <summary>
    /// Tries to automatically fix the signature string by testing different strategies
    /// Returns the fixed string if a match is found, null otherwise
    /// </summary>
    /// <param name="rps">The RPS being signed</param>
    /// <param name="verifiedString">The signature string returned by prefeitura</param>
    /// <returns>The fixed signature string, or null if no match found</returns>
    public string? AutoFixSignatureString(Rps rps, string verifiedString)
    {
        if (rps == null)
            throw new ArgumentNullException(nameof(rps));

        if (string.IsNullOrEmpty(verifiedString))
            return null;

        _logger.LogInformation(
            "🔧 Attempting to auto-fix signature string for RPS {InscricaoPrestador}-{NumeroRps}",
            rps.ChaveRPS.InscricaoPrestador,
            rps.ChaveRPS.NumeroRps
        );

        // First, try to detect the strategy from the verified string
        var serieRps = rps.ChaveRPS.SerieRps ?? "";
        var detectedStrategy = DetectSerieStrategy(verifiedString, serieRps);

        // Test all strategies
        var strategies = new[]
        {
            SeriePaddingStrategy.Fixed5Chars,
            SeriePaddingStrategy.VariablePlus1Space,
            SeriePaddingStrategy.VariableNoSpace
        };

        foreach (var strategy in strategies)
        {
            var testString = BuildSignatureStringWithStrategy(rps, strategy);
            
            if (testString == verifiedString)
            {
                _logger.LogInformation(
                    "✅ Auto-fix successful! Strategy {Strategy} produces matching string for RPS {InscricaoPrestador}-{NumeroRps}",
                    strategy,
                    rps.ChaveRPS.InscricaoPrestador,
                    rps.ChaveRPS.NumeroRps
                );
                return testString;
            }
        }

        // If no exact match, try the detected strategy and log the difference
        var detectedString = BuildSignatureStringWithStrategy(rps, detectedStrategy);
        _logger.LogWarning(
            "⚠️ Auto-fix could not find exact match. Detected strategy: {Strategy}. Testing differences...",
            detectedStrategy
        );

        CompareSignatureStrings(detectedString, verifiedString, rps);

        return null; // No match found
    }
}

