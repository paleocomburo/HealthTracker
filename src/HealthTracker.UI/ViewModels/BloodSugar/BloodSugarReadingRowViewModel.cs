namespace HealthTracker.UI.ViewModels.BloodSugar;

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class BloodSugarReadingRowViewModel : ViewModelBase
{
    [ObservableProperty] private string _readingInput = "";
    [ObservableProperty] private string? _validationError;

    partial void OnReadingInputChanged(string value) => Validate();

    private void Validate()
    {
        if (ReadingInput.Length == 0)
        {
            ValidationError = null;
            return;
        }
        if (!decimal.TryParse(ReadingInput, NumberStyles.Any, CultureInfo.CurrentCulture, out var v))
        {
            ValidationError = "Enter a number.";
            return;
        }
        if (v < 0.5m || v > 30.0m)
        {
            ValidationError = "Value: 0.5–30.0 mmol/L.";
            return;
        }
        ValidationError = null;
    }

    public bool TryGetReading(out decimal value)
    {
        if (!decimal.TryParse(ReadingInput, NumberStyles.Any, CultureInfo.CurrentCulture, out value)
            || value < 0.5m || value > 30.0m)
            return false;

        return true;
    }
}
