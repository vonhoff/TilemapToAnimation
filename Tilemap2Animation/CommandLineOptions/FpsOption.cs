using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class FpsOption : ICommandLineOption<int>
{
    public Option<int> Option { get; }

    public FpsOption()
    {
        Option = new Option<int>(
            name: "--fps",
            description: "The desired frames per second (FPS) for the animation.")
        {
            IsRequired = false
        };
        Option.AddAlias("-f");
        Option.SetDefaultValue(24);
    }
} 