namespace HealthTracker.UI.Views.Weight;

using Avalonia.Controls;
using HealthTracker.UI.ViewModels.Weight;

public partial class WeightEntryDialog : Window
{
    public WeightEntryDialog()
    {
        InitializeComponent();
    }

    public WeightEntryDialog(WeightEntryDialogViewModel vm) : this()
    {
        DataContext = vm;
        vm.RequestClose += (_, _) => Close();
    }
}
