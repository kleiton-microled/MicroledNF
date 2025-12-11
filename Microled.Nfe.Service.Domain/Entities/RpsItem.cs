using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Domain.Enums;

namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// RPS item (service item)
/// </summary>
public sealed class RpsItem
{
    public int CodigoServico { get; }
    public string Discriminacao { get; }
    public Money ValorServicos { get; }
    public Money ValorDeducoes { get; }
    public Aliquota AliquotaServicos { get; }
    public IssRetido IssRetido { get; }
    public TipoTributacao TributacaoRPS { get; }

    public RpsItem(
        int codigoServico,
        string discriminacao,
        Money valorServicos,
        Money valorDeducoes,
        Aliquota aliquotaServicos,
        IssRetido issRetido,
        TipoTributacao tributacaoRPS)
    {
        if (codigoServico <= 0)
            throw new ArgumentException("CodigoServico must be greater than zero", nameof(codigoServico));

        CodigoServico = codigoServico;
        Discriminacao = discriminacao ?? throw new ArgumentNullException(nameof(discriminacao));
        ValorServicos = valorServicos ?? throw new ArgumentNullException(nameof(valorServicos));
        ValorDeducoes = valorDeducoes ?? throw new ArgumentNullException(nameof(valorDeducoes));
        AliquotaServicos = aliquotaServicos ?? throw new ArgumentNullException(nameof(aliquotaServicos));
        IssRetido = issRetido;
        TributacaoRPS = tributacaoRPS;
    }
}

