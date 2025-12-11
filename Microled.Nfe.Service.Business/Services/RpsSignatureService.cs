using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
    /// Builds the signature string for RPS according to the official specification (85 positions)
    /// Reference: NFe_Web_Service-4.pdf - Campos para assinatura do RPS (versão 2.0)
    /// Format: InscricaoMunicipal(8) + SerieRPS(5) + NumeroRPS(12) + DataEmissao(8) + TipoTributacao(1) + StatusRPS(1) + ISSRetido(1) + ValorServicos(15) + ValorDeducoes(15) + CodigoServico(5) + CPF/CNPJTomador(14) = 85
    /// </summary>
    public string BuildSignatureString(Rps rps)
    {
        if (rps == null)
            throw new ArgumentNullException(nameof(rps));

        if (rps.Prestador == null)
            throw new ArgumentException("RPS must have a Prestador", nameof(rps));

        if (rps.Item == null)
            throw new ArgumentException("RPS must have an Item", nameof(rps));

        // Inscrição Municipal (CCM) do Prestador com 8 caracteres (zeros à esquerda)
        // Referência: PDF - "Inscrição Municipal (CCM) do Prestador com 8 caracteres. Caso o CCM do Prestador tenha menos de 8 caracteres, o mesmo deverá ser completado com zeros à esquerda."
        var inscricaoMunicipal = rps.Prestador.InscricaoMunicipal.ToString().PadLeft(8, '0');

        // Série do RPS com 5 posições (espaços à direita)
        // Referência: PDF - "Série do RPS com 5 posições. Caso a Série do RPS tenha menos de 5 caracteres, o mesmo deverá ser completado com espaços em branco à direita."
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

        // Valor dos Serviços com 15 posições e sem separador de milhar e decimal
        // Referência: PDF - "Valor dos Serviços com 15 posições e sem separador de milhar e decimal."
        // Converte para centavos (multiplica por 100) e formata sem decimais
        var valorServicos = ((long)(rps.Item.ValorServicos.Value * 100)).ToString().PadLeft(15, '0');

        // Valor das Deduções com 15 posições e sem separador de milhar e decimal
        // Referência: PDF - "Valor das Deduções com 15 posições e sem separador de milhar e decimal."
        var valorDeducoes = ((long)(rps.Item.ValorDeducoes.Value * 100)).ToString().PadLeft(15, '0');

        // Código do Serviço com 5 posições
        // Referência: PDF - "Código do Serviço com 5 posições."
        var codigoServico = rps.Item.CodigoServico.ToString().PadLeft(5, '0');

        // CPF/CNPJ do tomador com 14 posições (zeros à esquerda se não informado)
        // Referência: PDF - "CPF/CNPJ do tomador com 14 posições. Sem formatação (ponto, traço, barra, ....). Completar com zeros à esquerda caso seja necessário. Se o Indicador do CPF/CNPJ for 3 (não-informado), preencher com 14 zeros."
        var cpfCnpjTomador = rps.Tomador?.CpfCnpj?.GetValue() ?? "00000000000000";
        cpfCnpjTomador = cpfCnpjTomador.PadLeft(14, '0');

        // Concatena todos os campos (total: 85 caracteres)
        // 8 + 5 + 12 + 8 + 1 + 1 + 1 + 15 + 15 + 5 + 14 = 85
        // Note: According to the specification, it should be 85 characters, not 86
        var signatureString = inscricaoMunicipal + serieRps + numeroRps + dataEmissao + tipoTributacao + 
               statusRps + issRetido + valorServicos + valorDeducoes + codigoServico + cpfCnpjTomador;

        if (signatureString.Length != 85)
        {
            throw new InvalidOperationException($"Signature string must have exactly 85 characters, but has {signatureString.Length}");
        }

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
}

