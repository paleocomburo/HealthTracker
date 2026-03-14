namespace HealthTracker.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using HealthTracker.Services;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;
using Moq;
using Xunit;

public class BloodSugarServiceTests
{
    private static BloodSugarService CreateSut(IBloodSugarRepository repo) => new(repo);

    [Fact]
    public async Task AddEntry_ValidData_ReturnsEntry()
    {
        var repo = new Mock<IBloodSugarRepository>();
        repo.Setup(r => r.Add(It.IsAny<BloodSugarEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BloodSugarEntry e, CancellationToken _) => e);

        var sut = CreateSut(repo.Object);
        var date = DateOnly.FromDateTime(DateTime.Today);

        var result = await sut.AddEntry(date, [5.5m], TestContext.Current.CancellationToken);

        result.Context.Should().Be("fasting");
        result.Readings.Count.Should().Be(1);
    }

    [Fact]
    public async Task AddEntry_NoReadings_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodSugarRepository>().Object);

        var act = () => sut.AddEntry(DateOnly.FromDateTime(DateTime.Today), []);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_FourReadings_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodSugarRepository>().Object);

        var act = () => sut.AddEntry(DateOnly.FromDateTime(DateTime.Today), [5m, 6m, 7m, 8m]);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_ReadingBelowMinimum_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodSugarRepository>().Object);

        var act = () => sut.AddEntry(DateOnly.FromDateTime(DateTime.Today), [0.1m]);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_ReadingAboveMaximum_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodSugarRepository>().Object);

        var act = () => sut.AddEntry(DateOnly.FromDateTime(DateTime.Today), [35m]);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_FutureDate_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodSugarRepository>().Object);
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var act = () => sut.AddEntry(tomorrow, [5.5m]);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DeleteEntry_CallsRepositoryDelete()
    {
        var repo = new Mock<IBloodSugarRepository>();
        var sut = CreateSut(repo.Object);
        var id = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);

        await sut.DeleteEntry(id, date, TestContext.Current.CancellationToken);

        repo.Verify(r => r.Delete(id, date, It.IsAny<CancellationToken>()), Times.Once);
    }
}
