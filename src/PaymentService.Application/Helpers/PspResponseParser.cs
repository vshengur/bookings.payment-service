using System.Text.Json;

namespace PaymentService.Application.Helpers;

public static class PspResponseParser
{
    /// <summary>
    /// Extracts a provider reference/id from the PSP JSON response.
    /// Looks for "id" first, then "reference". Returns null on failure.
    /// </summary>
    public static string? ExtractProviderRef(string pspJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(pspJson);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
                return idProp.GetString();
            if (doc.RootElement.TryGetProperty("reference", out var refProp))
                return refProp.GetString();
        }
        catch (JsonException)
        {
        }
        return null;
    }
}
