namespace Tilemap2Animation.CommandLineOptions;

public class Tilemap2AnimationOptions
{
    public required string InputFile { get; init; }
    public string? OutputFile { get; init; }
    public string Format { get; init; } = "gif";
    public int FrameDelay { get; init; } = 100;
} 