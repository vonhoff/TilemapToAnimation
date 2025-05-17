using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

/// <summary>
/// Represents the output file option
/// </summary>
public class OutputFileOption : ICommandLineOption<string?>
{
    /// <summary>
    /// Gets the output file option
    /// </summary>
    public Option<string?> Option { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputFileOption"/> class
    /// </summary>
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