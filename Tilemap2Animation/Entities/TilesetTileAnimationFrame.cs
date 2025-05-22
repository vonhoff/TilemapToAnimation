using System.Xml.Serialization;

namespace Tilemap2Animation.Entities;

public class TilesetTileAnimationFrame
{
    [XmlAttribute("tileid")] public int TileId { get; set; }

    [XmlAttribute("duration")] public int Duration { get; set; }
}