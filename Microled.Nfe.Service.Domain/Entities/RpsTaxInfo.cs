using Microled.Nfe.Service.Domain.ValueObjects;

namespace Microled.Nfe.Service.Domain.Entities;

public sealed class RpsTaxInfo
{
    public Money? ValorPIS { get; }
    public Money? ValorCOFINS { get; }
    public Money? ValorINSS { get; }
    public Money? ValorIR { get; }
    public Money? ValorCSLL { get; }
    public Money? ValorIPI { get; }
    public Money? ValorCargaTributaria { get; }
    public decimal? PercentualCargaTributaria { get; }
    public string? FonteCargaTributaria { get; }
    public Money? ValorTotalRecebido { get; }
    public Money? ValorFinalCobrado { get; }
    public Money? ValorMulta { get; }
    public Money? ValorJuros { get; }
    public string? NCM { get; }

    public RpsTaxInfo(
        Money? valorPIS = null,
        Money? valorCOFINS = null,
        Money? valorINSS = null,
        Money? valorIR = null,
        Money? valorCSLL = null,
        Money? valorIPI = null,
        Money? valorCargaTributaria = null,
        decimal? percentualCargaTributaria = null,
        string? fonteCargaTributaria = null,
        Money? valorTotalRecebido = null,
        Money? valorFinalCobrado = null,
        Money? valorMulta = null,
        Money? valorJuros = null,
        string? ncm = null)
    {
        ValorPIS = valorPIS;
        ValorCOFINS = valorCOFINS;
        ValorINSS = valorINSS;
        ValorIR = valorIR;
        ValorCSLL = valorCSLL;
        ValorIPI = valorIPI;
        ValorCargaTributaria = valorCargaTributaria;
        PercentualCargaTributaria = percentualCargaTributaria;
        FonteCargaTributaria = fonteCargaTributaria;
        ValorTotalRecebido = valorTotalRecebido;
        ValorFinalCobrado = valorFinalCobrado;
        ValorMulta = valorMulta;
        ValorJuros = valorJuros;
        NCM = ncm;
    }
}
