namespace PaymentService.Application.Configuration;

public sealed class PspSettings
{
    public const string SectionName = "Psp";

    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
}
