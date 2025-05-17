using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Factories.Contracts;

/// <summary>
/// Factory for creating and managing Tilemap objects
/// </summary>
public interface ITilemapFactory
{
    /// <summary>
    /// Creates a Tilemap object from a TMX file
    /// </summary>
    /// <param name="tmxFilePath">The path to the TMX file</param>
    /// <returns>The created Tilemap object</returns>
    Task<Tilemap> CreateFromTmxFileAsync(string tmxFilePath);
} 