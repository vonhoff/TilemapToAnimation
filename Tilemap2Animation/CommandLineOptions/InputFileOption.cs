using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class InputFileOption : ICommandLineOption<string>
{
    public InputFileOption()
    {
        Option = new Option<string>(
            "--input",
            "Input file path");
        Option.AddAlias("-i");
        Option.IsRequired = true;
    }

    public Option<string> Option { get; }

    public Option<string> Register(Command command)
    {
        command.Add(Option);
        command.AddValidator(result =>
        {
            var inputResult = result.FindResultFor(Option);
            if (inputResult == null) return;

            var inputPath = inputResult.GetValueOrDefault<string>();
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                result.ErrorMessage = "The input path cannot be empty.";
                return;
            }

            if (File.Exists(inputPath) == false) result.ErrorMessage = $"The input path '{inputPath}' does not exist.";
        });
        return Option;
    }
}