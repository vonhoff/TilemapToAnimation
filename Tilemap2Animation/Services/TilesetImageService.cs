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
        {
            throw new FileNotFoundException($"Tileset image file not found: {imageFilePath}");
        }
        
        try
        {
            return await Image.LoadAsync<Rgba32>(imageFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error loading tileset image: {imageFilePath}");
            throw new InvalidOperationException($"Error loading tileset image: {ex.Message}", ex);
        }
    }
    
    public Image<Rgba32> ProcessTransparency(Image<Rgba32> tilesetImage, Tileset tileset)
    {
        if (tilesetImage == null) throw new ArgumentNullException(nameof(tilesetImage));
        if (tileset == null) throw new ArgumentNullException(nameof(tileset));
        
        try
        {
            // Check if the tileset has a transparency color specified
            string? transColorHex = tileset.Image?.Trans;
            if (string.IsNullOrEmpty(transColorHex))
            {
                // No transparency color specified, return the original image
                return tilesetImage;
            }
            
            // Parse the hexadecimal color (format: "FF00FF" for magenta)
            if (!TryParseHexColor(transColorHex, out var transColor))
            {
                Log.Warning($"Failed to parse transparency color: {transColorHex}");
                return tilesetImage;
            }
            
            Log.Debug($"Processing transparency for tileset: {tileset.Name}, color: #{transColorHex}");
            
            // Create a copy of the image to work with
            var processedImage = tilesetImage.Clone();
            
            // Iterate through each pixel and make the transparency color fully transparent
            processedImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        ref Rgba32 pixel = ref pixelRow[x];
                        
                        // Check if this pixel matches the transparent color (ignoring alpha)
                        if (pixel.R == transColor.R && pixel.G == transColor.G && pixel.B == transColor.B)
                        {
                            // Make it fully transparent
                            pixel.A = 0;
                        }
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
    
    private bool TryParseHexColor(string hex, out Rgba32 color)
    {
        color = default;
        try
        {
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }
            
            if (hex.Length == 6)
            {
                // Format: RRGGBB
                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);
                color = new Rgba32(r, g, b, 255); // Full alpha
                return true;
            }
            else if (hex.Length == 8)
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

    public Image<Rgba32> GetTileBitmap(Image<Rgba32> tilesetImage, Rectangle sourceRect)
    {
        try
        {
            // Clone the region of the tileset image for this tile
            return tilesetImage.Clone(ctx => ctx.Crop(sourceRect));
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error extracting tile from tileset image at {sourceRect}");
            throw new InvalidOperationException($"Error extracting tile from tileset image: {ex.Message}", ex);
        }
    }

    public Image<Rgba32> ApplyTileTransformations(Image<Rgba32> tileImage, bool flippedHorizontally, bool flippedVertically, bool flippedDiagonally)
    {
        if (tileImage == null)
        {
            throw new ArgumentNullException(nameof(tileImage));
        }

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
                var temp = flippedHorizontally;
                flippedHorizontally = flippedVertically;
                flippedVertically = temp;
            }

            if (flippedHorizontally)
            {
                transformedImage.Mutate(ctx => ctx.Flip(FlipMode.Horizontal));
            }

            if (flippedVertically)
            {
                transformedImage.Mutate(ctx => ctx.Flip(FlipMode.Vertical));
            }

            return transformedImage;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error applying tile transformations");
            throw new InvalidOperationException($"Error applying tile transformations: {ex.Message}", ex);
        }
    }
} 