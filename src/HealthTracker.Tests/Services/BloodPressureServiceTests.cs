namespace HealthTracker.Tests.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using HealthTracker.Services;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Enums;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;
using Moq;
using Xunit;

public class BloodPressureServiceTests
{
    private static BloodPressureService CreateSut(IBloodPressureRepository repo) => new(repo);

    private static IReadOnlyList<BloodPressureReading> OneValidReading =>
        [new BloodPressureReading(120, 80)];

    [Fact]
    public async Task AddEntry_ValidData_ReturnsEntry()
    {
        var repo = new Mock<IBloodPressureRepository>();
        repo.Setup(r => r.Add(It.IsAny<BloodPressureEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BloodPressureEntry e, CancellationToken _) => e);

        var sut = CreateSut(repo.Object);
        var date = DateOnly.FromDateTime(DateTime.Today);

        var result = await sut.AddEntry(date, TimeOfDay.Morning, OneValidReading, TestContext.Current.CancellationToken);

        result.Readings.Count.Should().Be(1);
        result.TimeOfDay.Should().Be(TimeOfDay.Morning);
    }

    [Fact]
    public async Task AddEntry_NoReadings_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodPressureRepository>().Object);
        var date = DateOnly.FromDateTime(DateTime.Today);

        var act = () => sut.AddEntry(date, TimeOfDay.Morning, []);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_SixReadings_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodPressureRepository>().Object);
        var date = DateOnly.FromDateTime(DateTime.Today);
        var readings = Enumerable.Repeat(new BloodPressureReading(120, 80), 6).ToList();

        var act = () => sut.AddEntry(date, TimeOfDay.Morning, readings);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_SystolicTooLow_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodPressureRepository>().Object);
        var date = DateOnly.FromDateTime(DateTime.Today);
        IReadOnlyList<BloodPressureReading> readings = [new BloodPressureReading(50, 80)];

        var act = () => sut.AddEntry(date, TimeOfDay.Morning, readings);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddEntry_FutureDate_ThrowsValidationException()
    {
        var sut = CreateSut(new Mock<IBloodPressureRepository>().Object);
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var act = () => sut.AddEntry(tomorrow, TimeOfDay.Morning, OneValidReading);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DeleteEntry_CallsRepositoryDelete()
    {
        var repo = new Mock<IBloodPressureRepository>();
        var sut = CreateSut(repo.Object);
        var id = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);

        await sut.DeleteEntry(id, date, TestContext.Current.CancellationToken);

        repo.Verify(r => r.Delete(id, date, It.IsAny<CancellationToken>()), Times.Once);
    }
}
