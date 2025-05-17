using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using Serilog;
using Tilemap2Animation.CommandLineOptions;
using Tilemap2Animation.CommandLineOptions.Binding;
using Tilemap2Animation.Workflows;

namespace Tilemap2Animation;

public static class Program
{
    private const string Description =
        """
        Tilemap2Animation is a command-line tool that converts a Tiled tilemap (TMX) and its associated tileset (TSX) into an animated sequence (GIF/WEBP/APNG).
        See: https://github.com/vonhoff/Tilemap2Animation for more information.
        """;

    public static async Task Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
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
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static Tilemap2AnimationOptionsBinder BuildMainWorkflowOptionsBinder(Command rootCommand)
    {
        var inputFileOption = new InputFileOption();
        var outputFileOption = new OutputFileOption();
        var formatOption = new FormatOption();
        var frameDelayOption = new FrameDelayOption();
        var verboseOption = new VerboseOption();

        return new Tilemap2AnimationOptionsBinder(
            rootCommand,
            inputFileOption,
            outputFileOption,
            formatOption,
            frameDelayOption,
            verboseOption);
    }
} 