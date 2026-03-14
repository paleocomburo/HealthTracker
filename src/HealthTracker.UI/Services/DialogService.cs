namespace HealthTracker.UI.Services;

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using HealthTracker.Shared.Interfaces;

public class DialogService : IDialogService
{
    private Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public async Task<bool> ShowConfirmation(string title, string message)
    {
        if (MainWindow is null)
            return false;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var tcs = new TaskCompletionSource<bool>();

        var yesButton = new Button { Content = "Yes", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var noButton = new Button { Content = "No", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };

        yesButton.Click += (_, _) => { tcs.SetResult(true); dialog.Close(); };
        noButton.Click += (_, _) => { tcs.SetResult(false); dialog.Close(); };
        dialog.Closing += (_, _) => tcs.TrySetResult(false);

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    Children = { yesButton, noButton }
                }
            }
        };

        Grid.SetColumn(noButton, 1);

        await dialog.ShowDialog(MainWindow);
        return await tcs.Task;
    }

    public async Task<string?> ShowSaveFileDialog(string suggestedFileName, string fileTypeDescription, string extension)
    {
        if (MainWindow is null)
            return null;

        var result = await MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedFileName,
            FileTypeChoices =
            [
                new FilePickerFileType(fileTypeDescription) { Patterns = [$"*{extension}"] }
            ]
        });

        return result?.Path.LocalPath;
    }
}
