using Tilemap2Animation.CommandLineOptions;

namespace Tilemap2Animation.Test.CommandLineOptions;

public class InputFileOptionTests
{
    private readonly InputFileOption _sut;

    public InputFileOptionTests()
    {
        _sut = new InputFileOption();
    }

    [Fact]
    public void Option_HasCorrectName()
    {
        // Assert
        Assert.Equal("input", _sut.Option.Name);
    }

    [Fact]
    public void Option_HasCorrectAlias()
    {
        // Assert
        Assert.Contains("-i", _sut.Option.Aliases);
    }

    [Fact]
    public void Option_IsRequired()
    {
        // Assert
        Assert.True(_sut.Option.IsRequired);
    }
}