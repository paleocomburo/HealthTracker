namespace HealthTracker.UI.ViewModels.BloodSugar;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthTracker.Shared.Dtos;

public partial class BloodSugarEntryDialogViewModel : ViewModelBase
{
    private readonly System.Threading.Tasks.TaskCompletionSource<BloodSugarEntry?> _tcs = new();

    [ObservableProperty] private DateTimeOffset _entryDate = DateTimeOffset.Now;
    [ObservableProperty] private string? _validationError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isSaveEnabled;

    public Guid? ExistingId { get; init; }
    public ObservableCollection<BloodSugarReadingRowViewModel> Readings { get; } = [new()];
    public System.Threading.Tasks.Task<BloodSugarEntry?> Result => _tcs.Task;
    public event EventHandler? RequestClose;

    public BloodSugarEntryDialogViewModel()
    {
        Readings.CollectionChanged += (_, _) => SubscribeToReadings();
        SubscribeToReadings();
    }

    partial void OnEntryDateChanged(DateTimeOffset value) => Validate();

    private void SubscribeToReadings()
    {
        foreach (var row in Readings)
            row.PropertyChanged += (_, _) => Validate();
        Validate();
    }

    private void Validate()
    {
        // Surface the first row-level validation error
        foreach (var row in Readings)
        {
            if (row.ValidationError is { } rowError)
            {
                ValidationError = rowError;
                IsSaveEnabled = false;
                return;
            }
        }

        // All rows must be fully filled with valid values before save is enabled
        if (!Readings.All(r => r.TryGetReading(out _)))
        {
            ValidationError = null;
            IsSaveEnabled = false;
            return;
        }

        if (EntryDate.Date > DateTime.Today)
        {
            ValidationError = "Date cannot be in the future.";
            IsSaveEnabled = false;
            return;
        }

        ValidationError = null;
        IsSaveEnabled = true;
    }

    [RelayCommand]
    private void AddReading()
    {
        if (Readings.Count < 3)
            Readings.Add(new BloodSugarReadingRowViewModel());
    }

    [RelayCommand]
    private void RemoveReading(BloodSugarReadingRowViewModel row)
    {
        if (Readings.Count > 1)
            Readings.Remove(row);
    }

    [RelayCommand(CanExecute = nameof(IsSaveEnabled))]
    private void Save()
    {
        var values = new System.Collections.Generic.List<decimal>();

        foreach (var row in Readings)
        {
            if (!row.TryGetReading(out var val))
                return;
            values.Add(val);
        }

        var date = DateOnly.FromDateTime(EntryDate.DateTime);
        var id = ExistingId ?? Guid.NewGuid();
        _tcs.TrySetResult(new BloodSugarEntry(id, date, values.AsReadOnly(), "fasting"));
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        _tcs.TrySetResult(null);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
