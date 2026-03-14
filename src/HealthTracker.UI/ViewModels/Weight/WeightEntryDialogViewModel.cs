namespace HealthTracker.UI.ViewModels.Weight;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthTracker.Shared.Dtos;

public partial class WeightEntryDialogViewModel : ViewModelBase
{
    private readonly System.Threading.Tasks.TaskCompletionSource<WeightEntry?> _tcs = new();

    [ObservableProperty] private System.DateTimeOffset _entryDate = System.DateTimeOffset.Now;
    [ObservableProperty] private string _weightKgInput = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DialogHeight))]
    private string? _validationError;

    public double DialogHeight => ValidationError is null ? 240 : 268;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaveEnabled;

    // Pre-populated when editing an existing entry.
    public System.Guid? ExistingId { get; init; }

    public System.Threading.Tasks.Task<WeightEntry?> Result => _tcs.Task;

    // Raised when the user clicks Save or Cancel — the view subscribes and closes the window.
    public event System.EventHandler? RequestClose;

    partial void OnWeightKgInputChanged(string value) => Validate();
    partial void OnEntryDateChanged(System.DateTimeOffset value) => Validate();

    private void Validate()
    {
        if (!decimal.TryParse(WeightKgInput, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out var weight))
        {
            ValidationError = "Enter a valid weight.";
            IsSaveEnabled = false;
            return;
        }

        if (weight <= 0 || weight >= 500)
        {
            ValidationError = "Weight must be between 0 and 500 kg.";
            IsSaveEnabled = false;
            return;
        }

        if (EntryDate.Date > System.DateTime.Today)
        {
            ValidationError = "Date cannot be in the future.";
            IsSaveEnabled = false;
            return;
        }

        ValidationError = null;
        IsSaveEnabled = true;
    }

    [RelayCommand(CanExecute = nameof(IsSaveEnabled))]
    private void Save()
    {
        if (!decimal.TryParse(WeightKgInput, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out var weight))
            return;

        var date = System.DateOnly.FromDateTime(EntryDate.DateTime);
        var id = ExistingId ?? System.Guid.NewGuid();
        _tcs.TrySetResult(new WeightEntry(id, date, weight));
        RequestClose?.Invoke(this, System.EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        _tcs.TrySetResult(null);
        RequestClose?.Invoke(this, System.EventArgs.Empty);
    }
}
