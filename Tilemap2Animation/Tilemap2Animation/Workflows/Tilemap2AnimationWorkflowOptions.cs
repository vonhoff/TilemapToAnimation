namespace Tilemap2Animation.Workflows;

/// <summary>
/// Options for the Tilemap2Animation workflow
/// </summary>
public class Tilemap2AnimationWorkflowOptions
{
    /// <summary>
    /// Gets or sets the input file path
    /// </summary>
    public required string InputFile { get; init; }
    
    /// <summary>
    /// Gets or sets the output file path
    /// </summary>
    public string? OutputFile { get; init; }
    
    /// <summary>
    /// Gets or sets the output format
    /// </summary>
    public string Format { get; init; } = "gif";
    
    /// <summary>
    /// Gets or sets the frame delay in milliseconds
    /// </summary>
    public int FrameDelay { get; init; } = 100;
    
    /// <summary>
    /// Gets or sets a value indicating whether verbose logging is enabled
    /// </summary>
    public bool Verbose { get; init; }
} 