namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Request DTO for sending RPS batch
/// </summary>
public class SendRpsRequestDto
{
    public ServiceProviderDto Prestador { get; set; } = null!;
    public List<RpsDto> RpsList { get; set; } = new();
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public bool Transacao { get; set; } = true;
}

public class ServiceProviderDto
{
    public string CpfCnpj { get; set; } = null!;
    public long InscricaoMunicipal { get; set; }
    public string RazaoSocial { get; set; } = null!;
    public AddressDto? Endereco { get; set; }
    public string? Email { get; set; }
}

public class ServiceCustomerDto
{
    public string? CpfCnpj { get; set; }
    public long? InscricaoMunicipal { get; set; }
    public long? InscricaoEstadual { get; set; }
    public string? RazaoSocial { get; set; }
    public AddressDto? Endereco { get; set; }
    public string? Email { get; set; }
}

public class AddressDto
{
    public string? TipoLogradouro { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public int? CodigoMunicipio { get; set; }
    public string? UF { get; set; }
    public int? CEP { get; set; }
}

public class RpsDto
{
    public long InscricaoPrestador { get; set; }
    public string? SerieRps { get; set; }
    public long NumeroRps { get; set; }
    public string TipoRPS { get; set; } = "RPS";
    public DateOnly DataEmissao { get; set; }
    public string StatusRPS { get; set; } = "N";
    public string TributacaoRPS { get; set; } = null!;
    public RpsItemDto Item { get; set; } = null!;
    public ServiceCustomerDto? Tomador { get; set; }
}

public class RpsItemDto
{
    public int CodigoServico { get; set; }
    public string Discriminacao { get; set; } = null!;
    public decimal ValorServicos { get; set; }
    public decimal ValorDeducoes { get; set; }
    public decimal AliquotaServicos { get; set; }
    public bool IssRetido { get; set; }
}

