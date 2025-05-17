namespace Tilemap2Animation.Workflows;

public class MainWorkflowOptions
{
    public required string InputFile { get; init; }
    
    public string? OutputFile { get; init; }
    
    public int FrameDelay { get; init; } = 100;
    
    public bool Verbose { get; init; }
} 