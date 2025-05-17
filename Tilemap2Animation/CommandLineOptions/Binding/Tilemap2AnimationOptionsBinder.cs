using System.CommandLine;
using System.CommandLine.Binding;
using Tilemap2Animation.CommandLineOptions.Contracts;
using Tilemap2Animation.Workflows;

namespace Tilemap2Animation.CommandLineOptions.Binding;

/// <summary>
/// Binder for Tilemap2Animation workflow options
/// </summary>
public class Tilemap2AnimationOptionsBinder : BinderBase<MainWorkflowOptions>
{
    private readonly Option<string> _inputFileOption;
    private readonly Option<string?> _outputFileOption;
    private readonly Option<string> _formatOption;
    private readonly Option<int> _frameDelayOption;
    private readonly Option<bool> _verboseOption;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tilemap2AnimationOptionsBinder"/> class
    /// </summary>
    /// <param name="rootCommand">The root command to bind options to</param>
    /// <param name="inputFileOption">The input file option</param>
    /// <param name="outputFileOption">The output file option</param>
    /// <param name="formatOption">The format option</param>
    /// <param name="frameDelayOption">The frame delay option</param>
    /// <param name="verboseOption">The verbose option</param>
    public Tilemap2AnimationOptionsBinder(
        Command rootCommand,
        ICommandLineOption<string> inputFileOption,
        ICommandLineOption<string?> outputFileOption,
        ICommandLineOption<string> formatOption,
        ICommandLineOption<int> frameDelayOption,
        ICommandLineOption<bool> verboseOption)
    {
        _inputFileOption = inputFileOption.Option;
        rootCommand.AddOption(_inputFileOption);
        
        _outputFileOption = outputFileOption.Option;
        rootCommand.AddOption(_outputFileOption);
        
        _formatOption = formatOption.Option;
        rootCommand.AddOption(_formatOption);
        
        _frameDelayOption = frameDelayOption.Option;
        rootCommand.AddOption(_frameDelayOption);
        
        _verboseOption = verboseOption.Option;
        rootCommand.AddOption(_verboseOption);
    }

    /// <summary>
    /// Creates a new instance of <see cref="MainWorkflowOptions"/> from command line arguments
    /// </summary>
    /// <param name="bindingContext">The binding context</param>
    /// <returns>A new instance of <see cref="MainWorkflowOptions"/></returns>
    protected override MainWorkflowOptions GetBoundValue(BindingContext bindingContext)
    {
        return new MainWorkflowOptions
        {
            InputFile = bindingContext.ParseResult.GetValueForOption(_inputFileOption)!,
            OutputFile = bindingContext.ParseResult.GetValueForOption(_outputFileOption),
            Format = bindingContext.ParseResult.GetValueForOption(_formatOption)!,
            FrameDelay = bindingContext.ParseResult.GetValueForOption(_frameDelayOption),
            Verbose = bindingContext.ParseResult.GetValueForOption(_verboseOption)
        };
    }
} 