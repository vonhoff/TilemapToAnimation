using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services.Contracts;
using Tilemap2Animation.Workflows;

namespace Tilemap2Animation.Test.Workflows;

public class MainWorkflowTests
{
    private readonly Mock<ITilemapService> _tilemapServiceMock;
    private readonly Mock<ITilesetService> _tilesetServiceMock;
    private readonly Mock<ITilesetImageService> _tilesetImageServiceMock;
    private readonly Mock<IAnimationGeneratorService> _animationGeneratorServiceMock;
    private readonly Mock<IAnimationEncoderService> _animationEncoderServiceMock;
    private readonly MainWorkflow _sut;

    public MainWorkflowTests()
    {
        _tilemapServiceMock = new Mock<ITilemapService>();
        _tilesetServiceMock = new Mock<ITilesetService>();
        _tilesetImageServiceMock = new Mock<ITilesetImageService>();
        _animationGeneratorServiceMock = new Mock<IAnimationGeneratorService>();
        _animationEncoderServiceMock = new Mock<IAnimationEncoderService>();
        
        _sut = new MainWorkflow(
            _tilemapServiceMock.Object,
            _tilesetServiceMock.Object,
            _tilesetImageServiceMock.Object,
            _animationGeneratorServiceMock.Object,
            _animationEncoderServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithTmxInput_ProcessesCorrectly()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), "test.tmx");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");
        
        var options = new MainWorkflowOptions
        {
            InputFile = testFilePath,
            OutputFile = outputPath
        };
        
        var tilemap = new Tilemap
        {
            Width = 10,
            Height = 10,
            TileWidth = 16,
            TileHeight = 16,
            Layers = new List<TilemapLayer>
            {
                new TilemapLayer
                {
                    Name = "Layer1", 
                    Data = new TilemapLayerData { Encoding = "csv", Text = "1,2,3,4" }
                }
            },
            Tilesets = new List<TilemapTileset>
            {
                new TilemapTileset { FirstGid = 1, Source = "test.tsx" }
            }
        };
        
        var tileset = new Tileset
        {
            Image = new TilesetImage { Path = "test.png" }
        };
        
        var layerData = new List<uint> { 1, 2, 3, 4 };
        var tilesetImage = new Image<Rgba32>(32, 32);
        var frames = new List<Image<Rgba32>> { new Image<Rgba32>(16, 16) };
        var delays = new List<int> { 100 };
        
        _tilemapServiceMock.Setup(x => x.DeserializeTmxAsync(testFilePath)).ReturnsAsync(tilemap);
        _tilemapServiceMock.Setup(x => x.ParseLayerData(It.IsAny<TilemapLayer>())).Returns(layerData);
        _tilesetServiceMock.Setup(x => x.DeserializeTsxAsync(It.IsAny<string>())).ReturnsAsync(tileset);
        _tilesetServiceMock.Setup(x => x.ResolveTilesetImagePath(It.IsAny<string>(), It.IsAny<string>())).Returns("test.png");
        _tilesetImageServiceMock.Setup(x => x.LoadTilesetImageAsync(It.IsAny<string>())).ReturnsAsync(tilesetImage);
        _tilesetImageServiceMock.Setup(x => x.ProcessTransparency(It.IsAny<Image<Rgba32>>(), It.IsAny<Tileset>())).Returns(tilesetImage);
        _animationGeneratorServiceMock.Setup(x => x.GenerateAnimationFramesFromMultipleTilesetsAsync(
            It.IsAny<Tilemap>(),
            It.IsAny<List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)>>(),
            It.IsAny<Dictionary<string, List<uint>>>()))
            .ReturnsAsync((frames, delays));

        try
        {
            // Act
            await _sut.ExecuteAsync(options);
            
            // Assert
            _animationEncoderServiceMock.Verify(x => x.SaveAsGifAsync(
                It.IsAny<List<Image<Rgba32>>>(), 
                It.IsAny<List<int>>(), 
                It.Is<string>(s => s == outputPath)), 
                Times.Once);
        }
        finally
        {
            tilesetImage.Dispose();
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithTsxInput_FindsTmxAndProcessesCorrectly()
    {
        // Arrange
        var tsxFilePath = Path.Combine(Path.GetTempPath(), "test.tsx");
        var tmxFilePath = Path.Combine(Path.GetTempPath(), "found.tmx");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");

        var options = new MainWorkflowOptions
        {
            InputFile = tsxFilePath,
            OutputFile = outputPath
        };

        var tilemap = new Tilemap
        {
            Width = 10,
            Height = 10,
            TileWidth = 16,
            TileHeight = 16,
            Layers = new List<TilemapLayer>
            {
                new TilemapLayer
                {
                    Name = "Layer1",
                    Data = new TilemapLayerData { Encoding = "csv", Text = "1,2,3,4" }
                }
            },
            Tilesets = new List<TilemapTileset>
            {
                new TilemapTileset { FirstGid = 1, Source = "test.tsx" }
            }
        };

        var tileset = new Tileset
        {
            Image = new TilesetImage { Path = "test.png" }
        };

        var layerData = new List<uint> { 1, 2, 3, 4 };
        var tilesetImage = new Image<Rgba32>(32, 32);
        var frames = new List<Image<Rgba32>> { new Image<Rgba32>(16, 16) };
        var delays = new List<int> { 100 };

        _tilemapServiceMock.Setup(x => x.FindTmxFilesReferencingTsxAsync(tsxFilePath))
            .ReturnsAsync(new List<string> { tmxFilePath });
        _tilemapServiceMock.Setup(x => x.DeserializeTmxAsync(tmxFilePath)).ReturnsAsync(tilemap);
        _tilemapServiceMock.Setup(x => x.ParseLayerData(It.IsAny<TilemapLayer>())).Returns(layerData);
        _tilesetServiceMock.Setup(x => x.DeserializeTsxAsync(It.IsAny<string>())).ReturnsAsync(tileset);
        _tilesetServiceMock.Setup(x => x.ResolveTilesetImagePath(It.IsAny<string>(), It.IsAny<string>())).Returns("test.png");
        _tilesetImageServiceMock.Setup(x => x.LoadTilesetImageAsync(It.IsAny<string>())).ReturnsAsync(tilesetImage);
        _tilesetImageServiceMock.Setup(x => x.ProcessTransparency(It.IsAny<Image<Rgba32>>(), It.IsAny<Tileset>())).Returns(tilesetImage);
        _animationGeneratorServiceMock.Setup(x => x.GenerateAnimationFramesFromMultipleTilesetsAsync(
            It.IsAny<Tilemap>(),
            It.IsAny<List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)>>(),
            It.IsAny<Dictionary<string, List<uint>>>()))
            .ReturnsAsync((frames, delays));

        try
        {
            // Act
            await _sut.ExecuteAsync(options);

            // Assert
            _tilemapServiceMock.Verify(x => x.FindTmxFilesReferencingTsxAsync(tsxFilePath), Times.Once);
            _tilemapServiceMock.Verify(x => x.DeserializeTmxAsync(tmxFilePath), Times.Once);
            _animationEncoderServiceMock.Verify(x => x.SaveAsGifAsync(
                It.IsAny<List<Image<Rgba32>>>(),
                It.IsAny<List<int>>(),
                It.Is<string>(s => s == outputPath)),
                Times.Once);
        }
        finally
        {
            tilesetImage.Dispose();
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithTsxInput_NoTmxFound_ThrowsException()
    {
        // Arrange
        var tsxFilePath = Path.Combine(Path.GetTempPath(), "test.tsx");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");

        var options = new MainWorkflowOptions
        {
            InputFile = tsxFilePath,
            OutputFile = outputPath
        };

        var tileset = new Tileset
        {
            Image = new TilesetImage { Path = "test.png" }
        };

        _tilemapServiceMock.Setup(x => x.FindTmxFilesReferencingTsxAsync(tsxFilePath))
            .ReturnsAsync(new List<string>()); // No TMX files found
        _tilemapServiceMock.Setup(x => x.ParseLayerData(It.IsAny<TilemapLayer>()))
            .Returns(new List<uint> { 0 }); // For dummy layer
        _tilesetServiceMock.Setup(x => x.DeserializeTsxAsync(tsxFilePath)).ReturnsAsync(tileset);
        _tilesetServiceMock.Setup(x => x.ResolveTilesetImagePath(It.IsAny<string>(), It.IsAny<string>())).Returns("test.png");
        
        var tilesetImage = new Image<Rgba32>(32, 32);
        _tilesetImageServiceMock.Setup(x => x.LoadTilesetImageAsync(It.IsAny<string>())).ReturnsAsync(tilesetImage);
        _tilesetImageServiceMock.Setup(x => x.ProcessTransparency(It.IsAny<Image<Rgba32>>(), It.IsAny<Tileset>())).Returns(tilesetImage);

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(options));
            Assert.Contains("No valid tilesets with images are available to generate animation frames", exception.Message);
            
            // Verify
            _tilemapServiceMock.Verify(x => x.FindTmxFilesReferencingTsxAsync(tsxFilePath), Times.Once);
            _tilesetServiceMock.Verify(x => x.DeserializeTsxAsync(tsxFilePath), Times.Once);
        }
        finally
        {
            tilesetImage.Dispose();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithImageInput_FindsTsxAndTmxAndProcessesCorrectly()
    {
        // Arrange
        var imageFilePath = Path.Combine(Path.GetTempPath(), "test.png");
        var tsxFilePath = Path.Combine(Path.GetTempPath(), "found.tsx");
        var tmxFilePath = Path.Combine(Path.GetTempPath(), "found.tmx");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");

        var options = new MainWorkflowOptions
        {
            InputFile = imageFilePath,
            OutputFile = outputPath
        };

        var tilemap = new Tilemap
        {
            Width = 10,
            Height = 10,
            TileWidth = 16,
            TileHeight = 16,
            Layers = new List<TilemapLayer>
            {
                new TilemapLayer
                {
                    Name = "Layer1",
                    Data = new TilemapLayerData { Encoding = "csv", Text = "1,2,3,4" }
                }
            },
            Tilesets = new List<TilemapTileset>
            {
                new TilemapTileset { FirstGid = 1, Source = "found.tsx" }
            }
        };

        var tileset = new Tileset
        {
            Image = new TilesetImage { Path = "test.png" }
        };

        var layerData = new List<uint> { 1, 2, 3, 4 };
        var tilesetImage = new Image<Rgba32>(32, 32);
        var frames = new List<Image<Rgba32>> { new Image<Rgba32>(16, 16) };
        var delays = new List<int> { 100 };

        _tilesetServiceMock.Setup(x => x.FindTsxFilesReferencingImageAsync(imageFilePath))
            .ReturnsAsync(new List<string> { tsxFilePath });
        _tilemapServiceMock.Setup(x => x.FindTmxFilesReferencingTsxAsync(tsxFilePath))
            .ReturnsAsync(new List<string> { tmxFilePath });
        _tilemapServiceMock.Setup(x => x.DeserializeTmxAsync(tmxFilePath)).ReturnsAsync(tilemap);
        _tilemapServiceMock.Setup(x => x.ParseLayerData(It.IsAny<TilemapLayer>())).Returns(layerData);
        _tilesetServiceMock.Setup(x => x.DeserializeTsxAsync(It.IsAny<string>())).ReturnsAsync(tileset);
        _tilesetServiceMock.Setup(x => x.ResolveTilesetImagePath(It.IsAny<string>(), It.IsAny<string>())).Returns("test.png");
        _tilesetImageServiceMock.Setup(x => x.LoadTilesetImageAsync(It.IsAny<string>())).ReturnsAsync(tilesetImage);
        _tilesetImageServiceMock.Setup(x => x.ProcessTransparency(It.IsAny<Image<Rgba32>>(), It.IsAny<Tileset>())).Returns(tilesetImage);
        _animationGeneratorServiceMock.Setup(x => x.GenerateAnimationFramesFromMultipleTilesetsAsync(
            It.IsAny<Tilemap>(),
            It.IsAny<List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)>>(),
            It.IsAny<Dictionary<string, List<uint>>>()))
            .ReturnsAsync((frames, delays));

        try
        {
            // Act
            await _sut.ExecuteAsync(options);

            // Assert
            _tilesetServiceMock.Verify(x => x.FindTsxFilesReferencingImageAsync(imageFilePath), Times.Once);
            _tilemapServiceMock.Verify(x => x.FindTmxFilesReferencingTsxAsync(tsxFilePath), Times.Once);
            _tilemapServiceMock.Verify(x => x.DeserializeTmxAsync(tmxFilePath), Times.Once);
            _animationEncoderServiceMock.Verify(x => x.SaveAsGifAsync(
                It.IsAny<List<Image<Rgba32>>>(),
                It.IsAny<List<int>>(),
                It.Is<string>(s => s == outputPath)),
                Times.Once);
        }
        finally
        {
            tilesetImage.Dispose();
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithImageInput_FindsTsxButNoTmx_ThrowsException()
    {
        // Arrange
        var imageFilePath = Path.Combine(Path.GetTempPath(), "test.png");
        var tsxFilePath = Path.Combine(Path.GetTempPath(), "found.tsx");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");

        var options = new MainWorkflowOptions
        {
            InputFile = imageFilePath,
            OutputFile = outputPath
        };

        var tileset = new Tileset
        {
            Image = new TilesetImage { Path = "test.png" }
        };

        _tilesetServiceMock.Setup(x => x.FindTsxFilesReferencingImageAsync(imageFilePath))
            .ReturnsAsync(new List<string> { tsxFilePath });
        _tilemapServiceMock.Setup(x => x.FindTmxFilesReferencingTsxAsync(tsxFilePath))
            .ReturnsAsync(new List<string>()); // No TMX files found
        _tilesetServiceMock.Setup(x => x.DeserializeTsxAsync(It.IsAny<string>())).ReturnsAsync(tileset);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(options));

        // Verify
        _tilesetServiceMock.Verify(x => x.FindTsxFilesReferencingImageAsync(imageFilePath), Times.Once);
        _tilemapServiceMock.Verify(x => x.FindTmxFilesReferencingTsxAsync(tsxFilePath), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithImageInput_NoTsxFound_ThrowsException()
    {
        // Arrange
        var imageFilePath = Path.Combine(Path.GetTempPath(), "test.png");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");

        var options = new MainWorkflowOptions
        {
            InputFile = imageFilePath,
            OutputFile = outputPath
        };

        _tilesetServiceMock.Setup(x => x.FindTsxFilesReferencingImageAsync(imageFilePath))
            .ReturnsAsync(new List<string>()); // No TSX files found

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(options));
        Assert.Contains("No TSX file found referencing the input image", exception.Message);

        // Verify
        _tilesetServiceMock.Verify(x => x.FindTsxFilesReferencingImageAsync(imageFilePath), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoLayers_ThrowsException()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), "test.tmx");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");
        
        var options = new MainWorkflowOptions
        {
            InputFile = testFilePath,
            OutputFile = outputPath
        };
        
        var tilemap = new Tilemap
        {
            Width = 10,
            Height = 10,
            TileWidth = 16,
            TileHeight = 16,
            Layers = new List<TilemapLayer>(), // Empty layers
            Tilesets = new List<TilemapTileset>
            {
                new TilemapTileset { FirstGid = 1, Source = "test.tsx" }
            }
        };
        
        var tileset = new Tileset
        {
            Image = new TilesetImage { Path = "test.png" }
        };
        
        var tilesetImage = new Image<Rgba32>(32, 32);
        
        _tilemapServiceMock.Setup(x => x.DeserializeTmxAsync(testFilePath)).ReturnsAsync(tilemap);
        _tilesetServiceMock.Setup(x => x.DeserializeTsxAsync(It.IsAny<string>())).ReturnsAsync(tileset);
        _tilesetServiceMock.Setup(x => x.ResolveTilesetImagePath(It.IsAny<string>(), It.IsAny<string>())).Returns("test.png");
        _tilesetImageServiceMock.Setup(x => x.LoadTilesetImageAsync(It.IsAny<string>())).ReturnsAsync(tilesetImage);
        _tilesetImageServiceMock.Setup(x => x.ProcessTransparency(It.IsAny<Image<Rgba32>>(), It.IsAny<Tileset>())).Returns(tilesetImage);

        try
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(options));
            Assert.Contains("No layer data available to generate animation", exception.Message);
        }
        finally
        {
            tilesetImage.Dispose();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithNoValidTilesets_ThrowsException()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), "test.tmx");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");
        
        var options = new MainWorkflowOptions
        {
            InputFile = testFilePath,
            OutputFile = outputPath
        };
        
        var tilemap = new Tilemap
        {
            Width = 10,
            Height = 10,
            TileWidth = 16,
            TileHeight = 16,
            Layers = new List<TilemapLayer>
            {
                new TilemapLayer
                {
                    Name = "Layer1", 
                    Data = new TilemapLayerData { Encoding = "csv", Text = "1,2,3,4" }
                }
            },
            Tilesets = new List<TilemapTileset>
            {
                new TilemapTileset { FirstGid = 1, Source = "test.tsx" }
            }
        };
        
        var tileset = new Tileset
        {
            // Missing image property
        };
        
        var layerData = new List<uint> { 1, 2, 3, 4 };
        
        _tilemapServiceMock.Setup(x => x.DeserializeTmxAsync(testFilePath)).ReturnsAsync(tilemap);
        _tilemapServiceMock.Setup(x => x.ParseLayerData(It.IsAny<TilemapLayer>())).Returns(layerData);
        _tilesetServiceMock.Setup(x => x.DeserializeTsxAsync(It.IsAny<string>())).ReturnsAsync(tileset);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(options));
        
        // Updated error message to match actual implementation
        Assert.Contains("No tilesets could be loaded for the conversion", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleExtensions_ProcessesCorrectly()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), "test.gif.tmx");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");
        
        var options = new MainWorkflowOptions
        {
            InputFile = testFilePath,
            OutputFile = outputPath
        };
        
        var tilemap = new Tilemap
        {
            Width = 10,
            Height = 10,
            TileWidth = 16,
            TileHeight = 16,
            Layers = new List<TilemapLayer>
            {
                new TilemapLayer
                {
                    Name = "Layer1", 
                    Data = new TilemapLayerData { Encoding = "csv", Text = "1,2,3,4" }
                }
            },
            Tilesets = new List<TilemapTileset>
            {
                new TilemapTileset { FirstGid = 1, Source = "test.tsx" }
            }
        };
        
        var tileset = new Tileset
        {
            Image = new TilesetImage { Path = "test.png" }
        };
        
        var layerData = new List<uint> { 1, 2, 3, 4 };
        var tilesetImage = new Image<Rgba32>(32, 32);
        var frames = new List<Image<Rgba32>> { new Image<Rgba32>(16, 16) };
        var delays = new List<int> { 100 };
        
        _tilemapServiceMock.Setup(x => x.DeserializeTmxAsync(testFilePath)).ReturnsAsync(tilemap);
        _tilemapServiceMock.Setup(x => x.ParseLayerData(It.IsAny<TilemapLayer>())).Returns(layerData);
        _tilesetServiceMock.Setup(x => x.DeserializeTsxAsync(It.IsAny<string>())).ReturnsAsync(tileset);
        _tilesetServiceMock.Setup(x => x.ResolveTilesetImagePath(It.IsAny<string>(), It.IsAny<string>())).Returns("test.png");
        _tilesetImageServiceMock.Setup(x => x.LoadTilesetImageAsync(It.IsAny<string>())).ReturnsAsync(tilesetImage);
        _tilesetImageServiceMock.Setup(x => x.ProcessTransparency(It.IsAny<Image<Rgba32>>(), It.IsAny<Tileset>())).Returns(tilesetImage);
        _animationGeneratorServiceMock.Setup(x => x.GenerateAnimationFramesFromMultipleTilesetsAsync(
            It.IsAny<Tilemap>(),
            It.IsAny<List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)>>(),
            It.IsAny<Dictionary<string, List<uint>>>()))
            .ReturnsAsync((frames, delays));

        try
        {
            // Act
            await _sut.ExecuteAsync(options);
            
            // Assert
            _tilemapServiceMock.Verify(x => x.DeserializeTmxAsync(testFilePath), Times.Once);
            _animationEncoderServiceMock.Verify(x => x.SaveAsGifAsync(
                It.IsAny<List<Image<Rgba32>>>(), 
                It.IsAny<List<int>>(), 
                It.Is<string>(s => s == outputPath)), 
                Times.Once);
        }
        finally
        {
            tilesetImage.Dispose();
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUnsupportedExtension_ThrowsArgumentException()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), "test.xyz");
        var outputPath = Path.Combine(Path.GetTempPath(), "output.gif");
        
        var options = new MainWorkflowOptions
        {
            InputFile = testFilePath,
            OutputFile = outputPath
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.ExecuteAsync(options));
        Assert.Contains("Unsupported input file type", exception.Message);
        Assert.Contains(".tmx", exception.Message);
        Assert.Contains(".tsx", exception.Message);
        Assert.Contains(".png", exception.Message);
    }
} 