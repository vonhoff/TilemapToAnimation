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

/// <summary>
/// Handles application startup and configuration
/// </summary>
public class Startup
{
    private readonly Tilemap2AnimationWorkflowOptions _workflowOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class
    /// </summary>
    /// <param name="workflowOptions">The workflow options</param>
    public Startup(Tilemap2AnimationWorkflowOptions workflowOptions)
    {
        _workflowOptions = workflowOptions;
    }

    /// <summary>
    /// Builds the application
    /// </summary>
    /// <returns>The workflow instance</returns>
    public Tilemap2AnimationWorkflow BuildApplication()
    {
        // Configure logging
        ConfigureLogging();
        
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Create workflow
        return serviceProvider.GetRequiredService<Tilemap2AnimationWorkflow>();
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
        // Register services
        services.AddTransient<ITilemapService, TilemapService>();
        services.AddTransient<ITilesetService, TilesetService>();
        services.AddTransient<ITilesetImageService, TilesetImageService>();
        services.AddTransient<IAnimationGeneratorService, AnimationGeneratorService>();
        services.AddTransient<IAnimationEncoderService, AnimationEncoderService>();
        
        // Register factories
        services.AddTransient<ITilemapFactory, TilemapFactory>();
        services.AddTransient<ITilesetFactory, TilesetFactory>();
        
        // Register workflow
        services.AddTransient<Tilemap2AnimationWorkflow>();
    }
} 