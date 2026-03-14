namespace HealthTracker.Infrastructure.Repositories;

using System.Text.Json;
using HealthTracker.Infrastructure.Json;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;

public class SettingsRepository(IAppPaths appPaths) : ISettingsRepository
{
    private string FilePath => Path.Combine(appPaths.DataDirectory, "settings.json");

    public async Task<AppSettings> Load(CancellationToken ct = default)
    {
        if (!File.Exists(FilePath))
            return AppSettings.Default;

        try
        {
            await using var stream = File.OpenRead(FilePath);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonConfig.Options, ct)
                   ?? AppSettings.Default;
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptException(FilePath, ex);
        }
    }

    public async Task Save(AppSettings settings, CancellationToken ct = default)
    {
        var tmpPath = FilePath + ".tmp";

        await using (var stream = File.Create(tmpPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, JsonConfig.Options, ct);
        }

        File.Move(tmpPath, FilePath, overwrite: true);
    }
}
