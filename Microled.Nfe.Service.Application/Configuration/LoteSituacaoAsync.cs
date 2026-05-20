namespace Microled.Nfe.Service.Application.Configuration;

/// <summary>
/// Situation codes for async batch (ConsultaSituacaoLote) per São Paulo NFS-e schema.
/// </summary>
public static class LoteSituacaoAsync
{
    public const int Enviado = 0;
    public const int Invalidado = 1;
    public const int Verificado = 2;
    public const int Processado = 3;

    public static bool IsPending(int? situacaoCodigo) =>
        situacaoCodigo is Enviado or Verificado;

    public static bool IsProcessed(int? situacaoCodigo) =>
        situacaoCodigo == Processado;

    public static bool IsInvalid(int? situacaoCodigo) =>
        situacaoCodigo == Invalidado;
}
