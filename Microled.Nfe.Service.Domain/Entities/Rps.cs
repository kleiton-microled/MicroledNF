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
    public RpsTaxInfo? Tributos { get; private set; }
    public RpsIbsCbsInfo? IbsCbs { get; private set; }

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

    public void SetTributos(RpsTaxInfo? tributos)
    {
        Tributos = tributos;
    }

    public void SetIbsCbs(RpsIbsCbsInfo? ibsCbs)
    {
        IbsCbs = ibsCbs;

        if (ibsCbs == null)
        {
            IbsCbsCClassTrib = null;
            IbsCbsCIndOp = null;
            return;
        }

        IbsCbsCClassTrib = ibsCbs.CClassTrib;
        IbsCbsCIndOp = ibsCbs.CIndOp;
    }

    public void SetIbsCbsCClassTrib(string? cClassTrib)
    {
        IbsCbsCClassTrib = cClassTrib;
    }

    public void SetIbsCbsCIndOp(string? cIndOp)
    {
        IbsCbsCIndOp = cIndOp;
    }

    /// <summary>
    /// Valor de <c>ValorFinalCobrado</c> a enviar no XML (SOAP) e na DANFSE; deve coincidir com a assinatura (erro 1206).
    /// Quando <c>Tributos.ValorFinalCobrado</c> é <b>0</b> e <c>Item.ValorServicos</c> é positivo, trata-se como
    /// valor ausente/errado (ex.: default numérico no banco) e usa-se o valor do item.
    /// </summary>
    public decimal GetValorFinalCobradoParaEnvio()
    {
        var fromTributos = Tributos?.ValorFinalCobrado?.Value;
        if (fromTributos is null)
        {
            return Item.ValorServicos.Value;
        }

        if (fromTributos == 0m && Item.ValorServicos.Value != 0m)
        {
            return Item.ValorServicos.Value;
        }

        return fromTributos.Value;
    }

    /// <summary>
    /// Valor para os 15 dígitos de "valor dos serviços" na assinatura digital do RPS.
    /// Deve ser idêntico ao <c>ValorFinalCobrado</c> enviado no XML; ver <see cref="GetValorFinalCobradoParaEnvio"/>.
    /// </summary>
    public decimal GetValorParaAssinaturaDigital()
        => GetValorFinalCobradoParaEnvio();
}

