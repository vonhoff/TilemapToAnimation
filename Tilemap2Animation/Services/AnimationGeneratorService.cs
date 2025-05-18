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
    private const int DefaultAnimationDuration = 100; // Default duration in ms if no animations
    private const int MinimumFrameDuration = 10; // Minimum duration for a frame in ms to avoid issues with 0ms delays
    
    public AnimationGeneratorService(ITilesetImageService tilesetImageService, ITilemapService tilemapService)
    {
        _tilesetImageService = tilesetImageService;
        _tilemapService = tilemapService;
    }

    public int CalculateTotalAnimationDuration(Tileset tileset)
    {
        ArgumentNullException.ThrowIfNull(tileset);
        
        try
        {
            var animatedTiles = tileset.Tiles?.Where(t => t.Animation?.Frames != null && t.Animation.Frames.Count != 0) ?? Enumerable.Empty<TilesetTile>();
            var animationCycleDurations = animatedTiles
                .Select(tile => tile.Animation!.Frames.Sum(f => f.Duration))
                .Where(duration => duration > 0) // Only consider positive durations
                .ToList();

            if (animationCycleDurations.Count == 0)
            {
                return DefaultAnimationDuration; // Return default if no positive-duration animations
            }
            
            // Calculate the least common multiple of all positive animation durations
            return animationCycleDurations.Aggregate(LeastCommonMultiple);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating total animation duration for a single tileset");
            throw new InvalidOperationException($"Error calculating total animation duration for a single tileset: {ex.Message}", ex);
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
        Dictionary<string, List<uint>> layerDataByName)
    {
        ArgumentNullException.ThrowIfNull(tilemap);
        ArgumentNullException.ThrowIfNull(tilesets);
        ArgumentNullException.ThrowIfNull(layerDataByName);
        
        var validTilesets = tilesets.Where(t => t.Tileset != null && t.TilesetImage != null).ToList();
        
        if (validTilesets.Count == 0)
        {
            throw new InvalidOperationException("No valid tilesets provided.");
        }
        
        try
        {
            var totalDuration = CalculateTotalAnimationDurationForMultipleTilesets(validTilesets);
            
            var frames = new List<Image<Rgba32>>();
            var delays = new List<int>();

            // If totalDuration is the default, it means no actual animations are present or all have 0 duration.
            // Create a single static frame.
            if (totalDuration == DefaultAnimationDuration)
            {
                var staticFrame = await RenderFrameAtTimeAsync(tilemap, validTilesets, layerDataByName, 0);
                frames.Add(staticFrame);
                delays.Add(DefaultAnimationDuration);
                return (frames, delays);
            }
            
            // Collect all event times (times when any tile changes its animation frame)
            var eventTimes = new SortedSet<int> { 0 }; // Start with 0

            foreach (var layer in tilemap.Layers)
            {
                if (!layerDataByName.TryGetValue(layer.Name ?? "", out var currentLayerData))
                {
                    currentLayerData = _tilemapService.ParseLayerData(layer);
                }

                for (var tileIndex = 0; tileIndex < currentLayerData.Count; tileIndex++)
                {
                    var gid = currentLayerData[tileIndex];
                    if (gid == 0) continue;

                    var actualGid = gid & TileIdMask;
                    var (firstGid, tilesetDef, _) = GetTilesetForGid(actualGid, validTilesets);

                    if (tilesetDef == null) continue;

                    var localTileId = (int)(actualGid - firstGid);
                    var animatedTileDefinition = tilesetDef.Tiles?.FirstOrDefault(t => t.Id == localTileId);

                    if (animatedTileDefinition?.Animation != null && animatedTileDefinition.Animation.Frames.Count > 0)
                    {
                        var tileAnimationFrames = animatedTileDefinition.Animation.Frames;
                        var tileCycleDuration = tileAnimationFrames.Sum(f => f.Duration);

                        if (tileCycleDuration > 0)
                        {
                            for (var cycleStartTime = 0; cycleStartTime < totalDuration; cycleStartTime += tileCycleDuration)
                            {
                                var accumulatedDurationInCycle = 0;
                                foreach (var animFrame in tileAnimationFrames)
                                {
                                    var eventTime = cycleStartTime + accumulatedDurationInCycle;
                                    if (eventTime < totalDuration) // Only add event times within the total duration
                                    {
                                        eventTimes.Add(eventTime);
                                    }
                                    accumulatedDurationInCycle += animFrame.Duration;
                                    // If the frame duration is 0, it means it shows indefinitely until next change or cycle end.
                                    // We only need one event time at the start of this frame.
                                    if (animFrame.Duration == 0) break; 
                                }
                            }
                        }
                    }
                }
            }
            
            eventTimes.Add(totalDuration); // Ensure the animation concludes at totalDuration
            var sortedEventTimes = eventTimes.ToList();

            for (var i = 0; i < sortedEventTimes.Count -1; i++)
            {
                var currentTime = sortedEventTimes[i];
                var nextTime = sortedEventTimes[i+1];
                var frameDuration = nextTime - currentTime;

                if (frameDuration <= 0) // Skip if duration is zero or negative (should not happen with SortedSet logic)
                {
                    // If it happens, it might mean multiple animation changes at the exact same millisecond.
                    // We only need one frame for that instant.
                    continue; 
                }

                var frame = await RenderFrameAtTimeAsync(tilemap, validTilesets, layerDataByName, currentTime);
                frames.Add(frame);
                delays.Add(Math.Max(MinimumFrameDuration, frameDuration)); // Ensure a minimum frame duration
            }
            
            // If, after processing, no frames were added (e.g. totalDuration was very small or event times were problematic)
            // ensure at least one frame is present to avoid empty animation errors.
            if (frames.Count == 0 && totalDuration > 0)
            {
                 Log.Warning("No frames generated despite positive totalDuration ({TotalDuration}ms). Adding a single frame.", totalDuration);
                 var fallbackFrame = await RenderFrameAtTimeAsync(tilemap, validTilesets, layerDataByName, 0);
                 frames.Add(fallbackFrame);
                 delays.Add(Math.Max(MinimumFrameDuration, totalDuration));
            }
            else if (frames.Count == 0 && totalDuration == 0) // Should be caught by DefaultAnimationDuration case, but as safety
            {
                Log.Warning("Total animation duration is 0 and no frames generated. Adding a single default frame.");
                var fallbackFrame = await RenderFrameAtTimeAsync(tilemap, validTilesets, layerDataByName, 0);
                frames.Add(fallbackFrame);
                delays.Add(DefaultAnimationDuration);
            }

            return (frames, delays);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating animation frames with multiple tilesets");
            throw new InvalidOperationException($"Error generating animation frames with multiple tilesets: {ex.Message}", ex);
        }
    }

    private async Task<Image<Rgba32>> RenderFrameAtTimeAsync(
        Tilemap tilemap,
        List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)> validTilesets,
        Dictionary<string, List<uint>> layerDataByName,
        int time)
    {
        var frame = new Image<Rgba32>(tilemap.Width * tilemap.TileWidth, tilemap.Height * tilemap.TileHeight);
        if (!string.IsNullOrEmpty(tilemap.BackgroundColor))
        {
            try
            {
                var bgColor = Rgba32.ParseHex(tilemap.BackgroundColor);
                frame.Mutate(ctx => ctx.Fill(bgColor));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse background color: {TilemapBackgroundColor}", tilemap.BackgroundColor);
            }
        }

        foreach (var layer in tilemap.Layers)
        {
            if (layerDataByName.TryGetValue(layer.Name ?? "", out var currentLayerData))
            {
                await DrawTilesOnFrameWithMultipleTilesetsAsync(frame, tilemap, validTilesets, currentLayerData, time);
            }
            else
            {
                var parsedLayerData = _tilemapService.ParseLayerData(layer);
                await DrawTilesOnFrameWithMultipleTilesetsAsync(frame, tilemap, validTilesets, parsedLayerData, time);
            }
        }
        return frame;
    }

    private (int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage) GetTilesetForGid(uint actualGid, List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)> tilesets)
    {
        // Assumes tilesets are pre-sorted by FirstGid descending or this method sorts/finds appropriately.
        // For simplicity, using the existing sorted list approach from DrawTilesOnFrameWithMultipleTilesetsAsync requires it to be passed or sorted here.
        // The original DrawTilesOnFrameWithMultipleTilesetsAsync sorts them. Let's reuse that or ensure sorted list.
        // For now, let's assume `tilesets` parameter is the `validTilesets` which should be used carefully or pre-sorted as needed.
        // This is a simplified version for event time calculation; the actual drawing method has more robust tileset finding.
        
        var sortedTilesets = tilesets.OrderByDescending(t => t.FirstGid).ToList(); // Ensure sorted for correct selection
        foreach (var tilesetInfo in sortedTilesets)
        {
            if (actualGid >= tilesetInfo.FirstGid && tilesetInfo.Tileset != null)
            {
                return tilesetInfo;
            }
        }
        return (0, null, null);
    }
    
    public int CalculateTotalAnimationDurationForMultipleTilesets(
        List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)> tilesets)
    {
        ArgumentNullException.ThrowIfNull(tilesets);
        
        try
        {
            var allAnimationCycleDurations = new List<int>();
            
            foreach (var (_, tileset, _) in tilesets)
            {
                if (tileset == null) continue;
                
                var animatedTiles = tileset.Tiles?.Where(t => t.Animation?.Frames != null && t.Animation.Frames.Count != 0) ?? Enumerable.Empty<TilesetTile>();
                
                allAnimationCycleDurations.AddRange(animatedTiles
                    .Select(tile => tile.Animation!.Frames.Sum(f => f.Duration))
                    .Where(duration => duration > 0)); // Only consider positive durations
            }
            
            if (allAnimationCycleDurations.Count == 0)
            {
                return DefaultAnimationDuration; // Return default if no positive-duration animations
            }
            
            // Remove duplicates before LCM calculation to avoid unnecessary computation,
            // though LCM itself would handle duplicates correctly.
            var distinctPositiveDurations = allAnimationCycleDurations.Distinct().ToList();

            if (distinctPositiveDurations.Count == 0) // Should be covered by allAnimationCycleDurations.Count == 0, but for safety
            {
                return DefaultAnimationDuration;
            }

            return distinctPositiveDurations.Aggregate(LeastCommonMultiple);
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