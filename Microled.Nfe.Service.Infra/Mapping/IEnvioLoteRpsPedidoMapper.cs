using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Infra.XmlSchemas;

namespace Microled.Nfe.Service.Infra.Mapping;

/// <summary>
/// Monta o <see cref="PedidoEnvioLoteRPS"/> exatamente como no envio SOAP (mesmo mapeamento de RPS/tpRPS e cabeçalho).
/// </summary>
public interface IEnvioLoteRpsPedidoMapper
{
    PedidoEnvioLoteRPS MapFromBatch(RpsBatch batch);
}
