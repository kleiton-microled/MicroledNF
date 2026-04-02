namespace Microled.Nfe.Service.Application.Enums;

/// <summary>
/// Regime tributário informado para cálculo auxiliar NFS-e SP (validação; regras futuras por regime).
/// Serialização JSON como string é feita pelo JsonStringEnumConverter no host (LocalAgent / API).
/// </summary>
public enum RegimeTributarioNfseSp
{
    SimplesNacional = 0,
    LucroPresumido = 1,
    LucroReal = 2
}
