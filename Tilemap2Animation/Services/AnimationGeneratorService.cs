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
    private const uint FlippedHorizontallyFlag = 0x80000000;
    private const uint FlippedVerticallyFlag = 0x40000000;
    private const uint FlippedDiagonallyFlag = 0x20000000;
    private const uint TileIdMask = ~(FlippedHorizontallyFlag | FlippedVerticallyFlag | FlippedDiagonallyFlag);
    
    public AnimationGeneratorService(ITilesetImageService tilesetImageService, ITilemapService tilemapService)
    {
        _tilesetImageService = tilesetImageService;
        _tilemapService = tilemapService;
    }
    
    public async Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateAnimationFramesAsync(
        Tilemap tilemap,
        Tileset tileset,
        Image<Rgba32> tilesetImage,
        List<uint>? layerData,
        int frameDelay)
    {
        ArgumentNullException.ThrowIfNull(tilemap);
        ArgumentNullException.ThrowIfNull(tileset);
        ArgumentNullException.ThrowIfNull(tilesetImage);
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
        ArgumentNullException.ThrowIfNull(tilemap);
        ArgumentNullException.ThrowIfNull(tileset);
        ArgumentNullException.ThrowIfNull(tilesetImage);
        ArgumentNullException.ThrowIfNull(layerDataByName);
        if (frameDelay <= 0) throw new ArgumentException("Frame delay must be greater than 0.", nameof(frameDelay));
        
        try
        {
            // Calculate total animation duration
            var totalDuration = CalculateTotalAnimationDuration(tileset, frameDelay);
            
            // Create a list to store all frames
            var frames = new List<Image<Rgba32>>();
            var delays = new List<int>();
            
            // Create frames for each time step, starting from the first frameDelay
            // to effectively skip generating the t=0 frame.
            for (var time = 0; time < totalDuration; time += frameDelay)
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
                        Log.Debug("Applied background color: {TilemapBackgroundColor}", tilemap.BackgroundColor);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to parse background color: {TilemapBackgroundColor}", tilemap.BackgroundColor);
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
        ArgumentNullException.ThrowIfNull(tileset);
        if (frameDelay <= 0) throw new ArgumentException("Frame delay must be greater than 0.", nameof(frameDelay));
        
        try
        {
            // Find all animated tiles
            var animatedTiles = tileset.Tiles?.Where(t => t.Animation?.Frames != null && t.Animation.Frames.Count != 0) ?? Enumerable.Empty<TilesetTile>();

            var tilesetTiles = animatedTiles.ToList();
            if (tilesetTiles.Count == 0)
            {
                // If no animated tiles, return frame delay as total duration
                return frameDelay;
            }
            
            // Calculate the least common multiple of all animation durations
            return tilesetTiles.Select(tile => tile.Animation!.Frames.Sum(f => f.Duration)).Aggregate(frameDelay, LeastCommonMultiple);
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
        var firstGid = (uint)tilemap.Tilesets.First().FirstGid;
        
        // Process each tile in the layer
        for (var tileIndex = 0; tileIndex < layerData.Count; tileIndex++)
        {
            // Get the GID for this tile
            var gid = layerData[tileIndex];
            
            // Skip empty tiles (GID 0)
            if (gid == 0)
            {
                continue;
            }
            
            // Calculate tile position in the map
            var mapX = tileIndex % tilemap.Width;
            var mapY = (int)(tileIndex / tilemap.Width);
            
            // Extract flip/rotation flags
            var flippedHorizontally = (gid & FlippedHorizontallyFlag) != 0;
            var flippedVertically = (gid & FlippedVerticallyFlag) != 0;
            var flippedDiagonally = (gid & FlippedDiagonallyFlag) != 0;
            
            // Clear the flip/rotation flags to get the actual tile ID
            var actualGid = gid & TileIdMask;
            
            // Calculate local tile ID within the tileset
            var localTileId = actualGid - firstGid;
            
            // Get the tile definition if it's animated
            var animatedTileDefinition = tileset.Tiles?.FirstOrDefault(t => (uint)t.Id == localTileId);
            uint tileIdToDrawFromSheet;
            
            if (animatedTileDefinition?.Animation != null && animatedTileDefinition.Animation.Frames.Count != 0)
            {
                // Calculate which frame of the animation to show at the current time
                var animationDuration = animatedTileDefinition.Animation.Frames.Sum(f => f.Duration);
                var timeInAnimation = currentTime % animationDuration;
                
                // Find the current frame
                var accumulatedDuration = 0;
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
            var tilesetColumns = tileset.Columns;
            var tileX = (int)(tileIdToDrawFromSheet % tilesetColumns) * tileset.TileWidth;
            var tileY = (int)(tileIdToDrawFromSheet / tilesetColumns) * tileset.TileHeight;
            
            // Extract the tile from the tileset
            var sourceRect = new Rectangle(tileX, tileY, tileset.TileWidth, tileset.TileHeight);
            var tileImage = await Task.Run(() => _tilesetImageService.GetTileBitmap(tilesetImage, sourceRect));
            
            // Apply any transformations
            if (flippedHorizontally || flippedVertically || flippedDiagonally)
            {
                tileImage = await Task.Run(() => _tilesetImageService.ApplyTileTransformations(tileImage, flippedHorizontally, flippedVertically, flippedDiagonally));
            }
            
            // Calculate the destination position in the frame
            var destX = mapX * tilemap.TileWidth;
            var destY = mapY * tilemap.TileHeight;
            
            // Draw the tile onto the frame, preserving transparency
            await Task.Run(() => frame.Mutate(ctx => 
            {
                // Use PixelBlenderMode.Normal to ensure proper alpha blending
                ctx.DrawImage(tileImage, new Point(destX, destY), 1f);
            }));
            
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
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
} 