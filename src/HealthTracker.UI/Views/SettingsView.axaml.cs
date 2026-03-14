namespace HealthTracker.UI.Views;

using Avalonia.Controls;
using HealthTracker.UI.ViewModels;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        DataContextChanged += async (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
                await vm.LoadSettingsCommand.ExecuteAsync(null);
        };
    }
}
