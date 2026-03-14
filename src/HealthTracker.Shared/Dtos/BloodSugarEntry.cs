namespace HealthTracker.Shared.Dtos;

public record BloodSugarEntry(
    Guid Id,
    DateOnly Date,
    IReadOnlyList<decimal> Readings,
    string Context);
