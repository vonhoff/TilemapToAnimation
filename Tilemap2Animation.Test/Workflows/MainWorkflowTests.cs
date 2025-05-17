using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
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
            OutputFile = outputPath,
            FrameDelay = 100
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
        _animationGeneratorServiceMock.Setup(x => x.GenerateAnimationFramesFromLayersAsync(
            It.IsAny<Tilemap>(), 
            It.IsAny<Tileset>(), 
            It.IsAny<Image<Rgba32>>(), 
            It.IsAny<Dictionary<string, List<uint>>>(), 
            It.IsAny<int>()))
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
} 