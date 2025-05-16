using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

/// <summary>
/// Represents the output format option
/// </summary>
public class FormatOption : ICommandLineOption<string>
{
    /// <summary>
    /// Gets the format option
    /// </summary>
    public Option<string> Option { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatOption"/> class
    /// </summary>
    public FormatOption()
    {
        Option = new Option<string>(
            name: "--format",
            description: "The output format: 'gif' or 'apng'. Defaults to 'gif'.")
        {
            IsRequired = false
        };
        Option.AddAlias("-f");
        Option.SetDefaultValue("gif");
    }
} 