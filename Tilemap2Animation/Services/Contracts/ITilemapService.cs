using Tilemap2Animation.Entities;

namespace Tilemap2Animation.Services.Contracts;

public interface ITilemapService
{
    Task<Tilemap> DeserializeTmxAsync(string tmxFilePath);

    List<uint> ParseLayerData(TilemapLayer layer);

    Task<List<string>> FindTmxFilesReferencingTsxAsync(string tsxFilePath);
}