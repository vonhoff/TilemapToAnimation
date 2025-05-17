using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Test.Services;

public class TilesetImageServiceTests
{
    private readonly TilesetImageService _sut;

    public TilesetImageServiceTests()
    {
        _sut = new TilesetImageService();
    }

    [Fact]
    public void GetTileBitmap_WithValidSourceRect_ReturnsCroppedImage()
    {
        // Arrange
        using var tilesetImage = new Image<Rgba32>(32, 32);
        var sourceRect = new Rectangle(0, 0, 16, 16);

        // Act
        using var result = _sut.GetTileBitmap(tilesetImage, sourceRect);

        // Assert
        Assert.Equal(16, result.Width);
        Assert.Equal(16, result.Height);
    }

    [Fact]
    public void ApplyTileTransformations_FlippedHorizontally_FlipsImage()
    {
        // Arrange
        using var tileImage = new Image<Rgba32>(16, 16);
        // Create an asymmetric pattern to test flipping
        tileImage[0, 0] = new Rgba32(255, 0, 0);
        
        // Act
        using var result = _sut.ApplyTileTransformations(tileImage, true, false, false);
        
        // Assert
        Assert.Equal(tileImage[0, 0], result[15, 0]); // Pixel should be mirrored horizontally
    }

    [Fact]
    public void ApplyTileTransformations_FlippedVertically_FlipsImage()
    {
        // Arrange
        using var tileImage = new Image<Rgba32>(16, 16);
        // Create an asymmetric pattern to test flipping
        tileImage[0, 0] = new Rgba32(255, 0, 0);
        
        // Act
        using var result = _sut.ApplyTileTransformations(tileImage, false, true, false);
        
        // Assert
        Assert.Equal(tileImage[0, 0], result[0, 15]); // Pixel should be mirrored vertically
    }
} 