using Tilemap2Animation.Entities;
using Tilemap2Animation.Factories.Contracts;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Factories;

public class TilesetFactory : ITilesetFactory
{
    private readonly ITilesetService _tilesetService;
    
    public TilesetFactory(ITilesetService tilesetService)
    {
        _tilesetService = tilesetService;
    }
    
    public async Task<Tileset> CreateFromTsxFileAsync(string tsxFilePath)
    {
        return await _tilesetService.DeserializeTsxAsync(tsxFilePath);
    }
} 