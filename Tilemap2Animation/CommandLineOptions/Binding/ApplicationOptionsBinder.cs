using System.CommandLine;
using System.CommandLine.Binding;
using Tilemap2Animation.CommandLineOptions.Contracts;
using Tilemap2Animation.Workflows;

namespace Tilemap2Animation.CommandLineOptions.Binding;

public class ApplicationOptionsBinder : BinderBase<MainWorkflowOptions>
{
    private readonly Option<int> _frameDelayOption;
    private readonly Option<string> _inputFileOption;
    private readonly Option<string?> _outputFileOption;

    public ApplicationOptionsBinder(
        Command rootCommand,
        ICommandLineOption<string> inputFileOption,
        ICommandLineOption<string?> outputFileOption,
        ICommandLineOption<int> frameDelayOption)
    {
        _inputFileOption = inputFileOption.Option;
        rootCommand.AddOption(_inputFileOption);

        _outputFileOption = outputFileOption.Option;
        rootCommand.AddOption(_outputFileOption);

        _frameDelayOption = frameDelayOption.Option;
        rootCommand.AddOption(_frameDelayOption);
    }

    protected override MainWorkflowOptions GetBoundValue(BindingContext bindingContext)
    {
        return new MainWorkflowOptions
        {
            InputFile = bindingContext.ParseResult.GetValueForOption(_inputFileOption)!,
            OutputFile = bindingContext.ParseResult.GetValueForOption(_outputFileOption),
            Fps = bindingContext.ParseResult.GetValueForOption(_frameDelayOption)
        };
    }
}