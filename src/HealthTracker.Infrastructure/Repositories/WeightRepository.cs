namespace HealthTracker.Infrastructure.Repositories;

using HealthTracker.Infrastructure.Json;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Interfaces;

public class WeightRepository(YearPartitionedStore<WeightEntryJson> store) : IWeightRepository
{
    public async Task<IReadOnlyList<WeightEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var results = new List<WeightEntry>();

        for (var year = from.Year; year <= to.Year; year++)
        {
            var raw = await store.ReadYear(year, ct);
            results.AddRange(
                raw.Select(ToDto)
                   .Where(e => e.Date >= from && e.Date <= to));
        }

        return results.OrderBy(e => e.Date).ToList();
    }

    public async Task<WeightEntry> Add(WeightEntry entry, CancellationToken ct = default)
    {
        var entries = await store.ReadYear(entry.Date.Year, ct);
        entries.Add(ToJson(entry));
        await store.WriteYear(entry.Date.Year, entries, ct);
        return entry;
    }

    public async Task Update(WeightEntry entry, CancellationToken ct = default)
    {
        var entries = await store.ReadYear(entry.Date.Year, ct);
        var idx = entries.FindIndex(e => e.Id == entry.Id.ToString());
        if (idx >= 0)
            entries[idx] = ToJson(entry);
        await store.WriteYear(entry.Date.Year, entries, ct);
    }

    public async Task Delete(Guid id, DateOnly entryDate, CancellationToken ct = default)
    {
        var entries = await store.ReadYear(entryDate.Year, ct);
        entries.RemoveAll(e => e.Id == id.ToString());
        await store.WriteYear(entryDate.Year, entries, ct);
    }

    private static WeightEntry ToDto(WeightEntryJson j) =>
        new(Guid.Parse(j.Id), DateOnly.Parse(j.Date), j.WeightKg);

    private static WeightEntryJson ToJson(WeightEntry e) =>
        new(e.Id.ToString(), e.Date.ToString("yyyy-MM-dd"), e.WeightKg);
}

// JSON model that mirrors the on-disk schema exactly.
// Kept separate from the domain DTO so JSON field names and domain names can evolve independently.
public record WeightEntryJson(string Id, string Date, decimal WeightKg);
