namespace HealthTracker.Tests.Infrastructure;

using System;
using System.IO;
using System.Threading.Tasks;
using AwesomeAssertions;
using HealthTracker.Infrastructure;
using HealthTracker.Infrastructure.Json;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;
using Xunit;

public class YearPartitionedStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly YearPartitionedStore<TestRecord> _store;

    public YearPartitionedStoreTests()
    {
        Directory.CreateDirectory(_tempDir);
        var paths = new TempAppPaths(_tempDir);
        _store = new YearPartitionedStore<TestRecord>("test", paths);
    }

    [Fact]
    public async Task WriteYear_ThenReadYear_ReturnsOriginalData()
    {
        var ct = TestContext.Current.CancellationToken;
        var records = new System.Collections.Generic.List<TestRecord>
        {
            new("alpha", 42),
            new("beta", 99)
        };

        await _store.WriteYear(2024, records, ct);
        var result = await _store.ReadYear(2024, ct);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("alpha");
        result[1].Value.Should().Be(99);
    }

    [Fact]
    public async Task ReadYear_FileDoesNotExist_ReturnsEmptyList()
    {
        var result = await _store.ReadYear(1900, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task WriteYear_AtomicWrite_NoTmpFileLeftBehind()
    {
        await _store.WriteYear(2025, [new TestRecord("x", 1)], TestContext.Current.CancellationToken);

        var tmpFiles = Directory.GetFiles(_tempDir, "*.tmp");
        tmpFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadYear_CorruptFile_ThrowsDataFileCorruptException()
    {
        var filePath = Path.Combine(_tempDir, "test_2023.json");
        await File.WriteAllTextAsync(filePath, "{ this is not valid json !!! }", TestContext.Current.CancellationToken);

        var act = () => _store.ReadYear(2023, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DataFileCorruptException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private record TestRecord(string Name, int Value);

    private sealed class TempAppPaths(string dir) : IAppPaths
    {
        public string DataDirectory { get; } = dir;
    }
}
