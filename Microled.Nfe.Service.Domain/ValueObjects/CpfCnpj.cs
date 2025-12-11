namespace Microled.Nfe.Service.Domain.ValueObjects;

/// <summary>
/// Value object representing either a CPF or CNPJ
/// </summary>
public sealed class CpfCnpj
{
    public Cpf? Cpf { get; }
    public Cnpj? Cnpj { get; }
    public bool IsCpf => Cpf != null;
    public bool IsCnpj => Cnpj != null;

    private CpfCnpj(Cpf? cpf, Cnpj? cnpj)
    {
        Cpf = cpf;
        Cnpj = cnpj;
    }

    public static CpfCnpj CreateFromCpf(string cpf)
    {
        return new CpfCnpj(Cpf.Create(cpf), null);
    }

    public static CpfCnpj CreateFromCnpj(string cnpj)
    {
        return new CpfCnpj(null, Cnpj.Create(cnpj));
    }

    public string GetValue() => Cpf?.Value ?? Cnpj?.Value ?? string.Empty;

    public string GetFormatted() => Cpf?.GetFormatted() ?? Cnpj?.GetFormatted() ?? string.Empty;

    public override string ToString() => GetValue();
}

