using System.Xml.Serialization;
using SixLabors.ImageSharp;

namespace Tilemap2Animation.Entities;

/// <summary>
/// Represents a Tiled tilemap (TMX)
/// </summary>
[XmlRoot("map")]
public class Tilemap
{
    /// <summary>
    /// Gets or sets the map width in tiles
    /// </summary>
    [XmlAttribute("width")]
    public int Width { get; set; }
    
    /// <summary>
    /// Gets or sets the map height in tiles
    /// </summary>
    [XmlAttribute("height")]
    public int Height { get; set; }
    
    /// <summary>
    /// Gets or sets the tile width in pixels
    /// </summary>
    [XmlAttribute("tilewidth")]
    public int TileWidth { get; set; }
    
    /// <summary>
    /// Gets or sets the tile height in pixels
    /// </summary>
    [XmlAttribute("tileheight")]
    public int TileHeight { get; set; }
    
    /// <summary>
    /// Gets or sets the background color of the map (hex format: #RRGGBB or #AARRGGBB)
    /// </summary>
    [XmlAttribute("backgroundcolor")]
    public string? BackgroundColor { get; set; }
    
    /// <summary>
    /// Gets or sets the tilemap layers
    /// </summary>
    [XmlElement("layer")]
    public List<TilemapLayer> Layers { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the tilemap tilesets
    /// </summary>
    [XmlElement("tileset")]
    public List<TilemapTileset> Tilesets { get; set; } = new();
}