namespace HealthTracker.UI.Views.BloodSugar;

using Avalonia.Controls;
using HealthTracker.UI.ViewModels.BloodSugar;

public partial class BloodSugarEntryDialog : Window
{
    public BloodSugarEntryDialog()
    {
        InitializeComponent();
    }

    public BloodSugarEntryDialog(BloodSugarEntryDialogViewModel vm) : this()
    {
        DataContext = vm;
        vm.RequestClose += (_, _) => Close();
    }
}
