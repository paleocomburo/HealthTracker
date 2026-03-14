namespace HealthTracker.UI.Messages;

/// <summary>
/// Sent via WeakReferenceMessenger when a child view model (e.g. Dashboard) needs to trigger
/// top-level navigation without being coupled to MainWindowViewModel.
/// </summary>
public record NavigateMessage(string Destination);
