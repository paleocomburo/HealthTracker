using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using HealthTracker.UI.ViewModels;
using HealthTracker.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HealthTracker.UI;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = AppBootstrapper.Build();

        // Configure Serilog once DI (and therefore IAppPaths) is available.
        var appPaths = _services.GetRequiredService<Shared.Interfaces.IAppPaths>();
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                System.IO.Path.Combine(appPaths.DataDirectory, "logs", "healthtracker-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Health Tracker starting up");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Prevent duplicate validation from both Avalonia and CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            var mainVm = _services.GetRequiredService<MainWindowViewModel>();
            mainVm.Activate();   // register WeakReferenceMessenger subscriptions
            desktop.MainWindow = new MainWindow { DataContext = mainVm };

            desktop.Exit += (_, _) =>
            {
                Log.Information("Health Tracker shutting down");
                Log.CloseAndFlush();
                _services.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var toRemove = BindingPlugins.DataValidators
            .OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        foreach (var plugin in toRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}
