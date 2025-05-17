using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class OutputFileOption : ICommandLineOption<string?>
{
    public Option<string?> Option { get; }

    public OutputFileOption()
    {
        Option = new Option<string?>(
            name: "--output",
            description: "The path for the output animation file. If not specified, derives from input filename.")
        {
            IsRequired = false
        };
        Option.AddAlias("-o");
    }
} 