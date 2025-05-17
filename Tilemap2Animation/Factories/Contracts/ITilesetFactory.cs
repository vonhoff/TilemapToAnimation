using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Factories.Contracts;

/// <summary>
/// Factory for creating and managing Tileset objects
/// </summary>
public interface ITilesetFactory
{
    /// <summary>
    /// Creates a Tileset object from a TSX file
    /// </summary>
    /// <param name="tsxFilePath">The path to the TSX file</param>
    /// <returns>The created Tileset object</returns>
    Task<Tileset> CreateFromTsxFileAsync(string tsxFilePath);
} 