using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Domain.Enums;

namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// RPS (Recibo Provisório de Serviços) entity
/// </summary>
public sealed class Rps
{
    public RpsKey ChaveRPS { get; }
    public TipoRps TipoRPS { get; }
    public DateOnly DataEmissao { get; }
    public StatusRps StatusRPS { get; }
    public TipoTributacao TributacaoRPS { get; }
    public RpsItem Item { get; }
    public ServiceProvider Prestador { get; }
    public ServiceCustomer? Tomador { get; }
    public string? Assinatura { get; private set; }

    /// <summary>
    /// Layout 2 (IBSCBS): classificação tributária (IBSCBS/valores/trib/gIBSCBS/cClassTrib).
    /// Vem do Access (coluna IBSCBS_CClassTrib).
    /// </summary>
    public string? IbsCbsCClassTrib { get; private set; }

    /// <summary>
    /// Layout 2 (IBSCBS): Código indicador da operação (IBSCBS/cIndOp).
    /// Vem do Access (coluna IBSCBS_CIndOp). Deve ter 6 dígitos (somente números).
    /// </summary>
    public string? IbsCbsCIndOp { get; private set; }

    public Rps(
        RpsKey chaveRPS,
        TipoRps tipoRPS,
        DateOnly dataEmissao,
        StatusRps statusRPS,
        TipoTributacao tributacaoRPS,
        RpsItem item,
        ServiceProvider prestador,
        ServiceCustomer? tomador = null)
    {
        ChaveRPS = chaveRPS ?? throw new ArgumentNullException(nameof(chaveRPS));
        TipoRPS = tipoRPS;
        DataEmissao = dataEmissao;
        StatusRPS = statusRPS;
        TributacaoRPS = tributacaoRPS;
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Prestador = prestador ?? throw new ArgumentNullException(nameof(prestador));
        Tomador = tomador;
    }

    public void SetAssinatura(string assinatura)
    {
        Assinatura = assinatura ?? throw new ArgumentNullException(nameof(assinatura));
    }

    public void SetIbsCbsCClassTrib(string? cClassTrib)
    {
        IbsCbsCClassTrib = cClassTrib;
    }

    public void SetIbsCbsCIndOp(string? cIndOp)
    {
        IbsCbsCIndOp = cIndOp;
    }
}

