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
    
    [Fact]
    public void CalculateTotalAnimationDurationForMultipleTilesets_WithValidTilesets_ReturnsCorrectDuration()
    {
        // Arrange
        var tileset1 = new Tileset 
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
                }
            }
        };
        
        var tileset2 = new Tileset 
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
                            new TilesetTileAnimationFrame { Duration = 150 },
                            new TilesetTileAnimationFrame { Duration = 150 },
                            new TilesetTileAnimationFrame { Duration = 150 }
                        }
                    }
                }
            }
        };
        
        var tilesetImage1 = new Image<Rgba32>(32, 32);
        var tilesetImage2 = new Image<Rgba32>(32, 32);
        
        var tilesets = new List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)>
        {
            (1, tileset1, tilesetImage1),
            (1001, tileset2, tilesetImage2)
        };
        
        var frameDelay = 50; // ms

        try
        {
            // Act
            var result = _sut.CalculateTotalAnimationDurationForMultipleTilesets(tilesets, frameDelay);

            // Assert
            Assert.Equal(900, result); // The actual LCM of 300 and 450, with frameDelay 50
        }
        finally
        {
            tilesetImage1.Dispose();
            tilesetImage2.Dispose();
        }
    }
    
    [Fact]
    public void CalculateTotalAnimationDurationForMultipleTilesets_WithNoAnimatedTiles_ReturnsFrameDelay()
    {
        // Arrange
        var tileset = new Tileset 
        { 
            Tiles = new List<TilesetTile>() // No animated tiles
        };
        
        var tilesetImage = new Image<Rgba32>(32, 32);
        
        var tilesets = new List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)>
        {
            (1, tileset, tilesetImage)
        };
        
        var frameDelay = 50; // ms

        try
        {
            // Act
            var result = _sut.CalculateTotalAnimationDurationForMultipleTilesets(tilesets, frameDelay);

            // Assert
            Assert.Equal(frameDelay, result); // Should just return the frame delay when no animations exist
        }
        finally
        {
            tilesetImage.Dispose();
        }
    }
} 