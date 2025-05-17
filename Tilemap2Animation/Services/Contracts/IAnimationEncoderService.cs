using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tilemap2Animation.Services.Contracts;

public interface IAnimationEncoderService
{
    Task SaveAsGifAsync(List<Image<Rgba32>> frames, List<int> delays, string outputPath);
} 