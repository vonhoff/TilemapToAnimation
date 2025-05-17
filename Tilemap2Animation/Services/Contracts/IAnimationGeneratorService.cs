using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface IAnimationGeneratorService
{
    /// <summary>
    /// Generates animation frames from the provided tilemap and tileset data.
    /// </summary>
    /// <param name="tilemap">The parsed tilemap.</param>
    /// <param name="tileset">The parsed tileset.</param>
    /// <param name="tilesetImage">The tileset image.</param>
    /// <param name="layerData">The parsed layer data (GIDs).</param>
    /// <param name="frameDelay">The global frame delay in milliseconds.</param>
    /// <returns>A tuple containing a list of animation frames and their corresponding delays.</returns>
    Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateAnimationFramesAsync(
        Tilemap tilemap,
        Tileset tileset,
        Image<Rgba32> tilesetImage,
        List<uint> layerData,
        int frameDelay);
        
    /// <summary>
    /// Generates animation frames from the provided tilemap and tileset data, processing multiple layers.
    /// </summary>
    /// <param name="tilemap">The parsed tilemap.</param>
    /// <param name="tileset">The parsed tileset.</param>
    /// <param name="tilesetImage">The tileset image.</param>
    /// <param name="layerDataByName">Dictionary of layer data indexed by layer name.</param>
    /// <param name="frameDelay">The global frame delay in milliseconds.</param>
    /// <returns>A tuple containing a list of animation frames and their corresponding delays.</returns>
    Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateAnimationFramesFromLayersAsync(
        Tilemap tilemap,
        Tileset tileset,
        Image<Rgba32> tilesetImage,
        Dictionary<string, List<uint>> layerDataByName,
        int frameDelay);
    
    /// <summary>
    /// Determines the total animation duration based on the animated tiles in the tileset.
    /// </summary>
    /// <param name="tileset">The tileset containing animated tiles.</param>
    /// <param name="frameDelay">The global frame delay in milliseconds.</param>
    /// <returns>The total animation duration in milliseconds.</returns>
    int CalculateTotalAnimationDuration(Tileset tileset, int frameDelay);
} 