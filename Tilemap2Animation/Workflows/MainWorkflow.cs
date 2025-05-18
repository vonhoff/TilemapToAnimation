using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Workflows;

public class MainWorkflow
{
    private readonly ITilemapService _tilemapService;
    private readonly ITilesetService _tilesetService;
    private readonly ITilesetImageService _tilesetImageService;
    private readonly IAnimationGeneratorService _animationGeneratorService;
    private readonly IAnimationEncoderService _animationEncoderService;

    public MainWorkflow(
        ITilemapService tilemapService,
        ITilesetService tilesetService,
        ITilesetImageService tilesetImageService,
        IAnimationGeneratorService animationGeneratorService,
        IAnimationEncoderService animationEncoderService)
    {
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

            var inputFile = Path.GetFullPath(options.InputFile);
            var extension = Path.GetExtension(inputFile).ToLowerInvariant();
            var fileName = Path.GetFileName(inputFile);
            var supportedExtensions = new[] { ".tmx", ".tsx", ".png", ".jpg", ".jpeg", ".bmp" };

            string? tmxInputFile = null;
            string? tsxInputFile = null;
            string? imageInputFile = null;

            // Find the last supported extension in the filename
            var lastSupportedExtension = supportedExtensions
                .Where(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(ext => fileName.LastIndexOf(ext, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (lastSupportedExtension == null)
            {
                throw new ArgumentException($"Unsupported input file type. File must end with one of: {string.Join(", ", supportedExtensions)}");
            }

            switch (lastSupportedExtension)
            {
                case ".tmx":
                    tmxInputFile = inputFile;
                    break;
                case ".tsx":
                    tsxInputFile = inputFile;
                    break;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                    imageInputFile = inputFile;
                    break;
            }

            Tilemap? tilemap = null;
            string? resolvedTmxFilePath = tmxInputFile; // This will hold the path to the TMX file, once determined

            // 1. Determine and load the Tilemap object and the resolved TMX file path
            if (tmxInputFile != null)
            {
                Log.Information("Parsing TMX file: {TmxFile}", tmxInputFile);
                tilemap = await _tilemapService.DeserializeTmxAsync(tmxInputFile);
                resolvedTmxFilePath = tmxInputFile;
            }
            else if (tsxInputFile != null)
            {
                Log.Information("Initial input is a TSX file: {TsxFile}", tsxInputFile);
                var tmxFilesFound = await _tilemapService.FindTmxFilesReferencingTsxAsync(tsxInputFile);
                if (tmxFilesFound.Count > 0)
                {
                    resolvedTmxFilePath = tmxFilesFound.First();
                    Log.Information("Found TMX file referencing this tileset: {TmxFile}", resolvedTmxFilePath);
                    tilemap = await _tilemapService.DeserializeTmxAsync(resolvedTmxFilePath);
                }
                else
                {
                    Log.Warning("No TMX file found referencing TSX file {TsxInputFile}. Will proceed with TSX only if no TMX can be resolved.", tsxInputFile);
                }
            }
            else if (imageInputFile != null)
            {
                Log.Information("Initial input is an image file: {ImageFile}", imageInputFile);
                var tsxFilesFoundForImage = await _tilesetService.FindTsxFilesReferencingImageAsync(imageInputFile);
                if (tsxFilesFoundForImage.Count > 0)
                {
                    var firstTsx = tsxFilesFoundForImage.First();
                    Log.Information("Found TSX file referencing this image: {TsxFile}", firstTsx);
                    tsxInputFile = firstTsx; // Set tsxInputFile to use in the next step if needed

                    var tmxFilesFoundForTsx = await _tilemapService.FindTmxFilesReferencingTsxAsync(firstTsx);
                    if (tmxFilesFoundForTsx.Count > 0)
                    {
                        resolvedTmxFilePath = tmxFilesFoundForTsx.First();
                        Log.Information("Found TMX file referencing this tileset: {TmxFile}", resolvedTmxFilePath);
                        tilemap = await _tilemapService.DeserializeTmxAsync(resolvedTmxFilePath);
                    }
                    else
                    {
                         Log.Warning("No TMX file found referencing TSX file {TsxInputFile} (derived from image). Will proceed with TSX only.", tsxInputFile);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"No TSX file found referencing the input image: {imageInputFile}");
                }
            }

            // 2. Load all tilesets based on the determined Tilemap or fallback to a single TSX
            var tilesets = new Dictionary<string, Tileset>();
            var tilesetImages = new Dictionary<string, Image<Rgba32>>();

            if (tilemap != null && resolvedTmxFilePath != null)
            {
                Log.Information("Loading all tilesets referenced in TMX file: {TmxFile}", resolvedTmxFilePath);
                var tmxDirectory = Path.GetDirectoryName(resolvedTmxFilePath) ?? ".";
                foreach (var tilemapTilesetEntry in tilemap.Tilesets)
                {
                    if (string.IsNullOrEmpty(tilemapTilesetEntry.Source))
                    {
                        Log.Warning("Tileset with firstgid {FirstGid} in TMX has no source defined, skipping.", tilemapTilesetEntry.FirstGid);
                        continue;
                    }

                    var tsxFilePath = Path.GetFullPath(Path.Combine(tmxDirectory, tilemapTilesetEntry.Source));
                    if (!tilesets.ContainsKey(tsxFilePath)) // Avoid reloading if somehow already processed
                    {
                        Log.Information("Loading tileset from {TsxFile} (referenced in TMX)", tsxFilePath);
                        var tileset = await _tilesetService.DeserializeTsxAsync(tsxFilePath);
                        
                        if (tileset.Image != null && !string.IsNullOrEmpty(tileset.Image.Path))
                        {
                            var imagePath = _tilesetService.ResolveTilesetImagePath(tsxFilePath, tileset.Image.Path);
                            Log.Information("Loading tileset image: {ImageFile}", imagePath);
                            var tilesetImage = await _tilesetImageService.LoadTilesetImageAsync(imagePath);
                            tilesetImage = _tilesetImageService.ProcessTransparency(tilesetImage, tileset);
                            
                            tilesets[tsxFilePath] = tileset;
                            tilesetImages[tsxFilePath] = tilesetImage;
                        }
                        else
                        {
                            Log.Warning("Tileset {TsxFile} has no image defined, skipping.", tsxFilePath);
                        }
                    }
                }
            }
            else if (tsxInputFile != null) // Fallback: No TMX resolved, but we started with a TSX
            {
                Log.Information("Proceeding with single TSX file: {TsxFile} as no TMX was resolved.", tsxInputFile);
                if (!tilesets.ContainsKey(tsxInputFile))
                {
                    var tileset = await _tilesetService.DeserializeTsxAsync(tsxInputFile);
                    if (tileset.Image != null && !string.IsNullOrEmpty(tileset.Image.Path))
                    {
                        var imagePath = _tilesetService.ResolveTilesetImagePath(tsxInputFile, tileset.Image.Path);
                        Log.Information("Loading tileset image for single TSX: {ImageFile}", imagePath);
                        var tilesetImage = await _tilesetImageService.LoadTilesetImageAsync(imagePath);
                        tilesetImage = _tilesetImageService.ProcessTransparency(tilesetImage, tileset);

                        tilesets[tsxInputFile] = tileset;
                        tilesetImages[tsxInputFile] = tilesetImage;
                        
                        // Create a dummy tilemap if none exists for single TSX processing
                        if(tilemap == null)
                        {
                            tilemap = new Tilemap 
                            { 
                                Layers = new List<TilemapLayer> { new TilemapLayer { Name = "Default Layer", Data = new TilemapLayerData { Text = "0" } } }, // Needs actual data if to be used
                                Tilesets = new List<TilemapTileset> { new TilemapTileset { FirstGid = 1, Source = Path.GetFileName(tsxInputFile) } } 
                            };
                             Log.Warning("Created a minimal tilemap for single TSX processing as no TMX was found.");
                            // Note: This dummy tilemap might not have correct width/height/tilewidth/tileheight
                            // and layer data needs to be handled carefully.
                            // For now, this allows the structure to pass through.
                            // A better approach for "TSX-only" would be a different workflow.
                            resolvedTmxFilePath = Path.GetDirectoryName(tsxInputFile); // For path resolution
                        }
                    }
                    else
                    {
                         Log.Warning("Single TSX file {TsxFile} has no image defined.", tsxInputFile);
                    }
                }
            }
            
            // 3. Validations
            if (tilemap == null)
            {
                throw new InvalidOperationException("No TMX file could be determined or loaded for the conversion.");
            }
            if (tilesets.Count == 0)
            {
                throw new InvalidOperationException("No tilesets could be loaded for the conversion.");
            }
            if (resolvedTmxFilePath == null)
            {
                 // This should ideally be caught by tilemap == null, but as a safeguard for Path.Combine later.
                throw new InvalidOperationException("Resolved TMX file path is null, cannot proceed.");
            }


            // 4. Process layers
            var layerDataByName = new Dictionary<string, List<uint>>();
            if (tilemap.Layers.Count == 0 && tsxInputFile != null && tilemap.Tilesets.Count == 1 && tilemap.Layers.FirstOrDefault()?.Name == "Default Layer")
            {
                Log.Warning("TMX has no layers, and processing was based on a single TSX. Animation might be empty or incorrect.");
                // Potentially create a default layer spanning the size of the output based on image?
                // For now, this will likely result in an empty animation if layerDataByName remains empty.
            }

            foreach (var layer in tilemap.Layers)
            {
                Log.Information("Parsing layer data: {LayerName}", layer.Name);
                var layerData = _tilemapService.ParseLayerData(layer);
                layerDataByName[layer.Name ?? $"UnnamedLayer_{layer.Id}"] = layerData;
            }
            
            if (layerDataByName.Count == 0 && tilemap.Layers.Count > 0) {
                Log.Warning("Parsed layer data resulted in an empty dictionary, though TMX layers exist. Check layer data parsing logic and TMX content.");
            }
             if (layerDataByName.Count == 0 && tilemap.Layers.Count == 0) {
                Log.Error("No layers found in TMX and no layer data could be parsed. Cannot generate animation.");
                throw new InvalidOperationException("No layer data available to generate animation.");
            }


            // 5. Generate animation frames
            Log.Information("Generating animation frames...");

            var tilesetEntriesForGenerator = new List<(int FirstGid, Tileset? Tileset, Image<Rgba32>? TilesetImage)>();
            var tmxDir = Path.GetDirectoryName(resolvedTmxFilePath) ?? ".";

            foreach (var entry in tilemap.Tilesets)
            {
                if (string.IsNullOrEmpty(entry.Source)) continue;
                var tsxFullPath = Path.GetFullPath(Path.Combine(tmxDir, entry.Source)); 
                if (tilesets.TryGetValue(tsxFullPath, out var ts) && tilesetImages.TryGetValue(tsxFullPath, out var img))
                {
                    tilesetEntriesForGenerator.Add((entry.FirstGid, ts, img));
                }
                else
                {
                    Log.Warning("Tileset source {TilesetSource} from TMX not found in loaded tilesets. FirstGid: {FirstGid}", entry.Source, entry.FirstGid);
                }
            }
            
            if (tilesetEntriesForGenerator.Count == 0 && tilesets.Count > 0)
            {
                // This can happen if TMX has no tileset entries but we loaded a single TSX.
                // Attempt to add the single loaded TSX if it's the case.
                if (tilesets.Count == 1 && tilesetImages.Count == 1)
                {
                    var singleTsx = tilesets.First();
                    var singleImg = tilesetImages.First();
                    // Find a suitable FirstGid, default to 1 if TMX had no tilesets.
                    var firstGid = tilemap.Tilesets.FirstOrDefault()?.FirstGid ?? 1;
                    tilesetEntriesForGenerator.Add((firstGid, singleTsx.Value, singleImg.Value));
                    Log.Information("Added single loaded tileset to generator as TMX had no explicit entries.");
                }
                else
                {
                    Log.Error("No valid tileset entries could be prepared for the animation generator, but tilesets were loaded. Check TMX tileset references.");
                    throw new InvalidOperationException("Could not prepare tileset entries for animation generator.");
                }
            }

            var (frames, delays) = await _animationGeneratorService.GenerateAnimationFramesFromMultipleTilesetsAsync(
                tilemap,
                tilesetEntriesForGenerator,
                layerDataByName);

            // Determine output file path
            var outputFilePath = options.OutputFile ?? Path.ChangeExtension(inputFile, ".gif");
            if (!outputFilePath.ToLowerInvariant().EndsWith(".gif"))
            {
                outputFilePath = Path.ChangeExtension(outputFilePath, ".gif");
            }

            // Encode and save the animation
            Log.Information("Encoding animation to GIF format: {OutputFile}", outputFilePath);
            await _animationEncoderService.SaveAsGifAsync(frames, delays, outputFilePath);

            Log.Information("Animation successfully saved to {OutputFile}", outputFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during tilemap-to-animation conversion");
            throw;
        }
    }

    // ResolveFilesAsync method is no longer needed as its logic is integrated above.
    // private async Task<(string? tmxFile, string? tsxFile, string? imageFile)> ResolveFilesAsync(...)
} 