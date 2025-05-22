using System.CommandLine;
using Tilemap2Animation.CommandLineOptions.Contracts;

namespace Tilemap2Animation.CommandLineOptions;

public class FpsOption : ICommandLineOption<int>
{
    public FpsOption()
    {
        Option = new Option<int>(
            "--fps",
            description: "Frames per second",
            getDefaultValue: () => 24);
        Option.AddAlias("-z");
    }

    public Option<int> Option { get; }

    public Option<int> Register(Command command)
    {
        command.Add(Option);
        command.AddValidator(result =>
        {
            var optionResult = result.FindResultFor(Option);
            int? fps;
            try
            {
                fps = optionResult?.GetValueOrDefault<int>();
            }
            catch (InvalidOperationException)
            {
                fps = null;
            }

            if (fps is not > 0)
                result.ErrorMessage = $"Invalid FPS value '{fps}'. Animation FPS should be greater than 0.";
        });
        return Option;
    }
}