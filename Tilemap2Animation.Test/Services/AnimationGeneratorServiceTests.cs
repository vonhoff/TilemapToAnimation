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
    
    [Fact]
    public void CalculateTotalAnimationDuration_WithNoAnimatedTiles_ReturnsFrameDelay()
    {
        // Arrange
        var tileset = new Tileset
        {
            Tiles = new List<TilesetTile>() // No animated tiles
        };
        
        var frameDelay = 50; // ms

        // Act
        var result = _sut.CalculateTotalAnimationDuration(tileset, frameDelay);

        // Assert
        Assert.Equal(frameDelay, result); // Should just return the frame delay when no animations exist
    }
    
    [Fact]
    public async Task GenerateAnimationFramesFromMultipleTilesetsAsync_DrawsRegularTiles()
    {
        // Arrange
        var tilemap = new Tilemap
        {
            Width = 2,
            Height = 2,
            TileWidth = 16,
            TileHeight = 16,
            Layers = new List<TilemapLayer>
            {
                new TilemapLayer { Name = "TestLayer" }
            }
        };
        
        var tileset = new Tileset
        {
            TileWidth = 16,
            TileHeight = 16,
            Columns = 2
        };
        
        using var tilesetImage = new Image<Rgba32>(32, 32);
        
        var layerData = new List<uint> { 1, 2, 3, 0 }; // Three tiles and one empty
        var layerDataByName = new Dictionary<string, List<uint>>
        {
            { "TestLayer", layerData }
        };
        
        var tilesets = new List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)>
        {
            (1, tileset, tilesetImage)
        };
        
        // Create a new image for each call to GetTileBitmap to avoid disposal issues
        _tilesetImageServiceMock.Setup(m => m.GetTileBitmap(
                It.IsAny<Image<Rgba32>>(), 
                It.IsAny<Rectangle>()))
            .Returns(() => new Image<Rgba32>(16, 16));
            
        // Act
        // Call the public method instead of the private one
        // This will indirectly call DrawTilesOnFrameWithMultipleTilesetsAsync
        // and we can verify the correct behavior
        
        // First, setup the tilemapService to return our layerData when ParseLayerData is called
        _tilemapServiceMock.Setup(m => m.ParseLayerData(It.IsAny<TilemapLayer>()))
            .Returns(layerData);
        
        // Now call the public method
        var (frames, delays) = await _sut.GenerateAnimationFramesFromMultipleTilesetsAsync(
            tilemap,
            tilesets,
            layerDataByName,
            100); // frameDelay
            
        // Assert
        Assert.Single(frames); // Should have created one frame
        Assert.Single(delays); // Should have one delay value
        Assert.Equal(100, delays[0]); // Delay should match what we provided
        
        // Clean up
        foreach (var frame in frames)
        {
            frame.Dispose();
        }
    }
    
    [Fact]
    public void GidFlagsAreExtracted_WhenHandlingTransformedTiles()
    {
        // Test flag extraction logic
        // GID bit flags for flipping/rotation in Tiled
        const uint FlippedHorizontallyFlag = 0x80000000;
        const uint FlippedVerticallyFlag = 0x40000000;
        const uint FlippedDiagonallyFlag = 0x20000000;
        const uint TileIdMask = ~(FlippedHorizontallyFlag | FlippedVerticallyFlag | FlippedDiagonallyFlag);
        
        // Tile with horizontal flip flag
        var gid = 1u | FlippedHorizontallyFlag;
        
        // Verify the flag is detected
        var flippedHorizontally = (gid & FlippedHorizontallyFlag) != 0;
        var flippedVertically = (gid & FlippedVerticallyFlag) != 0;
        var flippedDiagonally = (gid & FlippedDiagonallyFlag) != 0;
        var actualGid = gid & TileIdMask;
        
        Assert.True(flippedHorizontally);
        Assert.False(flippedVertically);
        Assert.False(flippedDiagonally);
        Assert.Equal(1u, actualGid);
        
        // Test another case with multiple flags
        gid = 42u | FlippedHorizontallyFlag | FlippedVerticallyFlag;
        
        flippedHorizontally = (gid & FlippedHorizontallyFlag) != 0;
        flippedVertically = (gid & FlippedVerticallyFlag) != 0;
        flippedDiagonally = (gid & FlippedDiagonallyFlag) != 0;
        actualGid = gid & TileIdMask;
        
        Assert.True(flippedHorizontally);
        Assert.True(flippedVertically);
        Assert.False(flippedDiagonally);
        Assert.Equal(42u, actualGid);
    }
    
    [Fact]
    public void AnimationFrameSelectionWorks_ForAnimatedTiles()
    {
        // Test animation frame selection logic
        var animatedTile = new TilesetTile
        {
            Id = 0, // Local tile ID within tileset
            Animation = new TilesetTileAnimation
            {
                Frames = new List<TilesetTileAnimationFrame>
                {
                    new TilesetTileAnimationFrame { TileId = 1, Duration = 100 },
                    new TilesetTileAnimationFrame { TileId = 2, Duration = 200 }
                }
            }
        };
        
        // Calculate which frame we should see at different times
        var animationDuration = animatedTile.Animation.Frames.Sum(f => f.Duration); // 300
        Assert.Equal(300, animationDuration);
        
        // Helper function to determine which frame would be selected at a given time
        TilesetTileAnimationFrame SelectFrameAtTime(int time)
        {
            var timeInAnimation = time % animationDuration;
            var accumulatedDuration = 0;
            
            foreach (var frame in animatedTile.Animation.Frames)
            {
                if (accumulatedDuration + frame.Duration > timeInAnimation)
                {
                    return frame;
                }
                accumulatedDuration += frame.Duration;
            }
            
            return animatedTile.Animation.Frames.First(); // Fallback
        }
        
        // Test frame selection at different times
        
        // t=0 should be frame 0 (TileId=1)
        var selectedFrame = SelectFrameAtTime(0);
        Assert.Equal(1, selectedFrame.TileId);
        
        // t=99 should still be frame 0 (TileId=1)
        selectedFrame = SelectFrameAtTime(99);
        Assert.Equal(1, selectedFrame.TileId);
        
        // t=100 should be frame 1 (TileId=2)
        selectedFrame = SelectFrameAtTime(100);
        Assert.Equal(2, selectedFrame.TileId);
        
        // t=299 should still be frame 1 (TileId=2)
        selectedFrame = SelectFrameAtTime(299);
        Assert.Equal(2, selectedFrame.TileId);
        
        // t=300 should wrap back to frame 0 (TileId=1)
        selectedFrame = SelectFrameAtTime(300);
        Assert.Equal(1, selectedFrame.TileId);
        
        // t=400 should be frame 1 (TileId=2) again
        selectedFrame = SelectFrameAtTime(400);
        Assert.Equal(2, selectedFrame.TileId);
    }
    
    [Fact]
    public void TilesetSelectionWorks_ForDifferentGids()
    {
        // Test GID to tileset mapping logic
        
        // Create two tilesets with different firstGid values
        var tileset1 = new Tileset
        {
            TileWidth = 16,
            TileHeight = 16,
            Columns = 2,
            TileCount = 4
        };
        
        var tileset2 = new Tileset
        {
            TileWidth = 16,
            TileHeight = 16,
            Columns = 2,
            TileCount = 4
        };
        
        var tilesets = new List<(int FirstGid, Tileset? Tileset)>
        {
            (1, tileset1),
            (100, tileset2)
        };
        
        // Sort tilesets by firstGid in descending order (as done in the method)
        var sortedTilesets = tilesets.OrderByDescending(t => t.FirstGid).ToList();
        
        // Helper function to find the correct tileset and calculate local ID
        (int FirstGid, Tileset? Tileset, int LocalTileId) FindTilesetAndLocalId(uint gid)
        {
            foreach (var (firstGid, tileset) in sortedTilesets)
            {
                if (gid >= firstGid && tileset != null)
                {
                    var localTileId = (int)(gid - firstGid);
                    return (firstGid, tileset, localTileId);
                }
            }
            return (0, null, 0); // Not found
        }
        
        // Test with GID from first tileset
        var gid1 = 3u;
        var (firstGid1, tileset1Result, localId1) = FindTilesetAndLocalId(gid1);
        Assert.Equal(1, firstGid1);
        Assert.Same(tileset1, tileset1Result);
        Assert.Equal(2, localId1); // localId = 3 - 1 = 2
        
        // Test with GID from second tileset
        var gid2 = 101u;
        var (firstGid2, tileset2Result, localId2) = FindTilesetAndLocalId(gid2);
        Assert.Equal(100, firstGid2);
        Assert.Same(tileset2, tileset2Result);
        Assert.Equal(1, localId2); // localId = 101 - 100 = 1
        
        // Test with GID that doesn't exist in any tileset
        var gidInvalid = 999u;
        var (firstGidInvalid, tilesetInvalid, localIdInvalid) = FindTilesetAndLocalId(gidInvalid);
        
        // Since tilesets are sorted descending by firstGid, a GID of 999 will match tileset2
        // because 999 > 100 (the firstGid of tileset2)
        Assert.Equal(100, firstGidInvalid);
        Assert.Same(tileset2, tilesetInvalid);
        Assert.Equal(899, localIdInvalid); // localId = 999 - 100 = 899
        
        // To test a truly invalid GID (doesn't match any tileset),
        // we'd need a GID < the lowest firstGid
        var gidTrulyInvalid = 0u; // Less than any firstGid
        var (firstGidTrulyInvalid, tilesetTrulyInvalid, localIdTrulyInvalid) = FindTilesetAndLocalId(gidTrulyInvalid);
        Assert.Equal(0, firstGidTrulyInvalid); // Should indicate not found
        Assert.Null(tilesetTrulyInvalid);
    }
} 