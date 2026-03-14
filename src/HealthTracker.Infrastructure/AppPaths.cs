namespace HealthTracker.Infrastructure;

using HealthTracker.Shared.Interfaces;

public class AppPaths : IAppPaths
{
    public string DataDirectory { get; }

    public AppPaths()
    {
        DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "HealthTracker");

        // Ensure the directory exists on first access so repositories never need to guard against it.
        Directory.CreateDirectory(DataDirectory);
    }
}
