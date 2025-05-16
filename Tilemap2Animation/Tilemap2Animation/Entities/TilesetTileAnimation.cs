using System.Xml.Serialization;

namespace Tilemap2Animation.Entities;

public class TilesetTileAnimation
{
    [XmlElement("frame")]
    public List<TilesetTileAnimationFrame> Frames { get; set; } = null!;

    [XmlIgnore]
    public uint Hash { get; init; }
}