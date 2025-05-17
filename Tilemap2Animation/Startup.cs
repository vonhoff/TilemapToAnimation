using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Tilemap2Animation.Common;
using Tilemap2Animation.Factories;
using Tilemap2Animation.Factories.Contracts;
using Tilemap2Animation.Services;
using Tilemap2Animation.Services.Contracts;
using Tilemap2Animation.Workflows;

namespace Tilemap2Animation;

public class Startup
{
    private readonly MainWorkflowOptions _workflowOptions;

    public Startup(MainWorkflowOptions workflowOptions)
    {
        _workflowOptions = workflowOptions;
    }

    public MainWorkflow BuildApplication()
    {
        var services = new ServiceCollection();
        ConfigureLogging();   
        ConfigureServices(services);
        
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<MainWorkflow>();
    }

    private void ConfigureLogging()
    {
        var logLevel = _workflowOptions.Verbose ? LogEventLevel.Verbose : LogEventLevel.Information;
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console()
            .CreateLogger();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ITilemapService, TilemapService>();
        services.AddTransient<ITilesetService, TilesetService>();
        services.AddTransient<ITilesetImageService, TilesetImageService>();
        services.AddTransient<IAnimationGeneratorService, AnimationGeneratorService>();
        services.AddTransient<IAnimationEncoderService, AnimationEncoderService>();
        services.AddTransient<ITilemapFactory, TilemapFactory>();
        services.AddTransient<ITilesetFactory, TilesetFactory>();
        services.AddTransient<MainWorkflow>();
    }
} 