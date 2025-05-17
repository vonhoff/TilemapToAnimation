using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class FormatOption : ICommandLineOption<string>
{
    public Option<string> Option { get; }

    public FormatOption()
    {
        Option = new Option<string>(
            name: "--format",
            description: "The output format: 'gif', 'webp' or 'apng'. Defaults to 'gif'.")
        {
            IsRequired = false
        };
        Option.AddAlias("-f");
        Option.SetDefaultValue("gif");
    }
} 