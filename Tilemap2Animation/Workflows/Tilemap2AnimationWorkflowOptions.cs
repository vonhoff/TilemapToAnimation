namespace Tilemap2Animation.Workflows;

public class Tilemap2AnimationWorkflowOptions
{
    public required string InputFile { get; init; }
    
    public string? OutputFile { get; init; }
    
    public string Format { get; init; } = "gif";
    
    public int FrameDelay { get; init; } = 100;
    
    public bool Verbose { get; init; }
} 