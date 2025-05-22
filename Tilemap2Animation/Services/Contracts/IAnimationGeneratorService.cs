using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface IAnimationGeneratorService
{
    Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateFramesFromTilesetsAsync(
        Tilemap tilemap,
        List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)> tilesets,
        Dictionary<string, List<uint>> layerDataByName,
        int fps);

    int CalculateTotalAnimationDuration(Tileset tileset, int frameDelay);

    int CalculateTotalDurationForTilesets(List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)> tilesets,
        int frameDelay);
}