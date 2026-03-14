namespace HealthTracker.UI;

using HealthTracker.Infrastructure;
using HealthTracker.Services;
using HealthTracker.Shared.Interfaces;
using HealthTracker.UI.Services;
using HealthTracker.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

public static class AppBootstrapper
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure();
        services.AddServices();

        // UI-layer services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<NotificationViewModel>();

        // View models — registered as singletons so navigation reuses the same instance.
        // WeightViewModel.Init() wires up the filter RangeChanged subscription after construction.
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>(sp =>
            ActivatorUtilities.CreateInstance<DashboardViewModel>(sp).Init());
        services.AddSingleton<WeightViewModel>(sp =>
            ActivatorUtilities.CreateInstance<WeightViewModel>(sp).Init());
        services.AddSingleton<BloodPressureViewModel>(sp =>
            ActivatorUtilities.CreateInstance<BloodPressureViewModel>(sp).Init());
        services.AddSingleton<BloodSugarViewModel>(sp =>
            ActivatorUtilities.CreateInstance<BloodSugarViewModel>(sp).Init());
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
