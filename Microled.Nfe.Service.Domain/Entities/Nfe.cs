using Microled.Nfe.Service.Domain.ValueObjects;

namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// NFe (Nota Fiscal Eletrônica) entity
/// </summary>
public sealed class Nfe
{
    public NfeKey ChaveNFe { get; }
    public DateTime DataEmissao { get; }
    public DateTime DataFatoGerador { get; }
    public string Status { get; }
    public Money ValorServicos { get; }
    public Money ValorDeducoes { get; }
    public Money ValorISS { get; }
    public string? CodigoVerificacao { get; }

    public Nfe(
        NfeKey chaveNFe,
        DateTime dataEmissao,
        DateTime dataFatoGerador,
        string status,
        Money valorServicos,
        Money valorDeducoes,
        Money valorISS,
        string? codigoVerificacao = null)
    {
        ChaveNFe = chaveNFe ?? throw new ArgumentNullException(nameof(chaveNFe));
        DataEmissao = dataEmissao;
        DataFatoGerador = dataFatoGerador;
        Status = status ?? throw new ArgumentNullException(nameof(status));
        ValorServicos = valorServicos ?? throw new ArgumentNullException(nameof(valorServicos));
        ValorDeducoes = valorDeducoes ?? throw new ArgumentNullException(nameof(valorDeducoes));
        ValorISS = valorISS ?? throw new ArgumentNullException(nameof(valorISS));
        CodigoVerificacao = codigoVerificacao;
    }
}

