namespace PaymentService.Application.Configuration;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
