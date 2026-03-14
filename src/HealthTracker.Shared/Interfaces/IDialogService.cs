namespace HealthTracker.Shared.Interfaces;

public interface IDialogService
{
    /// <summary>
    /// Shows a yes/no confirmation dialog. Returns true if the user confirmed.
    /// </summary>
    Task<bool> ShowConfirmation(string title, string message);

    /// <summary>
    /// Opens a Save File dialog and returns the chosen path, or null if cancelled.
    /// </summary>
    Task<string?> ShowSaveFileDialog(string suggestedFileName, string fileTypeDescription, string extension);
}
