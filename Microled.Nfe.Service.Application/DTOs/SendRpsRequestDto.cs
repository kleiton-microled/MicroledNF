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
    public RpsTributosDto? Tributos { get; set; }
    public RpsIbsCbsDto? IbsCbs { get; set; }

    // Compatibilidade com payload legado: alguns clientes enviam tributos no nível do RPS.
    public decimal? ValorPIS { get; set; }
    public decimal? ValorCOFINS { get; set; }
    public decimal? ValorINSS { get; set; }
    public decimal? ValorIR { get; set; }
    public decimal? ValorCSLL { get; set; }
    public decimal? ValorIPI { get; set; }
    public decimal? ValorCargaTributaria { get; set; }
    public decimal? PercentualCargaTributaria { get; set; }
    public string? FonteCargaTributaria { get; set; }
    public decimal? ValorTotalRecebido { get; set; }
    public decimal? ValorFinalCobrado { get; set; }
    public decimal? ValorMulta { get; set; }
    public decimal? ValorJuros { get; set; }
    public string? NCM { get; set; }
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

public class RpsTributosDto
{
    public decimal? ValorPIS { get; set; }
    public decimal? ValorCOFINS { get; set; }
    public decimal? ValorINSS { get; set; }
    public decimal? ValorIR { get; set; }
    public decimal? ValorCSLL { get; set; }
    public decimal? ValorIPI { get; set; }
    public decimal? ValorCargaTributaria { get; set; }
    public decimal? PercentualCargaTributaria { get; set; }
    public string? FonteCargaTributaria { get; set; }
    public decimal? ValorTotalRecebido { get; set; }
    public decimal? ValorFinalCobrado { get; set; }
    public decimal? ValorMulta { get; set; }
    public decimal? ValorJuros { get; set; }
    public string? NCM { get; set; }
}

public class RpsIbsCbsDto
{
    public int? FinNFSe { get; set; }
    public int? IndFinal { get; set; }
    public string? CIndOp { get; set; }
    public int? TpOper { get; set; }
    public List<string> RefNfSe { get; set; } = new();
    public int? TpEnteGov { get; set; }
    public int? IndDest { get; set; }
    public RpsIbsCbsPessoaDto? Dest { get; set; }
    public string? CClassTrib { get; set; }
    public string? CClassTribReg { get; set; }
    public string? NBS { get; set; }
    public int? CLocPrestacao { get; set; }
    public RpsIbsCbsImovelObraDto? ImovelObra { get; set; }
}

public class RpsIbsCbsPessoaDto
{
    public string? CpfCnpj { get; set; }
    public string? Nif { get; set; }
    public int? NaoNif { get; set; }
    public string RazaoSocial { get; set; } = null!;
    public AddressDto? Endereco { get; set; }
    public string? Email { get; set; }
}

public class RpsIbsCbsImovelObraDto
{
    public string? InscricaoImobiliariaFiscal { get; set; }
    public string? CCib { get; set; }
    public string? CObra { get; set; }
    public AddressDto? Endereco { get; set; }
}

