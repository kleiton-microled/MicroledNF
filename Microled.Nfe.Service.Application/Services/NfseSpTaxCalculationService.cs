using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.Service.Application.Services;

/// <summary>
/// Cálculo tributário auxiliar para NFS-e São Paulo (escopo fechado; sem integração SOAP/XML).
/// </summary>
public sealed class NfseSpTaxCalculationService : INfseSpTaxCalculationService
{
    public NfseSpTaxCalculationResponse Calculate(NfseSpTaxCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var memoria = new List<string>();
        AppendHeaderMemoria(request, memoria);

        var baseIss = ComputeBaseCalculoIss(request, memoria);
        var baseFederal = ComputeBaseCalculoFederal(request, memoria);
        var valorIss = ComputeValorIss(baseIss, request.AliquotaIss, memoria);

        var valorPis = ComputeRetencaoFederal(
            request.ReterPis, baseFederal, request.AliquotaPis, "PIS", memoria);
        var valorCofins = ComputeRetencaoFederal(
            request.ReterCofins, baseFederal, request.AliquotaCofins, "COFINS", memoria);
        var valorCsll = ComputeRetencaoFederal(
            request.ReterCsll, baseFederal, request.AliquotaCsll, "CSLL", memoria);
        var valorIr = ComputeRetencaoFederal(
            request.ReterIr, baseFederal, request.AliquotaIr, "IR", memoria);
        var valorInss = ComputeRetencaoFederal(
            request.ReterInss, baseFederal, request.AliquotaInss, "INSS", memoria);

        var totalFederais = valorPis + valorCofins + valorCsll + valorIr + valorInss;
        memoria.Add(
            "Total retenções federais = soma(PIS, COFINS, CSLL, IR, INSS) conforme retenções ativas.");

        var totalRetencoes = totalFederais;
        if (request.IssRetido)
        {
            totalRetencoes += valorIss;
            memoria.Add("ISS retido: valor do ISS somado ao total de retenções.");
        }
        else
        {
            memoria.Add("ISS não retido: valor do ISS não compõe o total de retenções.");
        }

        var valorLiquido = ComputeValorLiquido(request, totalRetencoes, memoria);

        var response = new NfseSpTaxCalculationResponse
        {
            BaseCalculoIss = baseIss,
            BaseCalculoFederal = baseFederal,
            ValorIss = valorIss,
            ValorPis = valorPis,
            ValorCofins = valorCofins,
            ValorCsll = valorCsll,
            ValorIr = valorIr,
            ValorInss = valorInss,
            TotalRetencoesFederais = totalFederais,
            TotalRetencoes = totalRetencoes,
            ValorLiquido = valorLiquido
        };

        if (request.ArredondarNaCasaFiscal)
        {
            ApplyFiscalRounding(request, response, memoria);
        }
        else
        {
            memoria.Add("Arredondamento na casa fiscal desligado: sem arredondamento AwayFromZero nos retornos.");
        }

        if (response.ValorLiquido < 0)
        {
            memoria.Add(
                "Atenção: valor líquido negativo — conferir cenário financeiro; o valor foi mantido conforme a regra informada.");
        }

        response.MemoriaCalculo = memoria;
        return response;
    }

    private static void AppendHeaderMemoria(NfseSpTaxCalculationRequest request, List<string> memoria)
    {
        memoria.Add(
            $"Regime tributário: {request.RegimeTributario} (recebido para evolução futura; nesta versão não altera alíquotas).");
        memoria.Add($"Código de serviço: {request.CodigoServico}.");
    }

    private static decimal ComputeBaseCalculoIss(NfseSpTaxCalculationRequest request, List<string> memoria)
    {
        memoria.Add("Base ISS = valorServico - valorDeducoes - descontoIncondicional.");
        var raw = request.ValorServico - request.ValorDeducoes - request.DescontoIncondicional;
        if (raw < 0)
        {
            memoria.Add("Base ISS ajustada para 0 (resultado intermediário seria negativo).");
            return 0;
        }

        return raw;
    }

    private static decimal ComputeBaseCalculoFederal(NfseSpTaxCalculationRequest request, List<string> memoria)
    {
        decimal raw;
        if (request.BaseFederalSobreValorLiquido)
        {
            memoria.Add(
                "Base federal = valorServico - valorDeducoes - descontoIncondicional (baseFederalSobreValorLiquido = true).");
            raw = request.ValorServico - request.ValorDeducoes - request.DescontoIncondicional;
        }
        else
        {
            memoria.Add("Base federal = valorServico - descontoIncondicional.");
            raw = request.ValorServico - request.DescontoIncondicional;
        }

        if (raw < 0)
        {
            memoria.Add("Base federal ajustada para 0 (resultado intermediário seria negativo).");
            return 0;
        }

        return raw;
    }

    private static decimal ComputeValorIss(decimal baseCalculoIss, decimal aliquotaIss, List<string> memoria)
    {
        memoria.Add("ISS = baseCalculoIss × aliquotaIss (ex.: 5% → 0,05).");
        return baseCalculoIss * aliquotaIss;
    }

    private static decimal ComputeRetencaoFederal(
        bool reter,
        decimal baseFederal,
        decimal aliquota,
        string nome,
        List<string> memoria)
    {
        if (!reter)
        {
            memoria.Add($"{nome}: retenção desligada → 0.");
            return 0;
        }

        memoria.Add($"{nome} = baseCalculoFederal × aliquota{nome}.");
        return baseFederal * aliquota;
    }

    private static decimal ComputeValorLiquido(
        NfseSpTaxCalculationRequest request,
        decimal totalRetencoes,
        List<string> memoria)
    {
        memoria.Add("Valor líquido = valorServico - totalRetencoes - descontoIncondicional.");
        var liquido = request.ValorServico - totalRetencoes - request.DescontoIncondicional;

        if (request.ConsiderarDescontoCondicionalNoLiquido)
        {
            memoria.Add("Subtrai também descontoCondicional (considerarDescontoCondicionalNoLiquido = true).");
            liquido -= request.DescontoCondicional;
        }
        else
        {
            memoria.Add(
                "descontoCondicional não subtraído do líquido neste modo (vide considerarDescontoCondicionalNoLiquido).");
        }

        return liquido;
    }

    private static void ApplyFiscalRounding(
        NfseSpTaxCalculationRequest request,
        NfseSpTaxCalculationResponse r,
        List<string> memoria)
    {
        memoria.Add("Arredondamento fiscal: 2 decimais, MidpointRounding.AwayFromZero, por campo monetário.");

        r.BaseCalculoIss = RoundMoney(r.BaseCalculoIss);
        r.BaseCalculoFederal = RoundMoney(r.BaseCalculoFederal);
        r.ValorIss = RoundMoney(r.ValorIss);
        r.ValorPis = RoundMoney(r.ValorPis);
        r.ValorCofins = RoundMoney(r.ValorCofins);
        r.ValorCsll = RoundMoney(r.ValorCsll);
        r.ValorIr = RoundMoney(r.ValorIr);
        r.ValorInss = RoundMoney(r.ValorInss);

        r.TotalRetencoesFederais = RoundMoney(
            r.ValorPis + r.ValorCofins + r.ValorCsll + r.ValorIr + r.ValorInss);

        r.TotalRetencoes = r.TotalRetencoesFederais;
        if (request.IssRetido)
            r.TotalRetencoes += r.ValorIss;

        r.TotalRetencoes = RoundMoney(r.TotalRetencoes);

        r.ValorLiquido = request.ValorServico - r.TotalRetencoes - request.DescontoIncondicional;
        if (request.ConsiderarDescontoCondicionalNoLiquido)
            r.ValorLiquido -= request.DescontoCondicional;

        r.ValorLiquido = RoundMoney(r.ValorLiquido);
    }

    private static decimal RoundMoney(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
