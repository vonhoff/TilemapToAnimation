using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Test.Services;

public class AnimationGeneratorServiceTests
{
    private readonly Mock<ITilesetImageService> _tilesetImageServiceMock;
    private readonly Mock<ITilemapService> _tilemapServiceMock;
    private readonly AnimationGeneratorService _sut;

    public AnimationGeneratorServiceTests()
    {
        _tilesetImageServiceMock = new Mock<ITilesetImageService>();
        _tilemapServiceMock = new Mock<ITilemapService>();
        
        _sut = new AnimationGeneratorService(
            _tilesetImageServiceMock.Object,
            _tilemapServiceMock.Object);
    }

    [Fact]
    public void CalculateTotalAnimationDuration_WithValidTileset_ReturnsCorrectDuration()
    {
        // Arrange
        var tileset = new Tileset 
        { 
            Tiles = new List<TilesetTile>
            {
                new TilesetTile
                {
                    Id = 1,
                    Animation = new TilesetTileAnimation
                    {
                        Frames = new List<TilesetTileAnimationFrame>
                        {
                            new TilesetTileAnimationFrame { Duration = 100 },
                            new TilesetTileAnimationFrame { Duration = 200 }
                        }
                    }
                },
                new TilesetTile
                {
                    Id = 2,
                    Animation = new TilesetTileAnimation
                    {
                        Frames = new List<TilesetTileAnimationFrame>
                        {
                            new TilesetTileAnimationFrame { Duration = 150 },
                            new TilesetTileAnimationFrame { Duration = 150 },
                            new TilesetTileAnimationFrame { Duration = 150 }
                        }
                    }
                }
            }
        };
        var frameDelay = 50; // ms

        // Act
        var result = _sut.CalculateTotalAnimationDuration(tileset, frameDelay);

        // Assert
        Assert.Equal(900, result); // The actual LCM of 300 and 450, with frameDelay 50
    }
} 