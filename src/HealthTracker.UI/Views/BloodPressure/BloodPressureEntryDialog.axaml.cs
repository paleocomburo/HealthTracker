namespace HealthTracker.UI.Views.BloodPressure;

using Avalonia.Controls;
using HealthTracker.UI.ViewModels.BloodPressure;

public partial class BloodPressureEntryDialog : Window
{
    public BloodPressureEntryDialog()
    {
        InitializeComponent();
    }

    public BloodPressureEntryDialog(BloodPressureEntryDialogViewModel vm) : this()
    {
        DataContext = vm;
        vm.RequestClose += (_, _) => Close();
    }
}
