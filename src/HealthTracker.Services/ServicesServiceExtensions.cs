namespace HealthTracker.Services;

using Microsoft.Extensions.DependencyInjection;

public static class ServicesServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<WeightService>();
        services.AddSingleton<BloodPressureService>();
        services.AddSingleton<BloodSugarService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<CsvExportService>();

        return services;
    }
}
