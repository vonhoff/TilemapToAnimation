using System.CommandLine;
using System.CommandLine.Binding;
using Tilemap2Animation.CommandLineOptions.Contracts;
using Tilemap2Animation.Workflows;

namespace Tilemap2Animation.CommandLineOptions.Binding;

public class Tilemap2AnimationOptionsBinder : BinderBase<MainWorkflowOptions>
{
    private readonly Option<string> _inputFileOption;
    private readonly Option<string?> _outputFileOption;
    private readonly Option<int> _fpsOption;
    private readonly Option<bool> _verboseOption;

    public Tilemap2AnimationOptionsBinder(
        Command rootCommand,
        ICommandLineOption<string> inputFileOption,
        ICommandLineOption<string?> outputFileOption,
        ICommandLineOption<int> fpsOption,
        ICommandLineOption<bool> verboseOption)
    {
        _inputFileOption = inputFileOption.Option;
        rootCommand.AddOption(_inputFileOption);
        
        _outputFileOption = outputFileOption.Option;
        rootCommand.AddOption(_outputFileOption);
        
        _fpsOption = fpsOption.Option;
        rootCommand.AddOption(_fpsOption);
        
        _verboseOption = verboseOption.Option;
        rootCommand.AddOption(_verboseOption);
    }

    protected override MainWorkflowOptions GetBoundValue(BindingContext bindingContext)
    {
        var fps = bindingContext.ParseResult.GetValueForOption(_fpsOption);
        var frameDelay = fps > 0 ? 1000 / fps : 42;

        return new MainWorkflowOptions
        {
            InputFile = bindingContext.ParseResult.GetValueForOption(_inputFileOption)!,
            OutputFile = bindingContext.ParseResult.GetValueForOption(_outputFileOption),
            FrameDelay = frameDelay,
            Verbose = bindingContext.ParseResult.GetValueForOption(_verboseOption)
        };
    }
} 