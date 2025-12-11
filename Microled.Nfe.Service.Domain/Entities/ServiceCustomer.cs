using Microled.Nfe.Service.Domain.ValueObjects;

namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// Service customer (tomador) entity
/// </summary>
public sealed class ServiceCustomer
{
    public CpfCnpj? CpfCnpj { get; }
    public long? InscricaoMunicipal { get; }
    public long? InscricaoEstadual { get; }
    public string? RazaoSocial { get; }
    public Address? Endereco { get; }
    public string? Email { get; }

    public ServiceCustomer(
        CpfCnpj? cpfCnpj = null,
        long? inscricaoMunicipal = null,
        long? inscricaoEstadual = null,
        string? razaoSocial = null,
        Address? endereco = null,
        string? email = null)
    {
        CpfCnpj = cpfCnpj;
        InscricaoMunicipal = inscricaoMunicipal;
        InscricaoEstadual = inscricaoEstadual;
        RazaoSocial = razaoSocial;
        Endereco = endereco;
        Email = email;
    }
}

