using FluentAssertions;

using PaymentService.Application.Helpers;

namespace Domain.Tests.Helpers;

[TestFixture]
public class PspResponseParserTests
{
    [Test]
    public void ExtractProviderRef_WithIdField_ReturnsId()
    {
        var json = """{"id": "pi_abc123", "status": "created"}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().Be("pi_abc123");
    }

    [Test]
    public void ExtractProviderRef_WithReferenceField_ReturnsReference()
    {
        var json = """{"reference": "ref-xyz", "status": "ok"}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().Be("ref-xyz");
    }

    [Test]
    public void ExtractProviderRef_WithBothFields_PrefersId()
    {
        var json = """{"id": "pi_111", "reference": "ref-222"}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().Be("pi_111");
    }

    [Test]
    public void ExtractProviderRef_WithoutIdOrReference_ReturnsNull()
    {
        var json = """{"status": "created", "amount": 25000}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().BeNull();
    }

    [Test]
    public void ExtractProviderRef_WithInvalidJson_ReturnsNull()
    {
        var result = PspResponseParser.ExtractProviderRef("not-json");

        result.Should().BeNull();
    }

    [Test]
    public void ExtractProviderRef_WithEmptyObject_ReturnsNull()
    {
        var result = PspResponseParser.ExtractProviderRef("{}");

        result.Should().BeNull();
    }
}
