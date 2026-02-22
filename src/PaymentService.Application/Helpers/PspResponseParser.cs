using System.Text.Json;

namespace PaymentService.Application.Helpers;

public static class PspResponseParser
{
    /// <summary>
    /// Extracts a provider reference/id from the PSP JSON response.
    /// Looks for "id" first, then "reference". Returns null on failure.
    /// Handles non-string values (numbers, booleans) via ToString() fallback.
    /// </summary>
    public static string? ExtractProviderRef(string pspJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(pspJson);

            if (doc.RootElement.TryGetProperty("id", out var idProp))
            {
                var value = GetStringValue(idProp);
                if (value is not null) return value;
            }

            if (doc.RootElement.TryGetProperty("reference", out var refProp))
            {
                var value = GetStringValue(refProp);
                if (value is not null) return value;
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }

    private static string? GetStringValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
            _ => null, // Object, Array, Null, Undefined — skip
        };
    }
}
