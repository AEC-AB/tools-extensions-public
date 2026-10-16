using Xunit;

namespace IdsValidatorTests;

public sealed class CorpusPathTests
{
    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("\\")]
    public void Extraction_accepts_directory_with_or_without_trailing_separator(string trailing)
    {
        var root = Path.Join(Path.GetTempPath(), "ids-corpus-tests", "cases");
        var actual = OfficialCases.ExtractionPath(root + trailing, "attribute/test.ids");
        Assert.Equal(Path.GetFullPath(Path.Join(root, "attribute", "test.ids")), actual);
    }

    [Theory]
    [InlineData("../outside.ids")]
    [InlineData("..\\outside.ids")]
    [InlineData("/outside.ids")]
    [InlineData("C:\\outside.ids")]
    [InlineData("C:outside.ids")]
    public void Extraction_rejects_rooted_paths_and_traversal(string entry)
    {
        var root = Path.Join(Path.GetTempPath(), "ids-corpus-tests", "cases");
        Assert.Throws<InvalidDataException>(() => OfficialCases.ExtractionPath(root, entry));
    }
}
