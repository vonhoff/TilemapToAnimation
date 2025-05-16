using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

/// <summary>
/// Represents the verbose option
/// </summary>
public class VerboseOption : ICommandLineOption<bool>
{
    /// <summary>
    /// Gets the verbose option
    /// </summary>
    public Option<bool> Option { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VerboseOption"/> class
    /// </summary>
    public VerboseOption()
    {
        Option = new Option<bool>(
            name: "--verbose",
            description: "Enable verbose logging output.");
        Option.AddAlias("-v");
    }
} 