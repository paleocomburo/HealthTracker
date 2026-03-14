namespace HealthTracker.UI.ViewModels.BloodPressure;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class BpReadingRowViewModel : ViewModelBase
{
    [ObservableProperty] private string _systolicInput = "";
    [ObservableProperty] private string _diastolicInput = "";
    [ObservableProperty] private string? _validationError;

    partial void OnSystolicInputChanged(string value) => Validate();
    partial void OnDiastolicInputChanged(string value) => Validate();

    private void Validate()
    {
        if (SystolicInput.Length > 0 && !int.TryParse(SystolicInput, out _))
        {
            ValidationError = "Systolic must be a whole number.";
            return;
        }
        if (int.TryParse(SystolicInput, out var s) && (s < 60 || s > 250))
        {
            ValidationError = "Systolic: 60–250 mmHg.";
            return;
        }
        if (DiastolicInput.Length > 0 && !int.TryParse(DiastolicInput, out _))
        {
            ValidationError = "Diastolic must be a whole number.";
            return;
        }
        if (int.TryParse(DiastolicInput, out var d) && (d < 40 || d > 150))
        {
            ValidationError = "Diastolic: 40–150 mmHg.";
            return;
        }
        ValidationError = null;
    }

    public bool TryGetReading(out int systolic, out int diastolic)
    {
        systolic = 0;
        diastolic = 0;

        if (!int.TryParse(SystolicInput, out systolic) || systolic < 60 || systolic > 250)
            return false;

        if (!int.TryParse(DiastolicInput, out diastolic) || diastolic < 40 || diastolic > 150)
            return false;

        return true;
    }
}
