namespace PaymentService.Application.Configuration;

public sealed class PaymentSettings
{
    public const string SectionName = "Payment";

    public string ReturnUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
    public string[] AllowedCurrencies { get; init; } = ["EUR", "USD", "GBP"];
}
