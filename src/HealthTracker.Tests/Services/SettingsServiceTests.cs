namespace HealthTracker.Tests.Services;

using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using HealthTracker.Services;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;
using Moq;
using Xunit;

public class SettingsServiceTests
{
    [Fact]
    public async Task Load_DelegatesToRepository()
    {
        var expected = AppSettings.Default;
        var repo = new Mock<ISettingsRepository>();
        repo.Setup(r => r.Load(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var sut = new SettingsService(repo.Object);
        var result = await sut.Load(TestContext.Current.CancellationToken);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Save_AllNullThresholds_Succeeds()
    {
        var repo = new Mock<ISettingsRepository>();
        var sut = new SettingsService(repo.Object);

        await sut.Save(AppSettings.Default, TestContext.Current.CancellationToken);

        repo.Verify(r => r.Save(AppSettings.Default, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_NegativeSystolicThreshold_ThrowsValidationException()
    {
        var sut = new SettingsService(new Mock<ISettingsRepository>().Object);
        var settings = new AppSettings(null, new ThresholdSettings(-1, null, null, null, null));

        var act = () => sut.Save(settings);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Save_NegativeBloodSugarDanger_ThrowsValidationException()
    {
        var sut = new SettingsService(new Mock<ISettingsRepository>().Object);
        var settings = new AppSettings(null, new ThresholdSettings(null, null, null, -5m, null));

        var act = () => sut.Save(settings);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
