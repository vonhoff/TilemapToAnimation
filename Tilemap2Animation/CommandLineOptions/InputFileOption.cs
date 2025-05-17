using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

/// <summary>
/// Represents the input file option
/// </summary>
public class InputFileOption : ICommandLineOption<string>
{
    /// <summary>
    /// Gets the input file option
    /// </summary>
    public Option<string> Option { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputFileOption"/> class
    /// </summary>
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