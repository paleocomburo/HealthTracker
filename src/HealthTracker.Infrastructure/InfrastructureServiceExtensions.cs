namespace HealthTracker.Infrastructure;

using HealthTracker.Infrastructure.Json;
using HealthTracker.Infrastructure.Repositories;
using HealthTracker.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, AppPaths>();

        // Year-partitioned stores — one per metric, identified by their file prefix.
        services.AddSingleton(sp => new YearPartitionedStore<WeightEntryJson>("weight", sp.GetRequiredService<IAppPaths>()));
        services.AddSingleton(sp => new YearPartitionedStore<BloodPressureEntryJson>("bloodpressure", sp.GetRequiredService<IAppPaths>()));
        services.AddSingleton(sp => new YearPartitionedStore<BloodSugarEntryJson>("bloodsugar", sp.GetRequiredService<IAppPaths>()));

        services.AddSingleton<IWeightRepository, WeightRepository>();
        services.AddSingleton<IBloodPressureRepository, BloodPressureRepository>();
        services.AddSingleton<IBloodSugarRepository, BloodSugarRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();

        return services;
    }
}
