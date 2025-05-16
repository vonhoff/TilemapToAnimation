using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface ITilemapService
{
    /// <summary>
    /// Deserializes a TMX file into a Tilemap object.
    /// </summary>
    /// <param name="tmxFilePath">The path to the TMX file.</param>
    /// <returns>A Tilemap object containing the parsed TMX data.</returns>
    Task<Tilemap> DeserializeTmxAsync(string tmxFilePath);
    
    /// <summary>
    /// Parses the layer data from a TilemapLayer.
    /// </summary>
    /// <param name="layer">The layer to parse data from.</param>
    /// <returns>A list of tile GIDs for the layer.</returns>
    List<uint> ParseLayerData(TilemapLayer layer);
    
    /// <summary>
    /// Searches for TMX files in a directory that reference a specific TSX file.
    /// </summary>
    /// <param name="tsxFilePath">The path to the TSX file.</param>
    /// <returns>A list of TMX file paths that reference the TSX file.</returns>
    Task<List<string>> FindTmxFilesReferencingTsxAsync(string tsxFilePath);
} 