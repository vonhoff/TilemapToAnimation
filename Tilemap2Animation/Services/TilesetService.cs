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
            await using var fileStream = new FileStream(tsxFilePath, FileMode.Open, FileAccess.Read);
            var serializer = new XmlSerializer(typeof(Tileset));
            var tileset = await Task.Run(() => (Tileset?)serializer.Deserialize(fileStream));
            
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
            Log.Error(ex, "Error deserializing TSX file: {TsxFilePath}", tsxFilePath);
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
            var directory = Path.GetDirectoryName(imageFilePath) ?? ".";
            var imageFileName = Path.GetFileName(imageFilePath);
            var tsxFiles = new List<string>();

            // Search for TSX files in the directory and its subdirectories
            var tsxFilesInDirectory = await Task.Run(() => Directory.GetFiles(directory, "*.tsx", SearchOption.AllDirectories));
            
            foreach (var tsxFile in tsxFilesInDirectory)
            {
                try
                {
                    await using var fileStream = new FileStream(tsxFile, FileMode.Open, FileAccess.Read);
                    var serializer = new XmlSerializer(typeof(Tileset));
                    var tileset = await Task.Run(() => (Tileset?)serializer.Deserialize(fileStream));

                    if (tileset?.Image != null && !string.IsNullOrEmpty(tileset.Image.Path))
                    {
                        var tilesetImagePath = ResolveTilesetImagePath(tsxFile, tileset.Image.Path);
                        if (Path.GetFileName(tilesetImagePath).Equals(imageFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            tsxFiles.Add(tsxFile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error reading TSX file: {TsxFile}", tsxFile);
                }
            }

            return tsxFiles;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error searching for TSX files referencing image: {ImageFilePath}", imageFilePath);
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
        
        var tsxDirectory = Path.GetDirectoryName(tsxFilePath)!;
        return Path.GetFullPath(Path.Combine(tsxDirectory, imagePath));
    }
} 