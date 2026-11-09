using JodVyapar.Api.Common.Text;

namespace JodVyapar.Api.Tests;

public sealed class KeywordNormalizerTests
{
    [Fact]
    public void Normalize_LowercasesAndTrims() =>
        Assert.Equal(["cotton", "yarn"], KeywordNormalizer.Normalize(["  Cotton ", "YARN"]));

    [Fact]
    public void Normalize_SplitsOnCommas() =>
        Assert.Equal(["cotton", "yarn", "silk"], KeywordNormalizer.Normalize(["cotton, yarn", "silk,"]));

    [Fact]
    public void Normalize_RemovesDuplicates_KeepingFirstSeenOrder() =>
        Assert.Equal(["yarn", "cotton"], KeywordNormalizer.Normalize(["yarn", "Cotton", " YARN ", "cotton"]));

    [Fact]
    public void Normalize_CollapsesInnerSpaces() =>
        Assert.Equal(["combed cotton"], KeywordNormalizer.Normalize(["combed    cotton"]));

    [Fact]
    public void Normalize_DropsBlanksAndNulls() =>
        Assert.Empty(KeywordNormalizer.Normalize(["", "   ", ",,", null]));

    [Fact]
    public void Normalize_Null_ReturnsEmpty() =>
        Assert.Empty(KeywordNormalizer.Normalize(null));

    [Fact]
    public void StoredForm_RoundTrips()
    {
        var stored = KeywordNormalizer.ToStored(["cotton", "combed yarn"]);

        Assert.Equal("cotton,combed yarn", stored);
        Assert.Equal(["cotton", "combed yarn"], KeywordNormalizer.FromStored(stored));
    }

    [Fact]
    public void StoredForm_NoKeywords_IsNull()
    {
        Assert.Null(KeywordNormalizer.ToStored([]));
        Assert.Empty(KeywordNormalizer.FromStored(null));
    }
}
