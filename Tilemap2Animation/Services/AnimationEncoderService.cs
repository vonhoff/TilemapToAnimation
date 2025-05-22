using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Services;

public class AnimationEncoderService : IAnimationEncoderService
{
    public async Task SaveAsGifAsync(List<Image<Rgba32>> frames, List<int> delays, string outputPath)
    {
        if (frames == null || frames.Count == 0) throw new ArgumentException("No frames to encode.", nameof(frames));

        if (delays == null || delays.Count != frames.Count)
            throw new ArgumentException("Delays must match the number of frames.", nameof(delays));

        try
        {
            // Ensure the output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            // Delete the file if it exists to avoid file locking issues
            if (File.Exists(outputPath)) File.Delete(outputPath);

            // Configure GIF encoder
            var gifEncoder = new GifEncoder
            {
                ColorTableMode = GifColorTableMode.Local
            };

            Log.Information("Creating GIF with {FramesCount} frames...", frames.Count);

            // Use the first frame as the base for the GIF.
            // Clone it to avoid modifying the original list's instance.
            using var image = frames[0].Clone();

            // Set metadata for the animation to loop forever
            // This should be on the root GIF metadata, not per frame.
            image.Metadata.GetGifMetadata().RepeatCount = 0;

            // The first frame is already 'image'. Set its delay.
            // ImageSharp uses centiseconds (1/100 of a second) for GIF delays
            // Convert from milliseconds to centiseconds
            if (frames.Count > 0 && delays.Count > 0)
            {
                var firstFrameDelayCentiseconds = delays[0] / 10;
                image.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = firstFrameDelayCentiseconds;
                Log.Debug("Set frame 0 delay to {FirstFrameDelayCentiseconds} centiseconds",
                    firstFrameDelayCentiseconds);
            }

            // Add subsequent frames (from the second frame onwards)
            for (var i = 1; i < frames.Count; i++)
            {
                // Clone the frame so we don't modify the original
                using var frameClone = frames[i].Clone();

                // ImageSharp uses centiseconds (1/100 of a second) for GIF delays
                // Convert from milliseconds to centiseconds
                var delayCentiseconds = delays[i] / 10;

                // Add the frame to the animation first
                image.Frames.AddFrame(frameClone.Frames.RootFrame);

                // Then set its delay on the metadata of the frame within the 'image'
                // The newly added frame will be the last one in the collection.
                image.Frames[^1].Metadata.GetGifMetadata().FrameDelay = delayCentiseconds;
                Log.Debug("Set frame {I} delay to {DelayCentiseconds} centiseconds", i, delayCentiseconds);
            }

            // Save the complete multi-frame gif
            await image.SaveAsGifAsync(outputPath, gifEncoder);

            Log.Information("Successfully saved GIF to {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving frames as GIF to {OutputPath}", outputPath);
            throw new InvalidOperationException($"Error saving frames as GIF: {ex.Message}", ex);
        }
    }
}