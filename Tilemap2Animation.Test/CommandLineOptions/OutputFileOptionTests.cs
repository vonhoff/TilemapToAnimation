using Tilemap2Animation.CommandLineOptions;

namespace Tilemap2Animation.Test.CommandLineOptions;

public class OutputFileOptionTests
{
    private readonly OutputFileOption _sut;

    public OutputFileOptionTests()
    {
        _sut = new OutputFileOption();
    }

    [Fact]
    public void Option_HasCorrectName()
    {
        // Assert
        Assert.Equal("output", _sut.Option.Name);
    }

    [Fact]
    public void Option_HasCorrectAlias()
    {
        // Assert
        Assert.Contains("-o", _sut.Option.Aliases);
    }

    [Fact]
    public void Option_IsNotRequired()
    {
        // Assert
        Assert.False(_sut.Option.IsRequired);
    }
}