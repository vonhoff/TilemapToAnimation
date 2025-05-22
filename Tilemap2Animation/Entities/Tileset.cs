using System.Xml.Serialization;
using SixLabors.ImageSharp;

namespace Tilemap2Animation.Entities;

[XmlRoot("tileset")]
public class Tileset
{
    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;

    [XmlAttribute("tilewidth")] public int TileWidth { get; set; }

    [XmlAttribute("tileheight")] public int TileHeight { get; set; }

    [XmlAttribute("spacing")] public int Spacing { get; set; }

    [XmlAttribute("margin")] public int Margin { get; set; }

    [XmlAttribute("tilecount")] public int TileCount { get; set; }

    [XmlAttribute("columns")] public int Columns { get; set; }

    [XmlElement("image")] public TilesetImage? Image { get; set; }

    [XmlElement("tile")] public List<TilesetTile> Tiles { get; set; } = new();

    [XmlIgnore] public IReadOnlyList<TilesetTile> RegisteredTiles { get; set; } = null!;

    [XmlIgnore] public IReadOnlyDictionary<Point, uint> HashAccumulations { get; set; } = null!;

    [XmlIgnore] public Size OriginalSize { get; set; }
}