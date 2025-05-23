using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Services;

public class TilesetImageService : ITilesetImageService
{
    public async Task<Image<Rgba32>> LoadTilesetImageAsync(string imageFilePath)
    {
        if (!File.Exists(imageFilePath))
            throw new FileNotFoundException($"Tileset image file not found: {imageFilePath}");

        try
        {
            return await Image.LoadAsync<Rgba32>(imageFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading tileset image: {ImageFilePath}", imageFilePath);
            throw new InvalidOperationException($"Error loading tileset image: {ex.Message}", ex);
        }
    }

    public Image<Rgba32> ProcessTransparency(Image<Rgba32> tilesetImage, Tileset tileset)
    {
        ArgumentNullException.ThrowIfNull(tilesetImage);
        ArgumentNullException.ThrowIfNull(tileset);

        try
        {
            // Check if the tileset has a transparency color specified
            var transColorHex = tileset.Image?.Trans;
            if (string.IsNullOrEmpty(transColorHex))
                // No transparency color specified, return the original image
                return tilesetImage;

            // Parse the hexadecimal color (format: "FF00FF" for magenta)
            if (!TryParseHexColor(transColorHex, out var transColor))
            {
                Log.Warning("Failed to parse transparency color: {TransColorHex}", transColorHex);
                return tilesetImage;
            }

            Log.Debug("Processing transparency for tileset: {TilesetName}, color: #{TransColorHex}", tileset.Name,
                transColorHex);

            // Create a copy of the image to work with
            var processedImage = tilesetImage.Clone();

            // Iterate through each pixel and make the transparency color fully transparent
            processedImage.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);
                    for (var x = 0; x < pixelRow.Length; x++)
                    {
                        ref var pixel = ref pixelRow[x];

                        // Check if this pixel matches the transparent color (ignoring alpha)
                        if (pixel.R == transColor.R && pixel.G == transColor.G && pixel.B == transColor.B)
                            // Make it fully transparent
                            pixel.A = 0;
                    }
                }
            });

            return processedImage;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing tileset transparency");
            return tilesetImage; // Return original in case of error
        }
    }

    public Image<Rgba32> GetTileBitmap(Image<Rgba32> tilesetImage, Rectangle sourceRect)
    {
        try
        {
            // Ensure the source rectangle is within the bounds of the tileset image
            if (sourceRect.X < 0 || sourceRect.Y < 0 ||
                sourceRect.X + sourceRect.Width > tilesetImage.Width ||
                sourceRect.Y + sourceRect.Height > tilesetImage.Height)
            {
                // Adjust the rectangle to fit within the image bounds
                var adjustedRect = new Rectangle(
                    Math.Max(0, sourceRect.X),
                    Math.Max(0, sourceRect.Y),
                    Math.Min(sourceRect.Width, tilesetImage.Width - Math.Max(0, sourceRect.X)),
                    Math.Min(sourceRect.Height, tilesetImage.Height - Math.Max(0, sourceRect.Y))
                );

                // Log.Verbose("Adjusted tile rectangle from {OriginalRect} to {AdjustedRect} to fit within tileset bounds",
                //     sourceRect, adjustedRect);

                // If the adjusted rectangle has zero width or height, create an empty tile
                if (adjustedRect.Width <= 0 || adjustedRect.Height <= 0)
                    // Log.Warning("Cannot extract tile: source rectangle {SourceRect} is outside image bounds {ImageSize}",
                    //     sourceRect, new Size(tilesetImage.Width, tilesetImage.Height));
                    // Return a transparent tile of the requested size
                    return new Image<Rgba32>(sourceRect.Width, sourceRect.Height);

                sourceRect = adjustedRect;
            }

            // Clone the region of the tileset image for this tile
            return tilesetImage.Clone(ctx => ctx.Crop(sourceRect));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error extracting tile from tileset image at {SourceRect}", sourceRect);
            throw new InvalidOperationException($"Error extracting tile from tileset image: {ex.Message}", ex);
        }
    }

    public Image<Rgba32> ApplyTileTransformations(Image<Rgba32> tileImage, bool flippedHorizontally,
        bool flippedVertically, bool flippedDiagonally)
    {
        ArgumentNullException.ThrowIfNull(tileImage);

        try
        {
            // Create a clone of the tile image to work with
            var transformedImage = tileImage.Clone();

            // Apply transformations in the correct order:
            // 1. Diagonal flip (rotate)
            // 2. Horizontal flip
            // 3. Vertical flip

            if (flippedDiagonally)
            {
                // Diagonal flip is achieved by rotating 90 degrees clockwise and then flipping horizontally
                transformedImage.Mutate(ctx => ctx
                    .Rotate(90)
                    .Flip(FlipMode.Horizontal));

                // After diagonal flip, we need to swap horizontal and vertical flags
                (flippedHorizontally, flippedVertically) = (flippedVertically, flippedHorizontally);
            }

            if (flippedHorizontally) transformedImage.Mutate(ctx => ctx.Flip(FlipMode.Horizontal));

            if (flippedVertically) transformedImage.Mutate(ctx => ctx.Flip(FlipMode.Vertical));

            return transformedImage;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error applying tile transformations");
            throw new InvalidOperationException($"Error applying tile transformations: {ex.Message}", ex);
        }
    }

    private bool TryParseHexColor(string hex, out Rgba32 color)
    {
        color = default;
        try
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);

            if (hex.Length == 6)
            {
                // Format: RRGGBB
                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);
                color = new Rgba32(r, g, b, 255); // Full alpha
                return true;
            }

            if (hex.Length == 8)
            {
                // Format: AARRGGBB
                var a = Convert.ToByte(hex.Substring(0, 2), 16);
                var r = Convert.ToByte(hex.Substring(2, 2), 16);
                var g = Convert.ToByte(hex.Substring(4, 2), 16);
                var b = Convert.ToByte(hex.Substring(6, 2), 16);
                color = new Rgba32(r, g, b, a);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}