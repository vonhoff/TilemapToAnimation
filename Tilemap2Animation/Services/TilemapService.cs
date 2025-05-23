using System.IO.Compression;
using System.Xml.Serialization;
using Serilog;
using Tilemap2Animation.Entities;
using Tilemap2Animation.Services.Contracts;

namespace Tilemap2Animation.Services;

public class TilemapService : ITilemapService
{
    public async Task<Tilemap> DeserializeTmxAsync(string tmxFilePath)
    {
        if (!File.Exists(tmxFilePath)) throw new FileNotFoundException($"TMX file not found: {tmxFilePath}");

        try
        {
            await using var fileStream = new FileStream(tmxFilePath, FileMode.Open, FileAccess.Read);
            var serializer = new XmlSerializer(typeof(Tilemap));
            var tilemap = await Task.Run(() => (Tilemap?)serializer.Deserialize(fileStream));

            if (tilemap == null) throw new InvalidOperationException("Failed to deserialize TMX file.");

            return tilemap;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deserializing TMX file: {TmxFilePath}", tmxFilePath);
            throw new InvalidOperationException($"Error deserializing TMX file: {ex.Message}", ex);
        }
    }

    public List<uint> ParseLayerData(TilemapLayer? layer)
    {
        if (layer?.Data == null || string.IsNullOrEmpty(layer.Data.Text))
            throw new InvalidOperationException("Layer data is missing or empty.");

        try
        {
            var encoding = layer.Data.Encoding ?? "csv";
            var compression = layer.Data.Compression ?? "";

            switch (encoding.ToLowerInvariant())
            {
                case "csv":
                    return ParseCsvData(layer.Data.Text);
                case "base64":
                    return ParseBase64Data(layer.Data.Text, compression);
                default:
                    throw new NotSupportedException($"Unsupported layer data encoding: {encoding}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error parsing layer data");
            throw new InvalidOperationException($"Error parsing layer data: {ex.Message}", ex);
        }
    }

    public async Task<List<string>> FindTmxFilesReferencingTsxAsync(string tsxFilePath)
    {
        if (string.IsNullOrEmpty(tsxFilePath))
            throw new ArgumentException("TSX file path cannot be null or empty.", nameof(tsxFilePath));

        try
        {
            var directory = Path.GetDirectoryName(tsxFilePath) ?? ".";
            var tsxFileName = Path.GetFileName(tsxFilePath);
            var tmxFiles = new List<string>();

            // Search for TMX files in the directory and its subdirectories
            var tmxFilesInDirectory =
                await Task.Run(() => Directory.GetFiles(directory, "*.tmx", SearchOption.AllDirectories));

            foreach (var tmxFile in tmxFilesInDirectory)
                try
                {
                    await using var fileStream = new FileStream(tmxFile, FileMode.Open, FileAccess.Read);
                    var serializer = new XmlSerializer(typeof(Tilemap));
                    var tilemap = await Task.Run(() => (Tilemap?)serializer.Deserialize(fileStream));

                    if (tilemap?.Tilesets != null)
                        // Check if any tileset in the TMX references the TSX file
                        if (tilemap.Tilesets.Any(t => t.Source != null &&
                                                      Path.GetFileName(t.Source).Equals(tsxFileName,
                                                          StringComparison.OrdinalIgnoreCase)))
                            tmxFiles.Add(tmxFile);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error reading TMX file: {TmxFile}", tmxFile);
                    // Continue with the next file
                }

            return tmxFiles;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error searching for TMX files referencing TSX: {TsxFilePath}", tsxFilePath);
            throw new InvalidOperationException($"Error searching for TMX files: {ex.Message}", ex);
        }
    }

    private List<uint> ParseCsvData(string data)
    {
        var result = new List<uint>();

        // Remove whitespace and split by commas
        var values = data.Replace("\n", "").Replace("\r", "").Replace(" ", "").Split(',');

        foreach (var value in values)
            if (!string.IsNullOrEmpty(value) && uint.TryParse(value, out var gid))
                result.Add(gid);

        return result;
    }

    private List<uint> ParseBase64Data(string data, string compression)
    {
        var result = new List<uint>();

        // Remove whitespace
        data = data.Replace("\n", "").Replace("\r", "").Replace(" ", "");

        // Decode base64
        var bytes = Convert.FromBase64String(data);

        // Decompress if needed
        bytes = compression.ToLowerInvariant() switch
        {
            "gzip" => DecompressGZip(bytes),
            "zlib" => DecompressZLib(bytes),
            "" => bytes,
            _ => throw new NotSupportedException($"Unsupported compression format: {compression}")
        };

        // Parse GIDs (each GID is a 32-bit unsigned integer in little-endian format)
        for (var i = 0; i < bytes.Length; i += 4)
        {
            var gid = BitConverter.ToUInt32(bytes, i);
            result.Add(gid);
        }

        return result;
    }

    private byte[] DecompressGZip(byte[] compressedData)
    {
        using var compressedStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();

        gzipStream.CopyTo(resultStream);
        return resultStream.ToArray();
    }

    private byte[] DecompressZLib(byte[] compressedData)
    {
        // Skip the ZLib header (2 bytes)
        var zlibData = new byte[compressedData.Length - 2];
        Array.Copy(compressedData, 2, zlibData, 0, zlibData.Length);

        using var compressedStream = new MemoryStream(zlibData);
        using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();

        deflateStream.CopyTo(resultStream);
        return resultStream.ToArray();
    }
}