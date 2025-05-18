using Tilemap2Animation.CommandLineOptions;

namespace Tilemap2Animation.Test.CommandLineOptions;

public class FpsOptionTests
{
    private readonly FpsOption _sut;

    public FpsOptionTests()
    {
        _sut = new FpsOption();
    }

    [Fact]
    public void Option_HasCorrectName()
    {
        // Assert
        Assert.Equal("fps", _sut.Option.Name);
    }

    [Fact]
    public void Option_HasCorrectAlias()
    {
        // Assert
        Assert.Contains("-f", _sut.Option.Aliases);
    }

    [Fact]
    public void Option_IsNotRequired()
    {
        // Assert
        Assert.False(_sut.Option.IsRequired);
    }
}