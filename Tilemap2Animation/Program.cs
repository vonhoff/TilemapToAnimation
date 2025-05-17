using System.CommandLine;
using System.CommandLine.Binding;
using Serilog;
using Tilemap2Animation.CommandLineOptions;
using Tilemap2Animation.CommandLineOptions.Binding;
using Tilemap2Animation.Workflows;

namespace Tilemap2Animation;

internal static class Program
{
    private const int ExitCodeError = 1;
    private const int ExitCodeSuccess = 0;

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Create root command
            var rootCommand = new RootCommand("A tool that converts a Tiled tilemap (TMX) and its associated tileset (TSX) into an animated sequence (GIF/APNG).");
            
            // Create options
            var inputFileOption = new InputFileOption();
            var outputFileOption = new OutputFileOption();
            var formatOption = new FormatOption();
            var frameDelayOption = new FrameDelayOption();
            var verboseOption = new VerboseOption();
            
            // Create options binder
            var optionsBinder = new Tilemap2AnimationOptionsBinder(
                rootCommand,
                inputFileOption,
                outputFileOption,
                formatOption,
                frameDelayOption,
                verboseOption);
            
            // Set up the command handler
            rootCommand.SetHandler(
                (string inputFile, string? outputFile, string format, int frameDelay, bool verbose) =>
                {
                    var options = new Tilemap2AnimationWorkflowOptions
                    {
                        InputFile = inputFile,
                        OutputFile = outputFile,
                        Format = format,
                        FrameDelay = frameDelay,
                        Verbose = verbose
                    };
                    
                    // Create startup and build application
                    var startup = new Startup(options);
                    var workflow = startup.BuildApplication();
                    
                    // Execute workflow
                    return workflow.ExecuteAsync(options);
                },
                inputFileOption.Option,
                outputFileOption.Option,
                formatOption.Option,
                frameDelayOption.Option,
                verboseOption.Option);

            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred");
            return ExitCodeError;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
} 