namespace Tilemap2Animation.Workflows;

public class MainWorkflowOptions
{
    public required string InputFile { get; init; }
    
    public string? OutputFile { get; init; }
    
    public bool Verbose { get; init; }
} 