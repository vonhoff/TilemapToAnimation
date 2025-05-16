using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

/// <summary>
/// Represents the frame delay option
/// </summary>
public class FrameDelayOption : ICommandLineOption<int>
{
    /// <summary>
    /// Gets the frame delay option
    /// </summary>
    public Option<int> Option { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameDelayOption"/> class
    /// </summary>
    public FrameDelayOption()
    {
        Option = new Option<int>(
            name: "--frame-delay",
            description: "The delay between frames in milliseconds. Defaults to 100ms.")
        {
            IsRequired = false
        };
        Option.AddAlias("-d");
        Option.SetDefaultValue(100);
    }
} 