namespace HealthTracker.Infrastructure.Json;

using System.Text.Json;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;

/// <summary>
/// The single file I/O primitive for all metric data.
/// All reads/writes go through here — atomic writes and error handling are implemented once.
/// </summary>
public class YearPartitionedStore<T>(string prefix, IAppPaths appPaths)
{
    private string FilePath(int year) =>
        Path.Combine(appPaths.DataDirectory, $"{prefix}_{year}.json");

    public async Task<List<T>> ReadYear(int year, CancellationToken ct = default)
    {
        var path = FilePath(year);

        if (!File.Exists(path))
            return [];

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonConfig.Options, ct)
                   ?? [];
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptException(path, ex);
        }
    }

    public async Task WriteYear(int year, List<T> entries, CancellationToken ct = default)
    {
        var finalPath = FilePath(year);
        var tmpPath = finalPath + ".tmp";

        // Write to .tmp first so a crash during write never corrupts the live file.
        await using (var stream = File.Create(tmpPath))
        {
            await JsonSerializer.SerializeAsync(stream, entries, JsonConfig.Options, ct);
        }

        File.Move(tmpPath, finalPath, overwrite: true);
    }
}
