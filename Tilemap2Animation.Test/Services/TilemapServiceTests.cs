using System.IO.Compression;
using System.Xml.Serialization;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services;

namespace Tilemap2Animation.Test.Services;

public class TilemapServiceTests : IDisposable
{
    private readonly TilemapService _sut;
    private readonly string _tempDirectory;

    public TilemapServiceTests()
    {
        _sut = new TilemapService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "TilemapServiceTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void ParseLayerData_WithValidLayer_ReturnsCorrectData()
    {
        // Arrange
        var layer = new TilemapLayer
        {
            Data = new TilemapLayerData
            {
                Encoding = "csv",
                Text = "1,2,3,4,5,6"
            }
        };

        // Act
        var result = _sut.ParseLayerData(layer);

        // Assert
        Assert.Equal(new List<uint> { 1, 2, 3, 4, 5, 6 }, result);
    }

    [Fact]
    public void ParseLayerData_WithBase64EncodedData_ReturnsCorrectData()
    {
        // Arrange
        var testData = new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0 }; // 1, 2, 3 in little endian format
        var base64Value = Convert.ToBase64String(testData);
        var layer = new TilemapLayer
        {
            Data = new TilemapLayerData
            {
                Encoding = "base64",
                Text = base64Value
            }
        };

        // Act
        var result = _sut.ParseLayerData(layer);

        // Assert
        Assert.Equal(new List<uint> { 1, 2, 3 }, result);
    }

    [Fact]
    public void ParseLayerData_WithBase64GzipEncodedData_ReturnsCorrectData()
    {
        // Arrange
        // Prepare test data: 1, 2, 3 in little endian format
        var originalData = new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0 };
        
        // Compress with GZip
        byte[] compressedData;
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(originalData, 0, originalData.Length);
            }
            compressedData = memoryStream.ToArray();
        }
        
        var base64Value = Convert.ToBase64String(compressedData);
        var layer = new TilemapLayer
        {
            Data = new TilemapLayerData
            {
                Encoding = "base64",
                Compression = "gzip",
                Text = base64Value
            }
        };

        // Act
        var result = _sut.ParseLayerData(layer);

        // Assert
        Assert.Equal(new List<uint> { 1, 2, 3 }, result);
    }
    
    [Fact]
    public void ParseLayerData_WithBase64ZlibEncodedData_ReturnsCorrectData()
    {
        // Arrange
        // Create a mock ZLib compressed data with a 2-byte ZLib header (78 9C - standard ZLib header)
        // followed by the equivalent of Deflate compression of the data
        // For test purposes, we'll create a simplified version
        var originalData = new byte[] { 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0 };
        
        // Create a deflate stream without the ZLib header
        byte[] deflatedData;
        using (var memoryStream = new MemoryStream())
        {
            using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
            {
                deflateStream.Write(originalData, 0, originalData.Length);
            }
            deflatedData = memoryStream.ToArray();
        }
        
        // Add ZLib header (78 9C) to the deflated data
        var zlibData = new byte[deflatedData.Length + 2];
        zlibData[0] = 0x78; // Standard ZLib header first byte
        zlibData[1] = 0x9C; // Standard ZLib header second byte
        Array.Copy(deflatedData, 0, zlibData, 2, deflatedData.Length);
        
        var base64Value = Convert.ToBase64String(zlibData);
        var layer = new TilemapLayer
        {
            Data = new TilemapLayerData
            {
                Encoding = "base64",
                Compression = "zlib",
                Text = base64Value
            }
        };

        // Act
        var result = _sut.ParseLayerData(layer);

        // Assert
        Assert.Equal(3, result.Count);
    }
    
    [Fact]
    public void ParseLayerData_WithUnsupportedCompression_ThrowsInvalidOperationException()
    {
        // Arrange
        var layer = new TilemapLayer
        {
            Data = new TilemapLayerData
            {
                Encoding = "base64",
                Compression = "unsupported",
                Text = "dGVzdA==" // "test" in base64
            }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _sut.ParseLayerData(layer));
        Assert.Contains("Unsupported compression format", exception.Message);
    }
    
    [Fact]
    public void ParseLayerData_WithNullLayer_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _sut.ParseLayerData(null));
        Assert.Contains("Layer data is missing or empty", exception.Message);
    }

    [Fact]
    public void ParseLayerData_WithUnsupportedEncoding_ThrowsInvalidOperationException()
    {
        // Arrange
        var layer = new TilemapLayer
        {
            Data = new TilemapLayerData
            {
                Encoding = "unsupported",
                Text = "some data"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _sut.ParseLayerData(layer));
        Assert.Contains("Unsupported layer data encoding", exception.Message);
    }
    
    [Fact]
    public async Task DeserializeTmxAsync_WithValidTmxFile_ReturnsTilemap()
    {
        // Arrange
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
        
        var tmxFilePath = Path.Combine(_tempDirectory, "test.tmx");
        var serializer = new XmlSerializer(typeof(Tilemap));
        
        using (var writer = new StreamWriter(tmxFilePath))
        {
            serializer.Serialize(writer, tilemap);
        }

        try
        {
            // Act
            var result = await _sut.DeserializeTmxAsync(tmxFilePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Width);
            Assert.Equal(10, result.Height);
            Assert.Equal(16, result.TileWidth);
            Assert.Equal(16, result.TileHeight);
            Assert.Single(result.Layers);
            Assert.Equal("Layer1", result.Layers[0].Name);
            Assert.Single(result.Tilesets);
            Assert.Equal(1, result.Tilesets[0].FirstGid);
            Assert.Equal("test.tsx", result.Tilesets[0].Source);
        }
        finally
        {
            // Clean up
            if (File.Exists(tmxFilePath))
            {
                File.Delete(tmxFilePath);
            }
        }
    }
    
    [Fact]
    public async Task DeserializeTmxAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "non_existent.tmx");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => _sut.DeserializeTmxAsync(nonExistentPath));
        Assert.Contains("TMX file not found", exception.Message);
    }
    
    [Fact]
    public async Task FindTmxFilesReferencingTsxAsync_WithValidReferences_ReturnsMatchingFiles()
    {
        // Arrange
        var tsxFileName = "test.tsx";
        var tsxFilePath = Path.Combine(_tempDirectory, tsxFileName);
        
        // Create a dummy TSX file
        File.WriteAllText(tsxFilePath, "<tileset/>");
        
        // Create TMX files that reference the TSX
        var tmxWithRef1 = CreateTmxWithTsxReference(_tempDirectory, "with_ref1.tmx", tsxFileName);
        var tmxWithRef2 = CreateTmxWithTsxReference(_tempDirectory, "with_ref2.tmx", tsxFileName);
        
        // Create a TMX file that doesn't reference the TSX
        var tmxWithoutRef = CreateTmxWithTsxReference(_tempDirectory, "without_ref.tmx", "other.tsx");
        
        try
        {
            // Act
            var result = await _sut.FindTmxFilesReferencingTsxAsync(tsxFilePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(tmxWithRef1, result);
            Assert.Contains(tmxWithRef2, result);
            Assert.DoesNotContain(tmxWithoutRef, result);
        }
        finally
        {
            // Clean up
            CleanupTestFiles(new[] { tsxFilePath, tmxWithRef1, tmxWithRef2, tmxWithoutRef });
        }
    }
    
    [Fact]
    public async Task FindTmxFilesReferencingTsxAsync_WithNoReferences_ReturnsEmptyList()
    {
        // Arrange
        var tsxFileName = "lonely.tsx";
        var tsxFilePath = Path.Combine(_tempDirectory, tsxFileName);
        
        // Create a dummy TSX file
        File.WriteAllText(tsxFilePath, "<tileset/>");
        
        // Create TMX files that don't reference the TSX
        var tmxWithoutRef1 = CreateTmxWithTsxReference(_tempDirectory, "without_ref1.tmx", "other.tsx");
        var tmxWithoutRef2 = CreateTmxWithTsxReference(_tempDirectory, "without_ref2.tmx", "another.tsx");
        
        try
        {
            // Act
            var result = await _sut.FindTmxFilesReferencingTsxAsync(tsxFilePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        finally
        {
            // Clean up
            CleanupTestFiles(new[] { tsxFilePath, tmxWithoutRef1, tmxWithoutRef2 });
        }
    }
    
    [Fact]
    public async Task FindTmxFilesReferencingTsxAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _sut.FindTmxFilesReferencingTsxAsync(""));
        Assert.Contains("TSX file path cannot be null or empty", exception.Message);
    }
    
    private string CreateTmxWithTsxReference(string directory, string tmxFileName, string tsxFileName)
    {
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
                new TilemapTileset { FirstGid = 1, Source = tsxFileName }
            }
        };
        
        var tmxFilePath = Path.Combine(directory, tmxFileName);
        var serializer = new XmlSerializer(typeof(Tilemap));
        
        using (var writer = new StreamWriter(tmxFilePath))
        {
            serializer.Serialize(writer, tilemap);
        }
        
        return tmxFilePath;
    }
    
    private void CleanupTestFiles(string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
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