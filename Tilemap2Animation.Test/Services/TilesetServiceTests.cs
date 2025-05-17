using Moq;
using System.IO;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Test.Services;

public class TilesetServiceTests
{
    private readonly Mock<ITilesetService> _tilesetServiceMock;

    public TilesetServiceTests()
    {
        _tilesetServiceMock = new Mock<ITilesetService>();
    }

    [Fact]
    public void ResolveTilesetImagePath_WithRelativePath_ReturnsCorrectPath()
    {
        // Arrange
        var tsxFilePath = Path.Combine("path", "to", "tileset.tsx");
        var imagePath = "images/tiles.png";
        var expectedPath = Path.Combine("path", "to", "images", "tiles.png");
        
        _tilesetServiceMock.Setup(x => x.ResolveTilesetImagePath(tsxFilePath, imagePath))
            .Returns(expectedPath);

        // Act
        var result = _tilesetServiceMock.Object.ResolveTilesetImagePath(tsxFilePath, imagePath);

        // Assert
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void ResolveTilesetImagePath_WithAbsolutePath_ReturnsOriginalPath()
    {
        // Arrange
        var tsxFilePath = Path.Combine("path", "to", "tileset.tsx");
        var imagePath = Path.Combine("C:", "absolute", "path", "tiles.png");
        
        _tilesetServiceMock.Setup(x => x.ResolveTilesetImagePath(tsxFilePath, imagePath))
            .Returns(imagePath);

        // Act
        var result = _tilesetServiceMock.Object.ResolveTilesetImagePath(tsxFilePath, imagePath);

        // Assert
        Assert.Equal(imagePath, result);
    }
} 