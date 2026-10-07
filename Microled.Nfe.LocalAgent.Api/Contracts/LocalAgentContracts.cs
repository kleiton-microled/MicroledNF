using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.LocalAgent.Api.Contracts;

public class LocalRpsProcessResponse
{
    public bool Success { get; set; }

    public bool IsSentToWebService { get; set; }

    public string? LocalFilePath { get; set; }

    public string? SoapFilePath { get; set; }

    public string? Protocol { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<EventoDto> Warnings { get; set; } = [];

    public List<EventoDto> Errors { get; set; } = [];

    public List<NfeRpsKeyDto> NfeRpsKeys { get; set; } = [];
}

public class WebServiceProbeRequest
{
    public List<string>? CandidateUrls { get; set; }
}

public class LocalAccessPendingRpsResponse
{
    public int Count { get; set; }

    public List<int> RecordIds { get; set; } = [];

    public SendRpsRequestDto? Request { get; set; }
}

public class LocalAccessSettingsResponse
{
    public string DatabasePath { get; set; } = string.Empty;

    public bool FileExists { get; set; }
}

public class ConfigureAccessRequest
{
    public string DatabasePath { get; set; } = string.Empty;
}

public class MarkAccessGeneratedRequest
{
    public List<int> RecordIds { get; set; } = [];
}

public class LocalAccessPendingCountResponse
{
    public int Count { get; set; }
}

public class QueuePendingRpsRequest
{
    public SendRpsRequestDto Request { get; set; } = null!;

    public List<int> RecordIds { get; set; } = [];

    /// <summary>Base de calculo federal do calculo de impostos (gravada na coluna BC do Access).</summary>
    public decimal? BaseCalculoFederal { get; set; }

    /// <summary>Valor liquido da nota calculado pelo servico (gravado na coluna Valor_liquido do Access).</summary>
    public decimal? ValorLiquido { get; set; }
}

public class UpdateNfPagamentoRequest
{
    public bool Pago { get; set; }

    public DateTime? DataPagamento { get; set; }

    public decimal? ValorDepositado { get; set; }
}

public class UpdateNfPagamentoResponse
{
    public bool Success { get; set; }

    /// <summary>False quando a NF nao existe na tabela do Access (nota fora do RPS ou anterior a rotina).</summary>
    public bool Found { get; set; }

    public string Message { get; set; } = string.Empty;
}

public class QueuePendingRpsResponse
{
    public bool Success { get; set; }

    public Guid? NotaId { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<string> Errors { get; set; } = [];
}
