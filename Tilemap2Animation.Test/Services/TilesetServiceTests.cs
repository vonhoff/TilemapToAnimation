using System.Xml.Serialization;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services;

namespace Tilemap2Animation.Test.Services;

public class TilesetServiceTests : IDisposable
{
    private readonly TilesetService _sut;
    private readonly string _tempDirectory;

    public TilesetServiceTests()
    {
        _sut = new TilesetService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "TilesetServiceTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void ResolveTilesetImagePath_WithRelativePath_ReturnsCorrectPath()
    {
        // Arrange
        var tsxFilePath = Path.Combine("path", "to", "tileset.tsx");
        var imagePath = "images/tiles.png";
        var expectedPath = Path.GetFullPath(Path.Combine("path", "to", "images", "tiles.png"));
        
        // Act
        var result = _sut.ResolveTilesetImagePath(tsxFilePath, imagePath);

        // Assert
        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void ResolveTilesetImagePath_WithAbsolutePath_ReturnsOriginalPath()
    {
        // Arrange
        var tsxFilePath = Path.Combine("path", "to", "tileset.tsx");
        var imagePath = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory) ?? "/", "absolute", "path", "tiles.png");
        
        // Act
        var result = _sut.ResolveTilesetImagePath(tsxFilePath, imagePath);

        // Assert
        Assert.Equal(imagePath, result);
    }
    
    [Fact]
    public void ResolveTilesetImagePath_WithEmptyImagePath_ThrowsArgumentException()
    {
        // Arrange
        var tsxFilePath = Path.Combine("path", "to", "tileset.tsx");
        var imagePath = string.Empty;
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _sut.ResolveTilesetImagePath(tsxFilePath, imagePath));
        Assert.Contains("Image path cannot be null or empty", exception.Message);
    }
    
    [Fact]
    public async Task DeserializeTsxAsync_WithValidTsxFile_ReturnsTileset()
    {
        // Arrange
        var tsxFileName = "test_tileset.tsx";
        var tsxFilePath = Path.Combine(_tempDirectory, tsxFileName);
        var imageFileName = "test_image.png";
        
        var tileset = new Tileset
        {
            Name = "TestTileset",
            TileWidth = 16,
            TileHeight = 16,
            Spacing = 0,
            Margin = 0,
            TileCount = 4,
            Columns = 2,
            Image = new TilesetImage
            {
                Path = imageFileName,
                Width = 32,
                Height = 32
            }
        };
        
        // Create the TSX file
        using (var fileStream = new FileStream(tsxFilePath, FileMode.Create, FileAccess.Write))
        {
            var serializer = new XmlSerializer(typeof(Tileset));
            serializer.Serialize(fileStream, tileset);
        }
        
        // Act
        var result = await _sut.DeserializeTsxAsync(tsxFilePath);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestTileset", result.Name);
        Assert.Equal(16, result.TileWidth);
        Assert.Equal(16, result.TileHeight);
        Assert.Equal(4, result.TileCount);
        Assert.Equal(2, result.Columns);
        Assert.NotNull(result.Image);
        Assert.Equal(Path.GetFullPath(Path.Combine(_tempDirectory, imageFileName)), result.Image!.Path);
    }
    
    [Fact]
    public async Task DeserializeTsxAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "non_existent.tsx");
        
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _sut.DeserializeTsxAsync(nonExistentPath));
    }
    
    [Fact]
    public async Task DeserializeTsxAsync_WithInvalidXml_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidTsxPath = Path.Combine(_tempDirectory, "invalid.tsx");
        File.WriteAllText(invalidTsxPath, "This is not valid XML");
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeserializeTsxAsync(invalidTsxPath));
    }
    
    [Fact]
    public async Task FindTsxFilesReferencingImageAsync_WithMatchingTsx_ReturnsMatchingFiles()
    {
        // Arrange
        var imageFileName = "referenced_image.png";
        var imagePath = Path.Combine(_tempDirectory, imageFileName);
        
        // Create test file to reference
        File.WriteAllText(imagePath, "Test image content");
        
        // Create TSX files that reference the image
        var tsxFile1 = CreateTilesetWithImage(_tempDirectory, "tileset1.tsx", imageFileName);
        var tsxFile2 = CreateTilesetWithImage(_tempDirectory, "tileset2.tsx", imageFileName);
        
        // Create TSX file that doesn't reference the image
        var tsxFile3 = CreateTilesetWithImage(_tempDirectory, "tileset3.tsx", "other_image.png");
        
        // Act
        var result = await _sut.FindTsxFilesReferencingImageAsync(imagePath);
        
        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(tsxFile1, result);
        Assert.Contains(tsxFile2, result);
        Assert.DoesNotContain(tsxFile3, result);
    }
    
    [Fact]
    public async Task FindTsxFilesReferencingImageAsync_WithNoMatchingTsx_ReturnsEmptyList()
    {
        // Arrange
        var imageFileName = "unreferenced_image.png";
        var imagePath = Path.Combine(_tempDirectory, imageFileName);
        
        // Create test file to reference
        File.WriteAllText(imagePath, "Test image content");
        
        // Create TSX file that doesn't reference the image
        CreateTilesetWithImage(_tempDirectory, "tileset.tsx", "other_image.png");
        
        // Act
        var result = await _sut.FindTsxFilesReferencingImageAsync(imagePath);
        
        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task FindTsxFilesReferencingImageAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.FindTsxFilesReferencingImageAsync(string.Empty));
    }
    
    private string CreateTilesetWithImage(string directory, string tsxFileName, string imageFileName)
    {
        var tsxFilePath = Path.Combine(directory, tsxFileName);
        
        var tileset = new Tileset
        {
            Name = Path.GetFileNameWithoutExtension(tsxFileName),
            TileWidth = 16,
            TileHeight = 16,
            TileCount = 4,
            Columns = 2,
            Image = new TilesetImage
            {
                Path = imageFileName,
                Width = 32,
                Height = 32
            }
        };
        
        using (var fileStream = new FileStream(tsxFilePath, FileMode.Create, FileAccess.Write))
        {
            var serializer = new XmlSerializer(typeof(Tileset));
            serializer.Serialize(fileStream, tileset);
        }
        
        return tsxFilePath;
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