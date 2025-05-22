using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Information)
            .WriteTo.Console()
            .CreateLogger();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITilemapService, TilemapService>();
        services.AddSingleton<ITilesetService, TilesetService>();
        services.AddSingleton<ITilesetImageService, TilesetImageService>();
        services.AddSingleton<IAnimationGeneratorService, AnimationGeneratorService>();
        services.AddSingleton<IAnimationEncoderService, AnimationEncoderService>();
        services.AddSingleton<MainWorkflow>();
    }
}