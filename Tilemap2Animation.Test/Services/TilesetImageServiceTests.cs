using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Test.Services;

public class TilesetImageServiceTests : IDisposable
{
    private readonly TilesetImageService _sut;
    private readonly string _tempDirectory;

    public TilesetImageServiceTests()
    {
        _sut = new TilesetImageService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "TilesetImageServiceTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }
    
    [Fact]
    public async Task LoadTilesetImageAsync_WithValidFile_ReturnsImage()
    {
        // Arrange
        var testImagePath = Path.Combine(_tempDirectory, "test_image.png");
        using (var image = new Image<Rgba32>(32, 32))
        {
            await image.SaveAsPngAsync(testImagePath);
        }

        try
        {
            // Act
            using var result = await _sut.LoadTilesetImageAsync(testImagePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(32, result.Width);
            Assert.Equal(32, result.Height);
        }
        finally
        {
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
            }
        }
    }
    
    [Fact]
    public async Task LoadTilesetImageAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "non_existent.png");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _sut.LoadTilesetImageAsync(nonExistentPath));
    }
    
    [Fact]
    public async Task LoadTilesetImageAsync_WithInvalidImage_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidImagePath = Path.Combine(_tempDirectory, "invalid_image.png");
        File.WriteAllText(invalidImagePath, "This is not a valid image file");

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.LoadTilesetImageAsync(invalidImagePath));
        }
        finally
        {
            if (File.Exists(invalidImagePath))
            {
                File.Delete(invalidImagePath);
            }
        }
    }
    
    [Fact]
    public void ProcessTransparency_WithTransparencyColor_MakesPixelsTransparent()
    {
        // Arrange
        using var tilesetImage = new Image<Rgba32>(32, 32);
        
        // Fill image with a specific color that will be made transparent
        var transColor = new Rgba32(255, 0, 255); // Magenta
        tilesetImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = transColor;
                }
            }
        });
        
        var tileset = new Tileset
        {
            Image = new TilesetImage
            {
                Trans = "FF00FF" // Magenta in hex
            }
        };

        // Act
        using var result = _sut.ProcessTransparency(tilesetImage, tileset);

        // Assert
        result.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    // Check that all pixels are now transparent
                    Assert.Equal(0, row[x].A);
                }
            }
        });
    }
    
    [Fact]
    public void ProcessTransparency_WithNoTransparencyColor_ReturnsOriginalImage()
    {
        // Arrange
        using var tilesetImage = new Image<Rgba32>(32, 32);
        var tileset = new Tileset
        {
            Image = new TilesetImage
            {
                Trans = null // No transparency color
            }
        };

        // Act
        using var result = _sut.ProcessTransparency(tilesetImage, tileset);

        // Assert
        Assert.Same(tilesetImage, result); // Should return the original image
    }
    
    [Fact]
    public void ProcessTransparency_WithInvalidTransparencyColor_ReturnsOriginalImage()
    {
        // Arrange
        using var tilesetImage = new Image<Rgba32>(32, 32);
        var tileset = new Tileset
        {
            Image = new TilesetImage
            {
                Trans = "NotAValidHexColor"
            }
        };

        // Act
        using var result = _sut.ProcessTransparency(tilesetImage, tileset);

        // Assert
        Assert.Same(tilesetImage, result); // Should return the original image
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
    public void GetTileBitmap_WithSourceRectOutsideBounds_ReturnsAdjustedImage()
    {
        // Arrange
        using var tilesetImage = new Image<Rgba32>(32, 32);
        var sourceRect = new Rectangle(20, 20, 20, 20); // Goes beyond the image bounds

        // Act
        using var result = _sut.GetTileBitmap(tilesetImage, sourceRect);

        // Assert
        Assert.Equal(12, result.Width);  // Should be adjusted to fit (32 - 20 = 12)
        Assert.Equal(12, result.Height); // Should be adjusted to fit (32 - 20 = 12)
    }
    
    [Fact]
    public void GetTileBitmap_WithSourceRectCompletelyOutsideBounds_ReturnsEmptyImage()
    {
        // Arrange
        using var tilesetImage = new Image<Rgba32>(32, 32);
        var sourceRect = new Rectangle(50, 50, 10, 10); // Completely outside the image

        // Act
        using var result = _sut.GetTileBitmap(tilesetImage, sourceRect);

        // Assert
        Assert.Equal(10, result.Width);  // Should return an empty image with requested dimensions
        Assert.Equal(10, result.Height);
        
        // Check that the image is transparent (all pixels have alpha = 0)
        result.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    Assert.Equal(0, row[x].A);
                }
            }
        });
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
    
    [Fact]
    public void ApplyTileTransformations_FlippedDiagonally_TransformsImage()
    {
        // Arrange
        using var tileImage = new Image<Rgba32>(16, 16);
        
        // Create a distinctive pattern that will change with diagonal flip
        // Red in top-left, green in top-right, blue in bottom-left
        tileImage[0, 0] = new Rgba32(255, 0, 0);      // Red
        tileImage[15, 0] = new Rgba32(0, 255, 0);     // Green
        tileImage[0, 15] = new Rgba32(0, 0, 255);     // Blue
        
        // Act
        using var result = _sut.ApplyTileTransformations(tileImage, false, false, true);
        
        // Assert
        Assert.Equal(16, result.Width);
        Assert.Equal(16, result.Height);
        
        // Verify that the pattern has changed due to transformation
        // At least one of the color positions should be different
        bool patternChanged = false;
        
        if (!ColorEquals(tileImage[0, 0], result[0, 0]) ||
            !ColorEquals(tileImage[15, 0], result[15, 0]) ||
            !ColorEquals(tileImage[0, 15], result[0, 15]))
        {
            patternChanged = true;
        }
        
        Assert.True(patternChanged, "The pattern should change after diagonal flip");
        
        // Verify all colors are still present in the image
        Assert.True(ContainsColor(result, 255, 0, 0), "Red should be present after transformation");
        Assert.True(ContainsColor(result, 0, 255, 0), "Green should be present after transformation");
        Assert.True(ContainsColor(result, 0, 0, 255), "Blue should be present after transformation");
    }
    
    // Helper methods for color comparisons
    private bool ColorEquals(Rgba32 a, Rgba32 b)
    {
        return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
    }
    
    private bool ContainsColor(Image<Rgba32> image, byte r, byte g, byte b)
    {
        bool found = false;
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height && !found; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length && !found; x++)
                {
                    if (row[x].R == r && row[x].G == g && row[x].B == b)
                    {
                        found = true;
                    }
                }
            }
        });
        return found;
    }
    
    [Fact]
    public void ApplyTileTransformations_AllFlips_TransformsImage()
    {
        // Arrange
        using var tileImage = new Image<Rgba32>(16, 16);
        
        // Create a distinctive pattern that will change with transformations
        // Red in top-left, green in top-right, blue in bottom-left
        tileImage[0, 0] = new Rgba32(255, 0, 0);      // Red
        tileImage[15, 0] = new Rgba32(0, 255, 0);     // Green
        tileImage[0, 15] = new Rgba32(0, 0, 255);     // Blue
        
        // Act
        using var result = _sut.ApplyTileTransformations(tileImage, true, true, true);
        
        // Assert
        Assert.Equal(16, result.Width);
        Assert.Equal(16, result.Height);
        
        // Verify that the pattern has changed due to transformation
        // At least one of the color positions should be different
        bool patternChanged = false;
        
        if (!ColorEquals(tileImage[0, 0], result[0, 0]) ||
            !ColorEquals(tileImage[15, 0], result[15, 0]) ||
            !ColorEquals(tileImage[0, 15], result[0, 15]))
        {
            patternChanged = true;
        }
        
        Assert.True(patternChanged, "The pattern should change after all flips");
        
        // Verify all colors are still present in the image
        Assert.True(ContainsColor(result, 255, 0, 0), "Red should be present after transformation");
        Assert.True(ContainsColor(result, 0, 255, 0), "Green should be present after transformation");
        Assert.True(ContainsColor(result, 0, 0, 255), "Blue should be present after transformation");
    }
    
    public void Dispose()
    {
        // Clean up the temporary directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
} 