namespace Microled.Nfe.Service.Domain.Entities;

public sealed class Tomador
{
    public Guid Id { get; private set; }
    public string CpfCnpj { get; private set; } = string.Empty;
    public string RazaoSocial { get; private set; } = string.Empty;
    public string? InscricaoMunicipal { get; private set; }
    public string? InscricaoEstadual { get; private set; }
    public string? Email { get; private set; }
    public string? TipoLogradouro { get; private set; }
    public string? Logradouro { get; private set; }
    public string? Numero { get; private set; }
    public string? Complemento { get; private set; }
    public string? Bairro { get; private set; }
    public string? Uf { get; private set; }
    public string? CodigoMunicipio { get; private set; }
    public string? Cep { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset? AlteradoEm { get; private set; }

    private Tomador()
    {
    }

    public static Tomador Create(
        string cpfCnpj,
        string razaoSocial,
        string? inscricaoMunicipal = null,
        string? inscricaoEstadual = null,
        string? email = null,
        string? tipoLogradouro = null,
        string? logradouro = null,
        string? numero = null,
        string? complemento = null,
        string? bairro = null,
        string? uf = null,
        string? codigoMunicipio = null,
        string? cep = null)
    {
        var digits = NormalizeCpfCnpj(cpfCnpj);
        if (digits.Length is not (11 or 14))
        {
            throw new ArgumentException("Informe um CPF (11 dígitos) ou CNPJ (14 dígitos).", nameof(cpfCnpj));
        }

        if (string.IsNullOrWhiteSpace(razaoSocial))
        {
            throw new ArgumentException("Razão social é obrigatória.", nameof(razaoSocial));
        }

        return new Tomador
        {
            Id = Guid.NewGuid(),
            CpfCnpj = digits,
            RazaoSocial = razaoSocial.Trim(),
            InscricaoMunicipal = TrimToNull(inscricaoMunicipal),
            InscricaoEstadual = TrimToNull(inscricaoEstadual),
            Email = TrimToNull(email),
            TipoLogradouro = TrimToNull(tipoLogradouro),
            Logradouro = TrimToNull(logradouro),
            Numero = TrimToNull(numero),
            Complemento = TrimToNull(complemento),
            Bairro = TrimToNull(bairro),
            Uf = TrimToNull(uf)?.ToUpperInvariant(),
            CodigoMunicipio = TrimToNull(codigoMunicipio),
            Cep = NormalizeDigits(cep),
            CriadoEm = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string razaoSocial,
        string? inscricaoMunicipal,
        string? inscricaoEstadual,
        string? email,
        string? tipoLogradouro,
        string? logradouro,
        string? numero,
        string? complemento,
        string? bairro,
        string? uf,
        string? codigoMunicipio,
        string? cep)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
        {
            throw new ArgumentException("Razão social é obrigatória.", nameof(razaoSocial));
        }

        RazaoSocial = razaoSocial.Trim();
        InscricaoMunicipal = TrimToNull(inscricaoMunicipal);
        InscricaoEstadual = TrimToNull(inscricaoEstadual);
        Email = TrimToNull(email);
        TipoLogradouro = TrimToNull(tipoLogradouro);
        Logradouro = TrimToNull(logradouro);
        Numero = TrimToNull(numero);
        Complemento = TrimToNull(complemento);
        Bairro = TrimToNull(bairro);
        Uf = TrimToNull(uf)?.ToUpperInvariant();
        CodigoMunicipio = TrimToNull(codigoMunicipio);
        Cep = NormalizeDigits(cep);
        AlteradoEm = DateTimeOffset.UtcNow;
    }

    public static string NormalizeCpfCnpj(string? value)
    {
        return NormalizeDigits(value) ?? string.Empty;
    }

    private static string? NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static string? TrimToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
