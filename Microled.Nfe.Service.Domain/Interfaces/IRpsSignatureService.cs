using System.Security.Cryptography.X509Certificates;
using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Domain.Interfaces;

/// <summary>
/// Strategy for padding SerieRPS in signature string
/// </summary>
public enum SeriePaddingStrategy
{
    /// <summary>
    /// Fixed 5 characters: "A    " (série + spaces to complete 5 chars)
    /// </summary>
    Fixed5Chars,
    
    /// <summary>
    /// Variable with 1 space: "A " (série + 1 space)
    /// </summary>
    VariablePlus1Space,
    
    /// <summary>
    /// Variable without space: "A" (série only, no space)
    /// </summary>
    VariableNoSpace
}

/// <summary>
/// Service for generating RPS digital signature
/// </summary>
public interface IRpsSignatureService
{
    /// <summary>
    /// Generates the signature string for RPS according to the official specification
    /// The string should contain 86 positions with specific fields and formatting
    /// Reference: NFe_Web_Service-4.pdf - Campos para assinatura do RPS (versão 2.0)
    /// </summary>
    string BuildSignatureString(Rps rps);

    /// <summary>
    /// Signs the RPS using the signature string and certificate
    /// Algorithm: SHA1 with RSA
    /// Returns Base64 encoded signature
    /// </summary>
    string SignRps(Rps rps, X509Certificate2 certificate);

    /// <summary>
    /// Compares our signature string with the one returned by the prefeitura in error 1206
    /// Extracts the string from error message and provides detailed diff
    /// </summary>
    /// <param name="ourString">The signature string we generated</param>
    /// <param name="prefeituraString">The string returned by prefeitura in error message</param>
    /// <param name="rps">The RPS being signed (for logging context)</param>
    /// <returns>True if strings match, false otherwise</returns>
    bool CompareSignatureStrings(string ourString, string prefeituraString, Rps rps);

    /// <summary>
    /// Extracts the signature string from error 1206 message
    /// Looks for pattern "String verificada (...)" in the error description
    /// </summary>
    /// <param name="errorMessage">The error message from prefeitura</param>
    /// <returns>The extracted string, or null if not found</returns>
    string? ExtractSignatureStringFromError(string errorMessage);

    /// <summary>
    /// Detects the SeriePaddingStrategy used in the verified string
    /// </summary>
    /// <param name="verifiedString">The signature string returned by prefeitura</param>
    /// <param name="serieRps">The actual serie RPS value (e.g., "A")</param>
    /// <returns>The detected strategy</returns>
    SeriePaddingStrategy DetectSerieStrategy(string verifiedString, string serieRps);

    /// <summary>
    /// Tries to automatically fix the signature string by testing different strategies
    /// Returns the fixed string if a match is found, null otherwise
    /// </summary>
    /// <param name="rps">The RPS being signed</param>
    /// <param name="verifiedString">The signature string returned by prefeitura</param>
    /// <returns>The fixed signature string, or null if no match found</returns>
    string? AutoFixSignatureString(Rps rps, string verifiedString);
}

