using System.Xml.Serialization;

namespace Tilemap2Animation.Entities;

[XmlRoot("map")]
public class Tilemap
{
    [XmlAttribute("width")]
    public int Width { get; set; }
    
    [XmlAttribute("height")]
    public int Height { get; set; }
    
    [XmlAttribute("tilewidth")]
    public int TileWidth { get; set; }
    
    [XmlAttribute("tileheight")]
    public int TileHeight { get; set; }
    
    [XmlAttribute("backgroundcolor")]
    public string? BackgroundColor { get; set; }
    
    [XmlElement("layer")]
    public List<TilemapLayer> Layers { get; set; } = new();
    
    [XmlElement("tileset")]
    public List<TilemapTileset> Tilesets { get; set; } = new();
}