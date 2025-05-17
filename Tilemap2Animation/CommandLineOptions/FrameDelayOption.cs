using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class FrameDelayOption : ICommandLineOption<int>
{
    public Option<int> Option { get; }

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