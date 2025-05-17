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
    }

    public async Task<List<string>> FindTsxFilesReferencingImageAsync(string imageFilePath)
    {
        if (string.IsNullOrEmpty(imageFilePath))
        {
            throw new ArgumentException("Image file path cannot be null or empty.", nameof(imageFilePath));
        }

        try
        {
            string directory = Path.GetDirectoryName(imageFilePath) ?? ".";
            string imageFileName = Path.GetFileName(imageFilePath);
            var tsxFiles = new List<string>();

            // Search for TSX files in the directory and its subdirectories
            foreach (string tsxFile in Directory.GetFiles(directory, "*.tsx", SearchOption.AllDirectories))
            {
                try
                {
                    using var fileStream = new FileStream(tsxFile, FileMode.Open, FileAccess.Read);
                    var serializer = new XmlSerializer(typeof(Tileset));
                    var tileset = (Tileset)serializer.Deserialize(fileStream);

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