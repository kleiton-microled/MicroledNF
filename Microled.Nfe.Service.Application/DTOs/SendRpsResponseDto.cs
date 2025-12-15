namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Response DTO for sending RPS batch
/// </summary>
public class SendRpsResponseDto
{
    public bool Sucesso { get; set; }
    public string? Protocolo { get; set; }
    public List<NfeRpsKeyDto> ChavesNFeRPS { get; set; } = new();
    public List<EventoDto> Alertas { get; set; } = new();
    public List<EventoDto> Erros { get; set; } = new();
}

public class NfeRpsKeyDto
{
    public NfeKeyDto ChaveNFe { get; set; } = null!;
    public RpsKeyDto ChaveRPS { get; set; } = null!;
}

public class NfeKeyDto
{
    public long InscricaoPrestador { get; set; }
    public long NumeroNFe { get; set; }
    public string? CodigoVerificacao { get; set; }
    public string? ChaveNotaNacional { get; set; }
}

public class RpsKeyDto
{
    public long InscricaoPrestador { get; set; }
    public string? SerieRps { get; set; }
    public long NumeroRps { get; set; }
}

public class EventoDto
{
    public int Codigo { get; set; }
    public string? Descricao { get; set; }
}

