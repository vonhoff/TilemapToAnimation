using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class OutputFileOption : ICommandLineOption<string?>
{
    public OutputFileOption()
    {
        Option = new Option<string?>(
            "--output",
            "The path for the output animation file. If not specified, derives from input filename.")
        {
            IsRequired = false
        };
        Option.AddAlias("-o");
    }

    public Option<string?> Option { get; }

    public Option<string?> Register(Command command)
    {
        command.Add(Option);
        return Option;
    }
}