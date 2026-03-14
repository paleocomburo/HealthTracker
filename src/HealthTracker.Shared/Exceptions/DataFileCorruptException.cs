namespace HealthTracker.Shared.Exceptions;

public class DataFileCorruptException(string filePath, Exception inner)
    : Exception($"Data file is corrupt or unreadable: {filePath}", inner)
{
    public string FilePath { get; } = filePath;
}
