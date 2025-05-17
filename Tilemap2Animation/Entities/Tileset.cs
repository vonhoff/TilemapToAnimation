using System.Xml.Serialization;
using SixLabors.ImageSharp;

namespace Tilemap2Animation.Entities;

/// <summary>
/// Represents a Tiled tileset (TSX)
/// </summary>
[XmlRoot("tileset")]
public class Tileset
{
    /// <summary>
    /// Gets or sets the tileset name
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;
    
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
    /// Gets or sets the spacing between tiles in pixels
    /// </summary>
    [XmlAttribute("spacing")]
    public int Spacing { get; set; }
    
    /// <summary>
    /// Gets or sets the margin around tiles in pixels
    /// </summary>
    [XmlAttribute("margin")]
    public int Margin { get; set; }
    
    /// <summary>
    /// Gets or sets the number of tiles in the tileset
    /// </summary>
    [XmlAttribute("tilecount")]
    public int TileCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of columns in the tileset
    /// </summary>
    [XmlAttribute("columns")]
    public int Columns { get; set; }
    
    /// <summary>
    /// Gets or sets the tileset image
    /// </summary>
    [XmlElement("image")]
    public TilesetImage? Image { get; set; }
    
    /// <summary>
    /// Gets or sets the animated tiles
    /// </summary>
    [XmlElement("tile")]
    public List<TilesetTile> Tiles { get; set; } = new();

    [XmlIgnore]
    public IReadOnlyList<TilesetTile> RegisteredTiles { get; set; } = null!;

    [XmlIgnore]
    public IReadOnlyDictionary<Point, uint> HashAccumulations { get; set; } = null!;

    [XmlIgnore]
    public Size OriginalSize { get; set; }
}