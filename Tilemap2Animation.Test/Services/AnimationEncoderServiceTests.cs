using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Services;

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
    
    [Fact]
    public async Task SaveAsGifAsync_WithExistingFile_OverwritesFile()
    {
        // Arrange
        var frames = new List<Image<Rgba32>>
        {
            new Image<Rgba32>(16, 16, new Rgba32(255, 0, 0)),
            new Image<Rgba32>(16, 16, new Rgba32(0, 255, 0))
        };
        var delays = new List<int> { 100, 100 };

        // Create dummy file
        File.WriteAllText(_testOutputPath, "Dummy content");
        var originalSize = new FileInfo(_testOutputPath).Length;

        try
        {
            // Act
            await _sut.SaveAsGifAsync(frames, delays, _testOutputPath);

            // Assert
            Assert.True(File.Exists(_testOutputPath));
            var newSize = new FileInfo(_testOutputPath).Length;
            Assert.NotEqual(originalSize, newSize); // File should be different
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
    
    [Fact]
    public async Task SaveAsGifAsync_WithEmptyFrames_ThrowsArgumentException()
    {
        // Arrange
        var frames = new List<Image<Rgba32>>();
        var delays = new List<int>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _sut.SaveAsGifAsync(frames, delays, _testOutputPath));
        
        Assert.Contains("No frames to encode", exception.Message);
    }
    
    [Fact]
    public async Task SaveAsGifAsync_WithNullFrames_ThrowsArgumentException()
    {
        // Arrange
        List<Image<Rgba32>>? frames = null;
        var delays = new List<int> { 100 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _sut.SaveAsGifAsync(frames!, delays, _testOutputPath));
        
        Assert.Contains("No frames to encode", exception.Message);
    }
    
    [Fact]
    public async Task SaveAsGifAsync_WithMismatchedDelaysCount_ThrowsArgumentException()
    {
        // Arrange
        var frames = new List<Image<Rgba32>>
        {
            new Image<Rgba32>(16, 16, new Rgba32(255, 0, 0)),
            new Image<Rgba32>(16, 16, new Rgba32(0, 255, 0))
        };
        var delays = new List<int> { 100 }; // Only one delay for two frames

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _sut.SaveAsGifAsync(frames, delays, _testOutputPath));
            
            Assert.Contains("Delays must match the number of frames", exception.Message);
        }
        finally
        {
            // Clean up
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }
    }
    
    [Fact]
    public async Task SaveAsGifAsync_WithNullDelays_ThrowsArgumentException()
    {
        // Arrange
        var frames = new List<Image<Rgba32>>
        {
            new Image<Rgba32>(16, 16, new Rgba32(255, 0, 0))
        };
        List<int>? delays = null;

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _sut.SaveAsGifAsync(frames, delays!, _testOutputPath));
            
            Assert.Contains("Delays must match the number of frames", exception.Message);
        }
        finally
        {
            // Clean up
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }
    }
    
    [Fact]
    public async Task SaveAsGifAsync_CreatesDirectoryIfNeeded()
    {
        // Arrange
        var frames = new List<Image<Rgba32>>
        {
            new Image<Rgba32>(16, 16, new Rgba32(255, 0, 0))
        };
        var delays = new List<int> { 100 };
        
        var nestedPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "test_animation.gif");
        var directory = Path.GetDirectoryName(nestedPath)!;
        
        // Make sure directory doesn't exist
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        try
        {
            // Act
            await _sut.SaveAsGifAsync(frames, delays, nestedPath);

            // Assert
            Assert.True(Directory.Exists(directory));
            Assert.True(File.Exists(nestedPath));
        }
        finally
        {
            // Clean up
            foreach (var frame in frames)
            {
                frame.Dispose();
            }

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
} 