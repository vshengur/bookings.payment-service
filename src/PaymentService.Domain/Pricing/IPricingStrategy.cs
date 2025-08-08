namespace PaymentService.Domain.Pricing;

/// <summary>
/// ��������� ���� ������, ��������� �� ��� � ������������� ��������,
/// � ����� ��������� ��� �������, ����� �������� ��������� ���������.
/// </summary>
public interface IPricingStrategy
{
    /// <summary>�������� ������������.</summary>
    bool IsApplicable(QuoteContext ctx);

    /// <summary>���������: ��� ������ ����� � ��� ����������� ���������.</summary>
    int Priority { get; }

    /// <summary>������ ���� (����������, ������ ���� <see cref="IsApplicable"/> ������ true).</summary>
    decimal CalculatePrice(QuoteContext ctx);
}