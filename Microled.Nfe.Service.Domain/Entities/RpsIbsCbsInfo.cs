namespace Microled.Nfe.Service.Domain.Entities;

public sealed class RpsIbsCbsInfo
{
    public int? FinNfSe { get; }
    public int? IndFinal { get; }
    public string? CIndOp { get; }
    public int? TpOper { get; }
    public IReadOnlyList<string> RefNfSe { get; }
    public int? TpEnteGov { get; }
    public int? IndDest { get; }
    public RpsIbsCbsPersonInfo? Dest { get; }
    public string? CClassTrib { get; }
    public string? CClassTribReg { get; }
    public string? Nbs { get; }
    public int? CLocPrestacao { get; }
    public RpsIbsCbsImovelObraInfo? ImovelObra { get; }

    public RpsIbsCbsInfo(
        int? finNfSe = null,
        int? indFinal = null,
        string? cIndOp = null,
        int? tpOper = null,
        IReadOnlyList<string>? refNfSe = null,
        int? tpEnteGov = null,
        int? indDest = null,
        RpsIbsCbsPersonInfo? dest = null,
        string? cClassTrib = null,
        string? cClassTribReg = null,
        string? nbs = null,
        int? cLocPrestacao = null,
        RpsIbsCbsImovelObraInfo? imovelObra = null)
    {
        FinNfSe = finNfSe;
        IndFinal = indFinal;
        CIndOp = cIndOp;
        TpOper = tpOper;
        RefNfSe = refNfSe ?? Array.Empty<string>();
        TpEnteGov = tpEnteGov;
        IndDest = indDest;
        Dest = dest;
        CClassTrib = cClassTrib;
        CClassTribReg = cClassTribReg;
        Nbs = nbs;
        CLocPrestacao = cLocPrestacao;
        ImovelObra = imovelObra;
    }
}

public sealed class RpsIbsCbsPersonInfo
{
    public string? Cpf { get; }
    public string? Cnpj { get; }
    public string? Nif { get; }
    public int? NaoNif { get; }
    public string RazaoSocial { get; }
    public Address? Endereco { get; }
    public string? Email { get; }

    public RpsIbsCbsPersonInfo(
        string? cpf,
        string? cnpj,
        string? nif,
        int? naoNif,
        string razaoSocial,
        Address? endereco,
        string? email)
    {
        Cpf = cpf;
        Cnpj = cnpj;
        Nif = nif;
        NaoNif = naoNif;
        RazaoSocial = razaoSocial;
        Endereco = endereco;
        Email = email;
    }
}

public sealed class RpsIbsCbsImovelObraInfo
{
    public string? InscricaoImobiliariaFiscal { get; }
    public string? CCib { get; }
    public string? CObra { get; }
    public Address? Endereco { get; }

    public RpsIbsCbsImovelObraInfo(
        string? inscricaoImobiliariaFiscal,
        string? cCib,
        string? cObra,
        Address? endereco)
    {
        InscricaoImobiliariaFiscal = inscricaoImobiliariaFiscal;
        CCib = cCib;
        CObra = cObra;
        Endereco = endereco;
    }
}
