using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface ITilesetImageService
{
    Task<Image<Rgba32>> LoadTilesetImageAsync(string imageFilePath);

    Image<Rgba32> ProcessTransparency(Image<Rgba32> tilesetImage, Tileset tileset);

    Image<Rgba32> GetTileBitmap(Image<Rgba32> tilesetImage, Rectangle sourceRect);

    Image<Rgba32> ApplyTileTransformations(Image<Rgba32> tileImage, bool flippedHorizontally, bool flippedVertically,
        bool flippedDiagonally);
}