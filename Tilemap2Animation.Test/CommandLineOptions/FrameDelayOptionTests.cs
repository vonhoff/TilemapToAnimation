using System.CommandLine;
using System.Reflection;
using Tilemap2Animation.CommandLineOptions;

namespace Tilemap2Animation.Test.CommandLineOptions;

public class FrameDelayOptionTests
{
    private readonly FrameDelayOption _sut;

    public FrameDelayOptionTests()
    {
        _sut = new FrameDelayOption();
    }

    [Fact]
    public void Option_HasCorrectName()
    {
        // Assert
        Assert.Equal("frame-delay", _sut.Option.Name);
    }

    [Fact]
    public void Option_HasCorrectAlias()
    {
        // Assert
        Assert.Contains("-d", _sut.Option.Aliases);
    }

    [Fact]
    public void Option_IsNotRequired()
    {
        // Assert
        Assert.False(_sut.Option.IsRequired);
    }
} 