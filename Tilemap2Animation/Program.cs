using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using Tilemap2Animation.CommandLineOptions;
using Tilemap2Animation.CommandLineOptions.Binding;

namespace Tilemap2Animation;

public static class Program
{
    private const string Description =
        """
        Tilemap2Animation is a command-line tool that converts a Tiled tilemap (TMX) into an animated GIF.
        See: https://github.com/vonhoff/Tilemap2Animation for more information.
        """;

    public static async Task Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        var rootCommand = new RootCommand(Description);
        var optionsBinder = BuildMainWorkflowOptionsBinder(rootCommand);

        rootCommand.SetHandler(options =>
        {
            var startup = new Startup(options);
            var workflow = startup.BuildApplication();
            return workflow.ExecuteAsync(options);
        }, optionsBinder);

        var parser = new CommandLineBuilder(rootCommand)
            .UseHelp("--help", "-?", "/?")
            .UseEnvironmentVariableDirective()
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseParseErrorReporting()
            .UseExceptionHandler()
            .CancelOnProcessTermination()
            .Build();

        await parser.InvokeAsync(args);
    }

    private static Tilemap2AnimationOptionsBinder BuildMainWorkflowOptionsBinder(Command rootCommand)
    {
        var inputFileOption = new InputFileOption();
        var outputFileOption = new OutputFileOption();
        var fpsOption = new FpsOption();
        var verboseOption = new VerboseOption();

        return new Tilemap2AnimationOptionsBinder(
            rootCommand,
            inputFileOption,
            outputFileOption,
            fpsOption,
            verboseOption);
    }
} 