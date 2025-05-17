using Tilemap2Animation.Entities;
using Tilemap2Animation.Factories.Contracts;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Factories;

public class TilemapFactory : ITilemapFactory
{
    private readonly ITilemapService _tilemapService;
    
    public TilemapFactory(ITilemapService tilemapService)
    {
        _tilemapService = tilemapService;
    }
    
    public async Task<Tilemap> CreateFromTmxFileAsync(string tmxFilePath)
    {
        return await _tilemapService.DeserializeTmxAsync(tmxFilePath);
    }
} 