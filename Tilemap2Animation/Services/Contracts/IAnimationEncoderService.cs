using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tilemap2Animation.Services.Contracts;

public interface IAnimationEncoderService
{
    /// <summary>
    /// Saves a sequence of animation frames as a GIF.
    /// </summary>
    /// <param name="frames">The list of animation frames.</param>
    /// <param name="delays">The list of frame delays in milliseconds.</param>
    /// <param name="outputPath">The output file path.</param>
    Task SaveAsGifAsync(List<Image<Rgba32>> frames, List<int> delays, string outputPath);
    
    /// <summary>
    /// Saves a sequence of animation frames as an APNG.
    /// </summary>
    /// <param name="frames">The list of animation frames.</param>
    /// <param name="delays">The list of frame delays in milliseconds.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <remarks>
    /// This may use external tools if ImageSharp doesn't support APNG directly.
    /// </remarks>
    Task SaveAsApngAsync(List<Image<Rgba32>> frames, List<int> delays, string outputPath);
} 