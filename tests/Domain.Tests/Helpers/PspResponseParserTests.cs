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

    [Test]
    public void ExtractProviderRef_WithNumericId_ReturnsStringRepresentation()
    {
        var json = """{"id": 123456}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().Be("123456");
    }

    [Test]
    public void ExtractProviderRef_WithObjectReference_ReturnsNull()
    {
        var json = """{"reference": {"nested": "value"}}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().BeNull();
    }

    [Test]
    public void ExtractProviderRef_WithBooleanId_ReturnsStringRepresentation()
    {
        var json = """{"id": true}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().Be("true");
    }

    [Test]
    public void ExtractProviderRef_WithNullId_FallsToReference()
    {
        var json = """{"id": null, "reference": "ref-fallback"}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().Be("ref-fallback");
    }

    [Test]
    public void ExtractProviderRef_WithArrayId_ReturnsNull()
    {
        var json = """{"id": [1,2,3]}""";

        var result = PspResponseParser.ExtractProviderRef(json);

        result.Should().BeNull();
    }
}
