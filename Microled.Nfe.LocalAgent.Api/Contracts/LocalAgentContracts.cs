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
