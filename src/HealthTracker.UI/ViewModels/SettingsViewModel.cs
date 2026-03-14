namespace HealthTracker.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthTracker.Services;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Exceptions;
using Serilog;

public partial class SettingsViewModel(SettingsService settingsService) : ViewModelBase
{
    [ObservableProperty] private string _targetWeightKg = "";
    [ObservableProperty] private string _bpSystolicUpper = "";
    [ObservableProperty] private string _bpDiastolicUpper = "";
    [ObservableProperty] private string _bloodSugarWarning = "";
    [ObservableProperty] private string _bloodSugarDanger = "";
    [ObservableProperty] private string _bloodSugarLower = "";
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isLoading;

    [RelayCommand]
    private async System.Threading.Tasks.Task LoadSettings(System.Threading.CancellationToken ct)
    {
        IsLoading = true;
        StatusMessage = null;

        try
        {
            var settings = await settingsService.Load(ct);
            TargetWeightKg = settings.TargetWeightKg?.ToString() ?? "";
            BpSystolicUpper = settings.Thresholds.BpSystolicUpperMmhg?.ToString() ?? "";
            BpDiastolicUpper = settings.Thresholds.BpDiastolicUpperMmhg?.ToString() ?? "";
            BloodSugarWarning = settings.Thresholds.BloodSugarWarningMmolL?.ToString() ?? "";
            BloodSugarDanger = settings.Thresholds.BloodSugarDangerMmolL?.ToString() ?? "";
            BloodSugarLower = settings.Thresholds.BloodSugarLowerMmolL?.ToString() ?? "";
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Failed to load settings");
            StatusMessage = "Could not load settings.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task Save(System.Threading.CancellationToken ct)
    {
        var settings = BuildSettings();
        if (settings is null) return;

        try
        {
            await settingsService.Save(settings, ct);
            StatusMessage = "Settings saved.";
            Log.Information("Settings saved successfully");
        }
        catch (ValidationException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Failed to save settings");
            StatusMessage = "Could not save settings.";
        }
    }

    private AppSettings? BuildSettings()
    {
        decimal? targetWeight = ParseOptionalDecimal(TargetWeightKg, "Target weight");
        int? systolicUpper = ParseOptionalInt(BpSystolicUpper, "Systolic threshold");
        int? diastolicUpper = ParseOptionalInt(BpDiastolicUpper, "Diastolic threshold");
        decimal? bsWarning = ParseOptionalDecimal(BloodSugarWarning, "Blood sugar warning");
        decimal? bsDanger = ParseOptionalDecimal(BloodSugarDanger, "Blood sugar danger");
        decimal? bsLower = ParseOptionalDecimal(BloodSugarLower, "Blood sugar lower limit");

        if (StatusMessage is not null)
            return null;

        return new AppSettings(targetWeight, new ThresholdSettings(
            systolicUpper, diastolicUpper, bsWarning, bsDanger, bsLower));
    }

    private decimal? ParseOptionalDecimal(string input, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (!decimal.TryParse(input, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out var value))
        {
            StatusMessage = $"{fieldName}: invalid number.";
            return null;
        }

        return value;
    }

    private int? ParseOptionalInt(string input, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (!int.TryParse(input, out var value))
        {
            StatusMessage = $"{fieldName}: invalid integer.";
            return null;
        }

        return value;
    }
}
