using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface IAnimationGeneratorService
{
    Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateAnimationFramesAsync(
        Tilemap tilemap,
        Tileset tileset,
        Image<Rgba32> tilesetImage,
        List<uint> layerData,
        int frameDelay);
        
    Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateAnimationFramesFromLayersAsync(
        Tilemap tilemap,
        Tileset tileset,
        Image<Rgba32> tilesetImage,
        Dictionary<string, List<uint>> layerDataByName,
        int frameDelay);
    
    int CalculateTotalAnimationDuration(Tileset tileset, int frameDelay);
} 