using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Services;

public class AnimationGeneratorService : IAnimationGeneratorService
{
    private readonly ITilesetImageService _tilesetImageService;
    private readonly ITilemapService _tilemapService;
    
    // GID bit flags for flipping/rotation in Tiled
    private const uint FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
    private const uint FLIPPED_VERTICALLY_FLAG = 0x40000000;
    private const uint FLIPPED_DIAGONALLY_FLAG = 0x20000000;
    private const uint TILE_ID_MASK = ~(FLIPPED_HORIZONTALLY_FLAG | FLIPPED_VERTICALLY_FLAG | FLIPPED_DIAGONALLY_FLAG);
    
    public AnimationGeneratorService(ITilesetImageService tilesetImageService, ITilemapService tilemapService)
    {
        _tilesetImageService = tilesetImageService;
        _tilemapService = tilemapService;
    }
    
    public async Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateAnimationFramesAsync(
        Tilemap tilemap,
        Tileset tileset,
        Image<Rgba32> tilesetImage,
        List<uint> layerData,
        int frameDelay)
    {
        if (tilemap == null) throw new ArgumentNullException(nameof(tilemap));
        if (tileset == null) throw new ArgumentNullException(nameof(tileset));
        if (tilesetImage == null) throw new ArgumentNullException(nameof(tilesetImage));
        if (frameDelay <= 0) throw new ArgumentException("Frame delay must be greater than 0.", nameof(frameDelay));
        
        // Convert from deprecated single layer to multi-layer processing
        var allLayerData = new Dictionary<string, List<uint>> 
        { 
            { "Layer 1", layerData ?? new List<uint>() } 
        };
        
        return await GenerateAnimationFramesFromLayersAsync(tilemap, tileset, tilesetImage, allLayerData, frameDelay);
    }

    public async Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateAnimationFramesFromLayersAsync(
        Tilemap tilemap,
        Tileset tileset,
        Image<Rgba32> tilesetImage,
        Dictionary<string, List<uint>> layerDataByName,
        int frameDelay)
    {
        if (tilemap == null) throw new ArgumentNullException(nameof(tilemap));
        if (tileset == null) throw new ArgumentNullException(nameof(tileset));
        if (tilesetImage == null) throw new ArgumentNullException(nameof(tilesetImage));
        if (layerDataByName == null) throw new ArgumentNullException(nameof(layerDataByName));
        if (frameDelay <= 0) throw new ArgumentException("Frame delay must be greater than 0.", nameof(frameDelay));
        
        try
        {
            // Calculate total animation duration
            int totalDuration = CalculateTotalAnimationDuration(tileset, frameDelay);
            
            // Create a list to store all frames
            var frames = new List<Image<Rgba32>>();
            var delays = new List<int>();
            
            // Create frames for each time step, starting from the first frameDelay
            // to effectively skip generating the t=0 frame.
            for (int time = 0; time < totalDuration; time += frameDelay)
            {
                // Create a new frame
                var frame = new Image<Rgba32>(tilemap.Width * tilemap.TileWidth, tilemap.Height * tilemap.TileHeight);
                
                // Apply background color if specified
                if (!string.IsNullOrEmpty(tilemap.BackgroundColor))
                {
                    try
                    {
                        var bgColor = Rgba32.ParseHex(tilemap.BackgroundColor);
                        frame.Mutate(ctx => ctx.Fill(bgColor));
                        Log.Debug($"Applied background color: {tilemap.BackgroundColor}");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"Failed to parse background color: {tilemap.BackgroundColor}");
                    }
                }
                
                // Process each layer in the correct order (visible layers only)
                foreach (var layer in tilemap.Layers)
                {
                    if (layerDataByName.TryGetValue(layer.Name ?? "", out var currentLayerData))
                    {
                        // Draw tiles onto the frame for this layer
                        await DrawTilesOnFrameAsync(frame, tilemap, tileset, tilesetImage, currentLayerData, time);
                    }
                    else
                    {
                        // If we have no parsed data for this layer, parse it now
                        var parsedLayerData = _tilemapService.ParseLayerData(layer);
                        await DrawTilesOnFrameAsync(frame, tilemap, tileset, tilesetImage, parsedLayerData, time);
                    }
                }
                
                frames.Add(frame);
                delays.Add(frameDelay);
            }
            
            return (frames, delays);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating animation frames");
            throw new InvalidOperationException($"Error generating animation frames: {ex.Message}", ex);
        }
    }

    public int CalculateTotalAnimationDuration(Tileset tileset, int frameDelay)
    {
        if (tileset == null) throw new ArgumentNullException(nameof(tileset));
        if (frameDelay <= 0) throw new ArgumentException("Frame delay must be greater than 0.", nameof(frameDelay));
        
        try
        {
            // Find all animated tiles
            var animatedTiles = tileset.Tiles?.Where(t => t.Animation?.Frames != null && t.Animation.Frames.Any()) ?? Enumerable.Empty<TilesetTile>();
            
            if (!animatedTiles.Any())
            {
                // If no animated tiles, return frame delay as total duration
                return frameDelay;
            }
            
            // Calculate the least common multiple of all animation durations
            int totalDuration = frameDelay;
            foreach (var tile in animatedTiles)
            {
                int animationDuration = tile.Animation!.Frames.Sum(f => f.Duration);
                totalDuration = LeastCommonMultiple(totalDuration, animationDuration);
            }
            
            return totalDuration;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating total animation duration");
            throw new InvalidOperationException($"Error calculating total animation duration: {ex.Message}", ex);
        }
    }

    private async Task DrawTilesOnFrameAsync(Image<Rgba32> frame, Tilemap tilemap, Tileset tileset, Image<Rgba32> tilesetImage, List<uint> layerData, int currentTime)
    {
        // Reference to the first tileset GID
        uint firstGid = (uint)tilemap.Tilesets.First().FirstGid;
        
        // Process each tile in the layer
        for (int tileIndex = 0; tileIndex < layerData.Count; tileIndex++)
        {
            // Get the GID for this tile
            uint gid = layerData[tileIndex];
            
            // Skip empty tiles (GID 0)
            if (gid == 0)
            {
                continue;
            }
            
            // Calculate tile position in the map
            int mapX = tileIndex % tilemap.Width;
            int mapY = (int)(tileIndex / tilemap.Width);
            
            // Extract flip/rotation flags
            bool flippedHorizontally = (gid & FLIPPED_HORIZONTALLY_FLAG) != 0;
            bool flippedVertically = (gid & FLIPPED_VERTICALLY_FLAG) != 0;
            bool flippedDiagonally = (gid & FLIPPED_DIAGONALLY_FLAG) != 0;
            
            // Clear the flip/rotation flags to get the actual tile ID
            uint actualGid = gid & TILE_ID_MASK;
            
            // Calculate local tile ID within the tileset
            uint localTileId = actualGid - firstGid;
            
            // Get the tile definition if it's animated
            var animatedTileDefinition = tileset.Tiles?.FirstOrDefault(t => (uint)t.Id == localTileId);
            uint tileIdToDrawFromSheet;
            
            if (animatedTileDefinition?.Animation != null && animatedTileDefinition.Animation.Frames.Any())
            {
                // Calculate which frame of the animation to show at the current time
                int animationDuration = animatedTileDefinition.Animation.Frames.Sum(f => f.Duration);
                int timeInAnimation = currentTime % animationDuration;
                
                // Find the current frame
                int accumulatedDuration = 0;
                var currentFrame = animatedTileDefinition.Animation.Frames.First();
                
                foreach (var animationFrame in animatedTileDefinition.Animation.Frames)
                {
                    if (accumulatedDuration + animationFrame.Duration > timeInAnimation)
                    {
                        currentFrame = animationFrame;
                        break;
                    }
                    accumulatedDuration += animationFrame.Duration;
                }
                
                // The TileId should be uint but it's defined as int in the entity
                tileIdToDrawFromSheet = (uint)currentFrame.TileId;
            }
            else
            {
                // Not animated, use the original tile ID
                tileIdToDrawFromSheet = localTileId;
            }
            
            // Calculate the position of the tile in the tileset image
            int tilesetColumns = tileset.Columns;
            int tileX = (int)(tileIdToDrawFromSheet % tilesetColumns) * tileset.TileWidth;
            int tileY = (int)(tileIdToDrawFromSheet / tilesetColumns) * tileset.TileHeight;
            
            // Extract the tile from the tileset
            var sourceRect = new Rectangle(tileX, tileY, tileset.TileWidth, tileset.TileHeight);
            var tileImage = _tilesetImageService.GetTileBitmap(tilesetImage, sourceRect);
            
            // Apply any transformations
            if (flippedHorizontally || flippedVertically || flippedDiagonally)
            {
                tileImage = _tilesetImageService.ApplyTileTransformations(tileImage, flippedHorizontally, flippedVertically, flippedDiagonally);
            }
            
            // Calculate the destination position in the frame
            int destX = mapX * tilemap.TileWidth;
            int destY = mapY * tilemap.TileHeight;
            
            // Draw the tile onto the frame, preserving transparency
            frame.Mutate(ctx => 
            {
                // Use PixelBlenderMode.Normal to ensure proper alpha blending
                ctx.DrawImage(tileImage, new Point(destX, destY), 1f);
            });
            
            // Dispose of the temporary tile image
            tileImage.Dispose();
        }
    }

    private static int LeastCommonMultiple(int a, int b)
    {
        return Math.Abs(a * b) / GreatestCommonDivisor(a, b);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
} 