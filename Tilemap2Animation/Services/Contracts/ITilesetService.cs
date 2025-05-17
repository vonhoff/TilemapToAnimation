using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface ITilesetService
{
    /// <summary>
    /// Deserializes a TSX file into a Tileset object.
    /// </summary>
    /// <param name="tsxFilePath">The path to the TSX file.</param>
    /// <returns>A Tileset object containing the parsed TSX data.</returns>
    Task<Tileset> DeserializeTsxAsync(string tsxFilePath);
    
    /// <summary>
    /// Searches for TSX files in a directory that reference a specific image file.
    /// </summary>
    /// <param name="imageFilePath">The path to the image file.</param>
    /// <returns>A list of TSX file paths that reference the image file.</returns>
    Task<List<string>> FindTsxFilesReferencingImageAsync(string imageFilePath);
    
    /// <summary>
    /// Resolves a tileset image path relative to the TSX file.
    /// </summary>
    /// <param name="tsxFilePath">The path to the TSX file.</param>
    /// <param name="imagePath">The image path from the TSX.</param>
    /// <returns>The absolute path to the tileset image.</returns>
    string ResolveTilesetImagePath(string tsxFilePath, string imagePath);
} 