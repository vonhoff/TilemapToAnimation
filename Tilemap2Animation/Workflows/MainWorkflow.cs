using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Factories.Contracts;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Workflows;

public class MainWorkflow
{
    private readonly ITilemapFactory _tilemapFactory;
    private readonly ITilesetFactory _tilesetFactory;
    private readonly ITilemapService _tilemapService;
    private readonly ITilesetService _tilesetService;
    private readonly ITilesetImageService _tilesetImageService;
    private readonly IAnimationGeneratorService _animationGeneratorService;
    private readonly IAnimationEncoderService _animationEncoderService;

    public MainWorkflow(
        ITilemapFactory tilemapFactory,
        ITilesetFactory tilesetFactory,
        ITilemapService tilemapService,
        ITilesetService tilesetService,
        ITilesetImageService tilesetImageService,
        IAnimationGeneratorService animationGeneratorService,
        IAnimationEncoderService animationEncoderService)
    {
        _tilemapFactory = tilemapFactory;
        _tilesetFactory = tilesetFactory;
        _tilemapService = tilemapService;
        _tilesetService = tilesetService;
        _tilesetImageService = tilesetImageService;
        _animationGeneratorService = animationGeneratorService;
        _animationEncoderService = animationEncoderService;
    }

    public async Task ExecuteAsync(MainWorkflowOptions options)
    {
        try
        {
            Log.Information("Starting Tilemap2Animation conversion...");
            
            // First, determine the type of input file
            string inputFile = Path.GetFullPath(options.InputFile);
            string extension = Path.GetExtension(inputFile).ToLowerInvariant();
            
            string? tmxFile = null;
            string? tsxFile = null;
            string? imageFile = null;
            
            switch (extension)
            {
                case ".tmx":
                    tmxFile = inputFile;
                    break;
                case ".tsx":
                    tsxFile = inputFile;
                    break;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                    imageFile = inputFile;
                    break;
                default:
                    throw new ArgumentException($"Unsupported input file type: {extension}");
            }
            
            // Resolve all necessary files
            var resolvedFiles = await ResolveFilesAsync(tmxFile, tsxFile, imageFile);
            tmxFile = resolvedFiles.tmxFile;
            tsxFile = resolvedFiles.tsxFile;
            imageFile = resolvedFiles.imageFile;
            
            if (tmxFile == null)
            {
                throw new InvalidOperationException("Could not determine a TMX file for the conversion.");
            }
            
            if (tsxFile == null)
            {
                throw new InvalidOperationException("Could not determine a TSX file for the conversion.");
            }
            
            if (imageFile == null)
            {
                throw new InvalidOperationException("Could not determine a tileset image file for the conversion.");
            }
            
            // Parse the TMX file
            Log.Information($"Parsing TMX file: {tmxFile}");
            var tilemap = await _tilemapFactory.CreateFromTmxFileAsync(tmxFile);
            
            // Parse the TSX file
            Log.Information($"Parsing TSX file: {tsxFile}");
            var tileset = await _tilesetFactory.CreateFromTsxFileAsync(tsxFile);
            
            // Load the tileset image
            Log.Information($"Loading tileset image: {imageFile}");
            var tilesetImage = await _tilesetImageService.LoadTilesetImageAsync(imageFile);
            
            // Apply transparency processing if needed
            Log.Information("Processing tileset transparency...");
            tilesetImage = _tilesetImageService.ProcessTransparency(tilesetImage, tileset);
            
            // Parse the layer data
            if (tilemap.Layers.Count == 0)
            {
                throw new InvalidOperationException("No layers found in the TMX file.");
            }
            
            // Process all layers and store the parsed data
            var layerDataByName = new Dictionary<string, List<uint>>();
            foreach (var layer in tilemap.Layers)
            {
                Log.Information($"Parsing layer data: {layer.Name}");
                var layerData = _tilemapService.ParseLayerData(layer);
                layerDataByName[layer.Name ?? ""] = layerData;
            }
            
            // Generate animation frames
            Log.Information("Generating animation frames...");
            var (frames, delays) = await _animationGeneratorService.GenerateAnimationFramesFromLayersAsync(
                tilemap, tileset, tilesetImage, layerDataByName, options.FrameDelay);
            
            // Determine output file path
            string outputFile = options.OutputFile ?? 
                Path.ChangeExtension(inputFile, options.Format.ToLowerInvariant());
            
            // Encode and save the animation
            Log.Information($"Encoding animation to {options.Format} format: {outputFile}");
            switch (options.Format.ToLowerInvariant())
            {
                case "gif":
                    await _animationEncoderService.SaveAsGifAsync(frames, delays, outputFile);
                    break;
                case "apng":
                    await _animationEncoderService.SaveAsApngAsync(frames, delays, outputFile);
                    break;
                default:
                    throw new ArgumentException($"Unsupported output format: {options.Format}");
            }
            
            Log.Information($"Animation successfully saved to {outputFile}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during tilemap-to-animation conversion");
            throw;
        }
    }

    private async Task<(string? tmxFile, string? tsxFile, string? imageFile)> ResolveFilesAsync(
        string? initialTmxFile,
        string? initialTsxFile,
        string? initialImageFile)
    {
        string? tmxFile = initialTmxFile;
        string? tsxFile = initialTsxFile;
        string? imageFile = initialImageFile;

        // If we have a TMX file, we can get the TSX from it
        if (tmxFile != null && tsxFile == null)
        {
            var tilemap = await _tilemapService.DeserializeTmxAsync(tmxFile);
            if (tilemap.Tilesets.Count > 0)
            {
                var tilesetSource = tilemap.Tilesets.First().Source;
                if (!string.IsNullOrEmpty(tilesetSource))
                {
                    tsxFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(tmxFile)!, tilesetSource));
                }
            }
        }
        
        // If we have a TSX file, we can get the image from it
        if (tsxFile != null && imageFile == null)
        {
            var tileset = await _tilesetService.DeserializeTsxAsync(tsxFile);
            if (tileset.Image != null && !string.IsNullOrEmpty(tileset.Image.Path))
            {
                imageFile = _tilesetService.ResolveTilesetImagePath(tsxFile, tileset.Image.Path);
            }
        }
        
        // If we have an image file and no TSX, search for TSX files referencing it
        if (imageFile != null && tsxFile == null)
        {
            var tsxFiles = await _tilesetService.FindTsxFilesReferencingImageAsync(imageFile);
            if (tsxFiles.Count > 0)
            {
                tsxFile = tsxFiles.First();
            }
        }
        
        // If we have a TSX file and no TMX, search for TMX files referencing it
        if (tsxFile != null && tmxFile == null)
        {
            var tmxFiles = await _tilemapService.FindTmxFilesReferencingTsxAsync(tsxFile);
            if (tmxFiles.Count > 0)
            {
                tmxFile = tmxFiles.First();
            }
        }

        return (tmxFile, tsxFile, imageFile);
    }
} 