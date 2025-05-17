using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class VerboseOption : ICommandLineOption<bool>
{
    public Option<bool> Option { get; }

    public VerboseOption()
    {
        Option = new Option<bool>(
            name: "--verbose",
            description: "Enable verbose logging output.");
        Option.AddAlias("-v");
    }
} 