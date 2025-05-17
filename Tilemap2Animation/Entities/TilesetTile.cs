using System.Xml.Serialization;

namespace Tilemap2Animation.Entities;

public class TilesetTile
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlElement("animation")]
    public TilesetTileAnimation? Animation { get; set; }

    [XmlIgnore]
    public TilesetTileImage Image { get; init; }
}