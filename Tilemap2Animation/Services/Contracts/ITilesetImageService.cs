using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface ITilesetImageService
{
    /// <summary>
    /// Loads a tileset image from a file.
    /// </summary>
    /// <param name="imageFilePath">The path to the tileset image file.</param>
    /// <returns>The loaded image.</returns>
    Task<Image<Rgba32>> LoadTilesetImageAsync(string imageFilePath);
    
    /// <summary>
    /// Processes transparency in a tileset image based on the transparency color defined in the tileset.
    /// </summary>
    /// <param name="tilesetImage">The tileset image to process.</param>
    /// <param name="tileset">The tileset containing transparency information.</param>
    /// <returns>A processed image with transparency applied.</returns>
    Image<Rgba32> ProcessTransparency(Image<Rgba32> tilesetImage, Tileset tileset);
    
    /// <summary>
    /// Extracts a tile from the tileset image.
    /// </summary>
    /// <param name="tilesetImage">The tileset image.</param>
    /// <param name="sourceRect">The source rectangle for the tile.</param>
    /// <returns>The extracted tile image.</returns>
    Image<Rgba32> GetTileBitmap(Image<Rgba32> tilesetImage, Rectangle sourceRect);
    
    /// <summary>
    /// Applies transformations (flip, rotate) to a tile image.
    /// </summary>
    /// <param name="tileImage">The tile image to transform.</param>
    /// <param name="flippedHorizontally">Whether to flip horizontally.</param>
    /// <param name="flippedVertically">Whether to flip vertically.</param>
    /// <param name="flippedDiagonally">Whether to flip diagonally.</param>
    /// <returns>The transformed tile image.</returns>
    Image<Rgba32> ApplyTileTransformations(Image<Rgba32> tileImage, bool flippedHorizontally, bool flippedVertically, bool flippedDiagonally);
} 