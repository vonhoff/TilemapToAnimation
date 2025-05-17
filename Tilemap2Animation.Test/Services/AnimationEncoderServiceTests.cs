using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using Tilemap2Animation.Services;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Test.Services;

public class AnimationEncoderServiceTests
{
    private readonly AnimationEncoderService _sut;
    private readonly string _testOutputPath;

    public AnimationEncoderServiceTests()
    {
        _sut = new AnimationEncoderService();
        _testOutputPath = Path.Combine(Path.GetTempPath(), "test_animation.gif");
        
        // Ensure the test output file doesn't exist before each test
        if (File.Exists(_testOutputPath))
        {
            File.Delete(_testOutputPath);
        }
    }

    [Fact]
    public async Task SaveAsGifAsync_WithValidFrames_CreatesGifFile()
    {
        // Arrange
        var frames = new List<Image<Rgba32>>
        {
            new Image<Rgba32>(16, 16, new Rgba32(255, 0, 0)),
            new Image<Rgba32>(16, 16, new Rgba32(0, 255, 0)),
            new Image<Rgba32>(16, 16, new Rgba32(0, 0, 255))
        };
        var delays = new List<int> { 100, 100, 100 };

        try
        {
            // Act
            await _sut.SaveAsGifAsync(frames, delays, _testOutputPath);

            // Assert
            Assert.True(File.Exists(_testOutputPath));
            Assert.True(new FileInfo(_testOutputPath).Length > 0);
        }
        finally
        {
            // Clean up
            foreach (var frame in frames)
            {
                frame.Dispose();
            }

            if (File.Exists(_testOutputPath))
            {
                File.Delete(_testOutputPath);
            }
        }
    }
} 