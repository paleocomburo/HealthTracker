namespace HealthTracker.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HealthTracker.UI.Messages;

public partial class MainWindowViewModel(
    DashboardViewModel dashboard,
    WeightViewModel weight,
    BloodPressureViewModel bloodPressure,
    BloodSugarViewModel bloodSugar,
    SettingsViewModel settings,
    NotificationViewModel notifications) : ViewModelBase,
    IRecipient<NavigateMessage>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsWeightActive))]
    [NotifyPropertyChangedFor(nameof(IsBloodPressureActive))]
    [NotifyPropertyChangedFor(nameof(IsBloodSugarActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    private ViewModelBase _currentView = dashboard;

    public bool IsDashboardActive => CurrentView is DashboardViewModel;
    public bool IsWeightActive => CurrentView is WeightViewModel;
    public bool IsBloodPressureActive => CurrentView is BloodPressureViewModel;
    public bool IsBloodSugarActive => CurrentView is BloodSugarViewModel;
    public bool IsSettingsActive => CurrentView is SettingsViewModel;

    public NotificationViewModel Notifications { get; } = notifications;

    public void Activate() =>
        WeakReferenceMessenger.Default.RegisterAll(this);

    // Called by Dashboard's "Go to" buttons via the messenger.
    public void Receive(NavigateMessage message)
    {
        CurrentView = message.Destination switch
        {
            "Weight"        => weight,
            "BloodPressure" => bloodPressure,
            "BloodSugar"    => bloodSugar,
            "Dashboard"     => dashboard,
            "Settings"      => settings,
            _               => CurrentView
        };
    }

    [RelayCommand]
    private void NavigateToDashboard() => CurrentView = dashboard;

    [RelayCommand]
    private void NavigateToWeight() => CurrentView = weight;

    [RelayCommand]
    private void NavigateToBloodPressure() => CurrentView = bloodPressure;

    [RelayCommand]
    private void NavigateToBloodSugar() => CurrentView = bloodSugar;

    [RelayCommand]
    private void NavigateToSettings() => CurrentView = settings;
}
