namespace HealthTracker.Services;

using System.Globalization;
using System.Text;
using HealthTracker.Shared.Interfaces;

public class CsvExportService(
    IWeightRepository weightRepo,
    IBloodPressureRepository bpRepo,
    IBloodSugarRepository bsRepo)
{
    public async Task ExportWeight(DateOnly from, DateOnly to, string filePath, CancellationToken ct = default)
    {
        var entries = await weightRepo.GetEntries(from, to, ct);
        var tmpPath = filePath + ".tmp";

        await using (var writer = new StreamWriter(tmpPath, false, Encoding.UTF8))
        {
            await writer.WriteLineAsync("Date,Weight_kg");

            foreach (var e in entries)
                await writer.WriteLineAsync($"{e.Date:yyyy-MM-dd},{e.WeightKg.ToString(CultureInfo.InvariantCulture)}");
        }

        File.Move(tmpPath, filePath, overwrite: true);
    }

    public async Task ExportBloodPressure(DateOnly from, DateOnly to, string filePath, CancellationToken ct = default)
    {
        var entries = await bpRepo.GetEntries(from, to, ct);
        var tmpPath = filePath + ".tmp";

        await using (var writer = new StreamWriter(tmpPath, false, Encoding.UTF8))
        {
            await writer.WriteLineAsync("Date,TimeOfDay,Reading,Systolic_mmHg,Diastolic_mmHg");

            foreach (var e in entries)
            {
                var timeLabel = e.TimeOfDay.ToString();

                for (var i = 0; i < e.Readings.Count; i++)
                {
                    var r = e.Readings[i];
                    await writer.WriteLineAsync(
                        $"{e.Date:yyyy-MM-dd},{timeLabel},{i + 1},{r.SystolicMmhg},{r.DiastolicMmhg}");
                }

                if (e.Readings.Count > 1)
                {
                    var avgSys = Math.Round(e.Readings.Average(r => r.SystolicMmhg), 1);
                    var avgDia = Math.Round(e.Readings.Average(r => r.DiastolicMmhg), 1);
                    await writer.WriteLineAsync(
                        $"{e.Date:yyyy-MM-dd},{timeLabel},Average," +
                        $"{avgSys.ToString(CultureInfo.InvariantCulture)}," +
                        $"{avgDia.ToString(CultureInfo.InvariantCulture)}");
                }
            }
        }

        File.Move(tmpPath, filePath, overwrite: true);
    }

    public async Task ExportBloodSugar(DateOnly from, DateOnly to, string filePath, CancellationToken ct = default)
    {
        var entries = await bsRepo.GetEntries(from, to, ct);
        var tmpPath = filePath + ".tmp";

        await using (var writer = new StreamWriter(tmpPath, false, Encoding.UTF8))
        {
            await writer.WriteLineAsync("Date,Reading,BloodSugar_mmol_L,Context");

            foreach (var e in entries)
            {
                var context = char.ToUpperInvariant(e.Context[0]) + e.Context[1..];

                for (var i = 0; i < e.Readings.Count; i++)
                    await writer.WriteLineAsync(
                        $"{e.Date:yyyy-MM-dd},{i + 1},{e.Readings[i].ToString(CultureInfo.InvariantCulture)},{context}");

                if (e.Readings.Count > 1)
                {
                    var avg = Math.Round(e.Readings.Average(), 2);
                    await writer.WriteLineAsync(
                        $"{e.Date:yyyy-MM-dd},Average,{avg.ToString(CultureInfo.InvariantCulture)},{context}");
                }
            }
        }

        File.Move(tmpPath, filePath, overwrite: true);
    }
}
