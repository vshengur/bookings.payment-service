namespace PaymentService.Domain.Pricing;

/// <summary>
/// Стратегия сама решает, применима ли она к запрашиваемым условиям,
/// и отдаёт приоритет для случаев, когда подходит несколько стратегий.
/// </summary>
public interface IPricingStrategy
{
    /// <summary>Проверка применимости.</summary>
    bool IsApplicable(QuoteContext ctx);

    /// <summary>Приоритет: чем больше число – тем желательнее стратегия.</summary>
    int Priority { get; }

    /// <summary>Расчёт цены (вызывается, только если <see cref="IsApplicable"/> вернул true).</summary>
    decimal CalculatePrice(QuoteContext ctx);
}