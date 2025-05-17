using System.Diagnostics;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Services.Contracts;
using System.Text;

namespace Tilemap2Animation.Services;

public class AnimationEncoderService : IAnimationEncoderService
{
    public async Task SaveAsGifAsync(List<Image<Rgba32>> frames, List<int> delays, string outputPath)
    {
        if (frames == null || !frames.Any())
        {
            throw new ArgumentException("No frames to encode.", nameof(frames));
        }
        
        if (delays == null || delays.Count != frames.Count)
        {
            throw new ArgumentException("Delays must match the number of frames.", nameof(delays));
        }
        
        try
        {
            // Ensure the output directory exists
            string? directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Delete the file if it exists to avoid file locking issues
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            
            // Configure GIF encoder
            var gifEncoder = new GifEncoder
            {
                ColorTableMode = GifColorTableMode.Local
            };
            
            Log.Information($"Creating GIF with {frames.Count} frames...");
            
            // Create a new image for the animation
            // using var image = new Image<Rgba32>(Configuration.Default, frames[0].Width, frames[0].Height);
            
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
                int firstFrameDelayCentiseconds = delays[0] / 10;
                image.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = firstFrameDelayCentiseconds;
                Log.Debug($"Set frame 0 delay to {firstFrameDelayCentiseconds} centiseconds");
            }
            
            // Add subsequent frames (from the second frame onwards)
            for (int i = 1; i < frames.Count; i++)
            {
                // Clone the frame so we don't modify the original
                using var frameClone = frames[i].Clone();
                
                // Each frame needs its own metadata for delay, but it's set on the frame AFTER adding it to the main image's frames.
                // var metadata = frameClone.Metadata.GetGifMetadata();
                
                // ImageSharp uses centiseconds (1/100 of a second) for GIF delays
                // Convert from milliseconds to centiseconds
                int delayCentiseconds = delays[i] / 10;
                
                // Add the frame to the animation first
                image.Frames.AddFrame(frameClone.Frames.RootFrame);
                
                // Then set its delay on the metadata of the frame within the 'image'
                // The newly added frame will be the last one in the collection.
                image.Frames[image.Frames.Count - 1].Metadata.GetGifMetadata().FrameDelay = delayCentiseconds;
                Log.Debug($"Set frame {i} delay to {delayCentiseconds} centiseconds");
            }
            
            // Save the complete multi-frame gif
            await image.SaveAsGifAsync(outputPath, gifEncoder);
            
            Log.Information($"Successfully saved GIF to {outputPath}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error saving frames as GIF to {outputPath}");
            throw new InvalidOperationException($"Error saving frames as GIF: {ex.Message}", ex);
        }
    }

    public async Task SaveAsApngAsync(List<Image<Rgba32>> frames, List<int> delays, string outputPath)
    {
        if (frames == null || !frames.Any())
        {
            throw new ArgumentException("No frames to encode.", nameof(frames));
        }
        
        if (delays == null || delays.Count != frames.Count)
        {
            throw new ArgumentException("Delays must match the number of frames.", nameof(delays));
        }
        
        try
        {
            // Ensure the output directory exists
            string? directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create a temporary directory for frame images
            string tempDir = Path.Combine(Path.GetTempPath(), "tilemap2animation_apng_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Save each frame as a PNG file
                var frameFiles = new List<string>();
                for (int i = 0; i < frames.Count; i++)
                {
                    string framePath = Path.Combine(tempDir, $"frame_{i:D4}.png");
                    await frames[i].SaveAsPngAsync(framePath);
                    frameFiles.Add(framePath);
                }
                
                // Check if apngasm is available
                bool hasApngasm = CheckForApngasm();
                if (!hasApngasm)
                {
                    Log.Warning("apngasm tool not found. Falling back to GIF format.");
                    await SaveAsGifAsync(frames, delays, Path.ChangeExtension(outputPath, ".gif"));
                    return;
                }
                
                // Build apngasm command
                var apngasmArgs = new StringBuilder();
                for (int i = 0; i < frameFiles.Count; i++)
                {
                    if (i > 0) apngasmArgs.Append(' ');
                    apngasmArgs.Append($"\"{frameFiles[i]}\" {delays[i] / 10} 1"); // Convert ms to centiseconds
                }
                apngasmArgs.Append($" -o \"{outputPath}\"");
                
                // Run apngasm
                var startInfo = new ProcessStartInfo
                {
                    FileName = "apngasm",
                    Arguments = apngasmArgs.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"apngasm failed with exit code {process.ExitCode}: {error}");
                }
                
                Log.Information($"Successfully saved APNG to {outputPath}");
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error cleaning up temporary files");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error saving frames as APNG to {outputPath}");
            throw new InvalidOperationException($"Error saving frames as APNG: {ex.Message}", ex);
        }
    }

    private bool CheckForApngasm()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "apngasm",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process == null) return false;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
} 