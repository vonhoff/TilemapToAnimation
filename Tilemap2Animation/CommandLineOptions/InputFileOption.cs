using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class InputFileOption : ICommandLineOption<string>
{
    public Option<string> Option { get; }

    public InputFileOption()
    {
        Option = new Option<string>(
            name: "--input",
            description: "The path to the input file (TMX, TSX, or tileset image).")
        {
            IsRequired = true
        };
        Option.AddAlias("-i");
    }
} 