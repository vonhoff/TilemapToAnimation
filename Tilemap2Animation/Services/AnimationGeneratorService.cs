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

    public async Task<(List<Image<Rgba32>> Frames, List<int> Delays)> GenerateAnimationFramesFromMultipleTilesetsAsync(
        Tilemap tilemap,
        List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)> tilesets,
        Dictionary<string, List<uint>> layerDataByName,
        int frameDelay)
    {
        ArgumentNullException.ThrowIfNull(tilemap);
        ArgumentNullException.ThrowIfNull(tilesets);
        ArgumentNullException.ThrowIfNull(layerDataByName);
        if (frameDelay <= 0) throw new ArgumentException("Frame delay must be greater than 0.", nameof(frameDelay));
        
        // Filter out any invalid tilesets
        var validTilesets = tilesets.Where(t => t.Tileset != null && t.TilesetImage != null).ToList();
        
        if (validTilesets.Count == 0)
        {
            throw new InvalidOperationException("No valid tilesets provided.");
        }
        
        try
        {
            // Calculate total animation duration across all tilesets
            var totalDuration = CalculateTotalAnimationDurationForMultipleTilesets(validTilesets, frameDelay);
            
            // Create a list to store all frames
            var frames = new List<Image<Rgba32>>();
            var delays = new List<int>();
            
            // Create frames for each time step
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
                
                // In Tiled, layers are drawn from bottom to top
                // The first layer in the collection is the bottommost layer
                // So we iterate through layers in the same order they appear in the TMX file
                foreach (var layer in tilemap.Layers)
                {
                    Log.Debug("Processing layer: {LayerName}", layer.Name);
                    if (layerDataByName.TryGetValue(layer.Name ?? "", out var currentLayerData))
                    {
                        // Draw tiles onto the frame for this layer using multiple tilesets
                        await DrawTilesOnFrameWithMultipleTilesetsAsync(frame, tilemap, validTilesets, currentLayerData, time);
                    }
                    else
                    {
                        // If we have no parsed data for this layer, parse it now
                        var parsedLayerData = _tilemapService.ParseLayerData(layer);
                        await DrawTilesOnFrameWithMultipleTilesetsAsync(frame, tilemap, validTilesets, parsedLayerData, time);
                    }
                }
                
                frames.Add(frame);
                delays.Add(frameDelay);
            }
            
            return (frames, delays);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating animation frames with multiple tilesets");
            throw new InvalidOperationException($"Error generating animation frames with multiple tilesets: {ex.Message}", ex);
        }
    }
    
    public int CalculateTotalAnimationDurationForMultipleTilesets(
        List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)> tilesets, 
        int frameDelay)
    {
        ArgumentNullException.ThrowIfNull(tilesets);
        if (frameDelay <= 0) throw new ArgumentException("Frame delay must be greater than 0.", nameof(frameDelay));
        
        try
        {
            // Get all animated tiles from all tilesets
            var animationDurations = new List<int>();
            
            foreach (var (_, tileset, _) in tilesets)
            {
                if (tileset == null) continue;
                
                // Find all animated tiles in this tileset
                var animatedTiles = tileset.Tiles?.Where(t => t.Animation?.Frames != null && t.Animation.Frames.Count != 0) ?? Enumerable.Empty<TilesetTile>();
                
                var tilesetTiles = animatedTiles.ToList();
                if (tilesetTiles.Count > 0)
                {
                    // Add all animation durations from this tileset
                    animationDurations.AddRange(tilesetTiles.Select(tile => tile.Animation!.Frames.Sum(f => f.Duration)));
                }
            }
            
            // If no animated tiles found in any tileset, return frame delay as total duration
            if (animationDurations.Count == 0)
            {
                return frameDelay;
            }
            
            // Calculate the least common multiple of all animation durations
            return animationDurations.Aggregate(frameDelay, LeastCommonMultiple);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating total animation duration for multiple tilesets");
            throw new InvalidOperationException($"Error calculating total animation duration for multiple tilesets: {ex.Message}", ex);
        }
    }
    
    private async Task DrawTilesOnFrameWithMultipleTilesetsAsync(
        Image<Rgba32> frame, 
        Tilemap tilemap, 
        List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)> tilesets, 
        List<uint> layerData, 
        int currentTime)
    {
        // Sort tilesets by firstGid in descending order to select the correct tileset for each tile
        var sortedTilesets = tilesets.OrderByDescending(t => t.FirstGid).ToList();
        
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
            
            // Extract flip/rotation flags
            var flippedHorizontally = (gid & FlippedHorizontallyFlag) != 0;
            var flippedVertically = (gid & FlippedVerticallyFlag) != 0;
            var flippedDiagonally = (gid & FlippedDiagonallyFlag) != 0;
            
            // Clear the flip/rotation flags to get the actual tile ID
            var actualGid = gid & TileIdMask;
            
            // Find the correct tileset for this GID
            var (firstGid, tileset, tilesetImage) = (0, (Tileset?)null, (Image<Rgba32>?)null);
            
            foreach (var tilesetInfo in sortedTilesets)
            {
                if (actualGid >= tilesetInfo.FirstGid && tilesetInfo.Tileset != null && tilesetInfo.TilesetImage != null)
                {
                    firstGid = tilesetInfo.FirstGid;
                    tileset = tilesetInfo.Tileset;
                    tilesetImage = tilesetInfo.TilesetImage;
                    break;
                }
            }
            
            // Skip if we couldn't find a valid tileset for this GID
            if (tileset == null || tilesetImage == null)
            {
                Log.Warning("Could not find a valid tileset for GID {Gid}", actualGid);
                continue;
            }
            
            // Calculate local tile ID within the tileset
            var localTileId = (int)(actualGid - firstGid);
            
            // Calculate tile position in the map
            var mapX = tileIndex % tilemap.Width;
            var mapY = (int)(tileIndex / tilemap.Width);
            
            // Get the tile definition if it's animated
            var animatedTileDefinition = tileset.Tiles?.FirstOrDefault(t => t.Id == localTileId);
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
                tileIdToDrawFromSheet = (uint)localTileId;
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
} 