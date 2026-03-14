namespace HealthTracker.Shared.Dtos;

using HealthTracker.Shared.Enums;

public record BloodPressureEntry(
    Guid Id,
    DateOnly Date,
    TimeOfDay TimeOfDay,
    IReadOnlyList<BloodPressureReading> Readings);
