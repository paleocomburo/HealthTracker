namespace HealthTracker.UI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class NotificationViewModel : ObservableObject
{
    public ObservableCollection<NotificationMessage> Messages { get; } = [];

    public void Post(string text, NotificationSeverity severity = NotificationSeverity.Info)
    {
        var msg = new NotificationMessage(text, severity, new RelayCommand<NotificationMessage>(m =>
        {
            if (m is not null)
                Messages.Remove(m);
        }));

        Messages.Add(msg);
    }
}

public enum NotificationSeverity { Info, Warning, Danger }

public record NotificationMessage(string Text, NotificationSeverity Severity, IRelayCommand DismissCommand);
