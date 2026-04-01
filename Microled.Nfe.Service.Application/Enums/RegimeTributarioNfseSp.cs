using System.Text.Json.Serialization;

namespace Microled.Nfe.Service.Application.Enums;

/// <summary>
/// Regime tributário informado para cálculo auxiliar NFS-e SP (validação; regras futuras por regime).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegimeTributarioNfseSp
{
    SimplesNacional = 0,
    LucroPresumido = 1,
    LucroReal = 2
}
