using System.Xml.Serialization;
using Serilog;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Services;

public class TilesetService : ITilesetService
{
    public async Task<Tileset> DeserializeTsxAsync(string tsxFilePath)
    {
        if (!File.Exists(tsxFilePath))
        {
            throw new FileNotFoundException($"TSX file not found: {tsxFilePath}");
        }
        
        try
        {
            using var fileStream = new FileStream(tsxFilePath, FileMode.Open, FileAccess.Read);
            var serializer = new XmlSerializer(typeof(Tileset));
            var tileset = (Tileset)await Task.Run(() => serializer.Deserialize(fileStream));
            
            if (tileset == null)
            {
                throw new InvalidOperationException("Failed to deserialize TSX file.");
            }
            
            // Resolve the image path relative to the TSX file
            if (tileset.Image != null && !string.IsNullOrEmpty(tileset.Image.Path))
            {
                tileset.Image.Path = ResolveTilesetImagePath(tsxFilePath, tileset.Image.Path);
            }
            
            return tileset;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error deserializing TSX file: {tsxFilePath}");
            throw new InvalidOperationException($"Error deserializing TSX file: {ex.Message}", ex);
        }
                // Set the delay (different property name depending on ImageSharp version)
                // try
                // {
                //     // Try to set the frame delay using the current property name
                //     var metadataType = metadata.GetType();
                //     var delayProperty = metadataType.GetProperty("FrameDelay") ?? 
                //                        metadataType.GetProperty("Delay");
                //     
                //     if (delayProperty != null)
                //     {
                //         delayProperty.SetValue(metadata, delayCentiseconds);
                //         Log.Debug($"Set frame {i} delay to {delayCentiseconds} centiseconds");
                //     }
                //     else
                //     {
                //         Log.Warning("Unable to set frame delay - property not found");
                //     }
                // }
                // catch (Exception ex)
                // {
                //     Log.Warning(ex, "Error setting frame delay");
                // }
                // 
                // // Add the frame to the animation
                // image.Frames.AddFrame(frameClone.Frames.RootFrame);
                    // Check if the tileset references the image file
                    if (tileset.Image != null && !string.IsNullOrEmpty(tileset.Image.Path))
                    {
                        string tilesetImagePath = ResolveTilesetImagePath(tsxFile, tileset.Image.Path);
                        if (Path.GetFileName(tilesetImagePath).Equals(imageFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            tsxFiles.Add(tsxFile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"Error reading TSX file: {tsxFile}");
                    // Continue with the next file
                    continue;
                }
            }

            return tsxFiles;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error searching for TSX files referencing image: {imageFilePath}");
            throw new InvalidOperationException($"Error searching for TSX files: {ex.Message}", ex);
        }
    }

    public string ResolveTilesetImagePath(string tsxFilePath, string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            throw new ArgumentException("Image path cannot be null or empty.", nameof(imagePath));
        }
        
        if (Path.IsPathRooted(imagePath))
        {
            return imagePath;
        }
        
        string tsxDirectory = Path.GetDirectoryName(tsxFilePath)!;
        return Path.GetFullPath(Path.Combine(tsxDirectory, imagePath));
    }
} 