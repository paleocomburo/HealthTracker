namespace HealthTracker.UI.ViewModels.Shared;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthTracker.Shared.Enums;

public partial class DateRangeFilterViewModel : ViewModelBase
{
    public event System.EventHandler? RangeChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeriodLabel))]
    private DateRangeMode _selectedMode = DateRangeMode.Month;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeriodLabel))]
    private System.DateOnly _rangeStart;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeriodLabel))]
    private System.DateOnly _rangeEnd;

    public string PeriodLabel => SelectedMode switch
    {
        DateRangeMode.Week  => $"{RangeStart:d MMM} – {RangeEnd:d MMM}",
        DateRangeMode.Month => $"{RangeStart:MMM yyyy}",
        DateRangeMode.Year  => $"{RangeStart.Year}",
        _                   => $"{RangeStart:d MMM} – {RangeEnd:d MMM}"
    };

    public DateRangeFilterViewModel()
    {
        ApplyMode(DateRangeMode.Month, System.DateOnly.FromDateTime(System.DateTime.Today));
    }

    partial void OnSelectedModeChanged(DateRangeMode value)
    {
        // Re-anchor to today when switching modes so the user gets a sensible range.
        ApplyMode(value, System.DateOnly.FromDateTime(System.DateTime.Today));
        RangeChanged?.Invoke(this, System.EventArgs.Empty);
    }

    [RelayCommand]
    private void StepBackward()
    {
        // Move the anchor back by one unit of the current mode.
        var anchor = RangeStart;
        anchor = SelectedMode switch
        {
            DateRangeMode.Week   => anchor.AddDays(-7),
            DateRangeMode.Month  => anchor.AddMonths(-1),
            DateRangeMode.Year   => anchor.AddYears(-1),
            _                    => anchor.AddMonths(-1)
        };
        ApplyMode(SelectedMode, anchor);
        RangeChanged?.Invoke(this, System.EventArgs.Empty);
    }

    [RelayCommand]
    private void StepForward()
    {
        var anchor = RangeStart;
        anchor = SelectedMode switch
        {
            DateRangeMode.Week   => anchor.AddDays(7),
            DateRangeMode.Month  => anchor.AddMonths(1),
            DateRangeMode.Year   => anchor.AddYears(1),
            _                    => anchor.AddMonths(1)
        };
        ApplyMode(SelectedMode, anchor);
        RangeChanged?.Invoke(this, System.EventArgs.Empty);
    }

    [RelayCommand]
    private void SelectMode(DateRangeMode mode) => SelectedMode = mode;

    public void SetCustomRange(System.DateOnly from, System.DateOnly to)
    {
        SelectedMode = DateRangeMode.Custom;
        RangeStart = from;
        RangeEnd = to;
        RangeChanged?.Invoke(this, System.EventArgs.Empty);
    }

    private void ApplyMode(DateRangeMode mode, System.DateOnly anchor)
    {
        (RangeStart, RangeEnd) = mode switch
        {
            DateRangeMode.Week  => (anchor, anchor.AddDays(6)),
            DateRangeMode.Month => (new System.DateOnly(anchor.Year, anchor.Month, 1),
                                    new System.DateOnly(anchor.Year, anchor.Month,
                                        System.DateTime.DaysInMonth(anchor.Year, anchor.Month))),
            DateRangeMode.Year  => (new System.DateOnly(anchor.Year, 1, 1),
                                    new System.DateOnly(anchor.Year, 12, 31)),
            _                   => (RangeStart, RangeEnd) // Custom: leave as-is
        };
    }
}
