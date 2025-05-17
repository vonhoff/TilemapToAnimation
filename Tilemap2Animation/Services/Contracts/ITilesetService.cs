using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface ITilesetService
{
    Task<Tileset> DeserializeTsxAsync(string tsxFilePath);
    
    Task<List<string>> FindTsxFilesReferencingImageAsync(string imageFilePath);
    
    string ResolveTilesetImagePath(string tsxFilePath, string imagePath);
} 