using Moq;
using System.Text;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Test.Services;

public class TilemapServiceTests
{
    private readonly TilemapService _sut;

    public TilemapServiceTests()
    {
        _sut = new TilemapService();
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
} 