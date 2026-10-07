using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.DTOs.Tomadores;

public sealed class TomadorRequest
{
    public string CpfCnpj { get; init; } = string.Empty;
    public string RazaoSocial { get; init; } = string.Empty;
    public string? InscricaoMunicipal { get; init; }
    public string? InscricaoEstadual { get; init; }
    public string? Email { get; init; }
    public string? TipoLogradouro { get; init; }
    public string? Logradouro { get; init; }
    public string? Numero { get; init; }
    public string? Complemento { get; init; }
    public string? Bairro { get; init; }
    public string? Uf { get; init; }
    public string? CodigoMunicipio { get; init; }
    public string? Cep { get; init; }
}

public sealed class TomadorResponse
{
    public Guid Id { get; init; }
    public string CpfCnpj { get; init; } = string.Empty;
    public string RazaoSocial { get; init; } = string.Empty;
    public string? InscricaoMunicipal { get; init; }
    public string? InscricaoEstadual { get; init; }
    public string? Email { get; init; }
    public string? TipoLogradouro { get; init; }
    public string? Logradouro { get; init; }
    public string? Numero { get; init; }
    public string? Complemento { get; init; }
    public string? Bairro { get; init; }
    public string? Uf { get; init; }
    public string? CodigoMunicipio { get; init; }
    public string? Cep { get; init; }
    public DateTimeOffset CriadoEm { get; init; }
    public DateTimeOffset? AlteradoEm { get; init; }
}

public sealed class PagedTomadorResponse
{
    public IReadOnlyList<TomadorResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public static class TomadorMapper
{
    public static TomadorResponse ToResponse(Tomador tomador) => new()
    {
        Id = tomador.Id,
        CpfCnpj = tomador.CpfCnpj,
        RazaoSocial = tomador.RazaoSocial,
        InscricaoMunicipal = tomador.InscricaoMunicipal,
        InscricaoEstadual = tomador.InscricaoEstadual,
        Email = tomador.Email,
        TipoLogradouro = tomador.TipoLogradouro,
        Logradouro = tomador.Logradouro,
        Numero = tomador.Numero,
        Complemento = tomador.Complemento,
        Bairro = tomador.Bairro,
        Uf = tomador.Uf,
        CodigoMunicipio = tomador.CodigoMunicipio,
        Cep = tomador.Cep,
        CriadoEm = tomador.CriadoEm,
        AlteradoEm = tomador.AlteradoEm
    };
}
