namespace HealthTracker.Tests.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using HealthTracker.Services;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;
using Moq;
using Xunit;

public class WeightServiceTests
{
    private static WeightService CreateSut(IWeightRepository repo) => new(repo);

    [Fact]
    public async Task AddEntry_ValidData_CallsRepositoryAndReturnsEntry()
    {
        var repo = new Mock<IWeightRepository>();
        repo.Setup(r => r.Add(It.IsAny<WeightEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeightEntry e, CancellationToken _) => e);

        var sut = CreateSut(repo.Object);
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        var result = await sut.AddEntry(date, 75.5m, TestContext.Current.CancellationToken);

        result.WeightKg.Should().Be(75.5m);
        result.Date.Should().Be(date);
        repo.Verify(r => r.Add(It.IsAny<WeightEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddEntry_ZeroWeight_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IWeightRepository>().Object);

        var act = () => sut.AddEntry(DateOnly.FromDateTime(DateTime.Today), 0m);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_NegativeWeight_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IWeightRepository>().Object);

        var act = () => sut.AddEntry(DateOnly.FromDateTime(DateTime.Today), -5m);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_WeightAtMaxLimit_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IWeightRepository>().Object);

        var act = () => sut.AddEntry(DateOnly.FromDateTime(DateTime.Today), 500m);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_FutureDate_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IWeightRepository>().Object);
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var act = () => sut.AddEntry(tomorrow, 75m);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateEntry_ValidEntry_CallsRepositoryUpdate()
    {
        var repo = new Mock<IWeightRepository>();
        var sut = CreateSut(repo.Object);
        var entry = new WeightEntry(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), 80m);

        await sut.UpdateEntry(entry, TestContext.Current.CancellationToken);

        repo.Verify(r => r.Update(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEntry_CallsRepositoryDelete()
    {
        var repo = new Mock<IWeightRepository>();
        var sut = CreateSut(repo.Object);
        var id = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);

        await sut.DeleteEntry(id, date, TestContext.Current.CancellationToken);

        repo.Verify(r => r.Delete(id, date, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLast10DateRange_TenOrMoreEntries_ReturnsRangeOfLast10()
    {
        var repo = new Mock<IWeightRepository>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var entries = Enumerable.Range(0, 15)
            .Select(i => new WeightEntry(Guid.NewGuid(), today.AddDays(-i), 75m))
            .ToList<WeightEntry>();

        repo.Setup(r => r.GetEntries(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var sut = CreateSut(repo.Object);
        var (from, to) = await sut.GetLast10DateRange(TestContext.Current.CancellationToken);

        to.Should().Be(today);
        from.Should().Be(today.AddDays(-9));
    }

    [Fact]
    public async Task GetLast10DateRange_NoEntries_ReturnsDefaultRange()
    {
        var repo = new Mock<IWeightRepository>();
        repo.Setup(r => r.GetEntries(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WeightEntry>());

        var sut = CreateSut(repo.Object);
        var (from, to) = await sut.GetLast10DateRange(TestContext.Current.CancellationToken);

        to.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        from.Should().Be(DateOnly.FromDateTime(DateTime.Today).AddMonths(-1));
    }
}
