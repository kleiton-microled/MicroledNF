using Microled.Nfe.Service.Domain.ValueObjects;

namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// Service provider entity
/// </summary>
public sealed class ServiceProvider
{
    public CpfCnpj CpfCnpj { get; }
    public long InscricaoMunicipal { get; }
    public string RazaoSocial { get; }
    public Address? Endereco { get; }
    public string? Email { get; }

    public ServiceProvider(
        CpfCnpj cpfCnpj,
        long inscricaoMunicipal,
        string razaoSocial,
        Address? endereco = null,
        string? email = null)
    {
        CpfCnpj = cpfCnpj ?? throw new ArgumentNullException(nameof(cpfCnpj));
        InscricaoMunicipal = inscricaoMunicipal;
        RazaoSocial = razaoSocial ?? throw new ArgumentNullException(nameof(razaoSocial));
        Endereco = endereco;
        Email = email;
    }
}

/// <summary>
/// Address value object
/// </summary>
public sealed class Address
{
    public string? TipoLogradouro { get; }
    public string? Logradouro { get; }
    public string? Numero { get; }
    public string? Complemento { get; }
    public string? Bairro { get; }
    public int? CodigoMunicipio { get; }
    public string? UF { get; }
    public int? CEP { get; }

    public Address(
        string? tipoLogradouro = null,
        string? logradouro = null,
        string? numero = null,
        string? complemento = null,
        string? bairro = null,
        int? codigoMunicipio = null,
        string? uf = null,
        int? cep = null)
    {
        TipoLogradouro = tipoLogradouro;
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Bairro = bairro;
        CodigoMunicipio = codigoMunicipio;
        UF = uf;
        CEP = cep;
    }
}

