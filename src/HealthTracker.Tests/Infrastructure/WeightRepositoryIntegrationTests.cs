namespace HealthTracker.Tests.Infrastructure;

using System;
using System.IO;
using System.Threading.Tasks;
using AwesomeAssertions;
using HealthTracker.Infrastructure;
using HealthTracker.Infrastructure.Json;
using HealthTracker.Infrastructure.Repositories;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Interfaces;
using Xunit;

public class WeightRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly WeightRepository _repo;

    public WeightRepositoryIntegrationTests()
    {
        Directory.CreateDirectory(_tempDir);
        var paths = new TempAppPaths(_tempDir);
        var store = new YearPartitionedStore<WeightEntryJson>("weight", paths);
        _repo = new WeightRepository(store);
    }

    [Fact]
    public async Task Add_ThenGetEntries_ReturnsEntry()
    {
        var ct = TestContext.Current.CancellationToken;
        var entry = new WeightEntry(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), 75m);

        await _repo.Add(entry, ct);
        var results = await _repo.GetEntries(
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            ct);

        results.Should().ContainSingle();
        results[0].WeightKg.Should().Be(75m);
    }

    [Fact]
    public async Task Update_ExistingEntry_PersistsChange()
    {
        var ct = TestContext.Current.CancellationToken;
        var entry = new WeightEntry(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), 75m);
        await _repo.Add(entry, ct);

        var updated = entry with { WeightKg = 80m };
        await _repo.Update(updated, ct);

        var results = await _repo.GetEntries(
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today),
            ct);

        results.Should().ContainSingle();
        results[0].WeightKg.Should().Be(80m);
    }

    [Fact]
    public async Task Delete_ExistingEntry_RemovesIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var entry = new WeightEntry(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), 75m);
        await _repo.Add(entry, ct);

        await _repo.Delete(entry.Id, entry.Date, ct);

        var results = await _repo.GetEntries(
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today),
            ct);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEntries_CrossYearRange_ReadsFromBothYearFiles()
    {
        var ct = TestContext.Current.CancellationToken;
        var dec31 = new DateOnly(2023, 12, 31);
        var jan1 = new DateOnly(2024, 1, 1);

        var entry1 = new WeightEntry(Guid.NewGuid(), dec31, 70m);
        var entry2 = new WeightEntry(Guid.NewGuid(), jan1, 71m);

        await _repo.Add(entry1, ct);
        await _repo.Add(entry2, ct);

        var results = await _repo.GetEntries(dec31, jan1, ct);

        results.Should().HaveCount(2);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private sealed class TempAppPaths(string dir) : IAppPaths
    {
        public string DataDirectory { get; } = dir;
    }
}
