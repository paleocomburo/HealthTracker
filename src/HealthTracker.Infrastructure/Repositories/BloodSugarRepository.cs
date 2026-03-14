namespace HealthTracker.Infrastructure.Repositories;

using HealthTracker.Infrastructure.Json;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Interfaces;

public class BloodSugarRepository(YearPartitionedStore<BloodSugarEntryJson> store) : IBloodSugarRepository
{
    public async Task<IReadOnlyList<BloodSugarEntry>> GetEntries(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var results = new List<BloodSugarEntry>();

        for (var year = from.Year; year <= to.Year; year++)
        {
            var raw = await store.ReadYear(year, ct);
            results.AddRange(
                raw.Select(ToDto)
                   .Where(e => e.Date >= from && e.Date <= to));
        }

        return results.OrderBy(e => e.Date).ToList();
    }

    public async Task<BloodSugarEntry> Add(BloodSugarEntry entry, CancellationToken ct = default)
    {
        var entries = await store.ReadYear(entry.Date.Year, ct);
        entries.Add(ToJson(entry));
        await store.WriteYear(entry.Date.Year, entries, ct);
        return entry;
    }

    public async Task Update(BloodSugarEntry entry, CancellationToken ct = default)
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

    private static BloodSugarEntry ToDto(BloodSugarEntryJson j) =>
        new(Guid.Parse(j.Id), DateOnly.Parse(j.Date), j.Readings.AsReadOnly(), j.Context);

    private static BloodSugarEntryJson ToJson(BloodSugarEntry e) =>
        new(e.Id.ToString(), e.Date.ToString("yyyy-MM-dd"), e.Readings.ToList(), e.Context);
}

public record BloodSugarEntryJson(string Id, string Date, List<decimal> Readings, string Context);
