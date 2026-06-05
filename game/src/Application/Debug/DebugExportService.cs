using System.Text.Json;
using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Application.Debug;

public sealed class DebugExportService
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public DebugExportService()
    {
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    }

    public string ExportCombatLog(CombatState combat, string outputDirectory, string? fileName = null)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, fileName ?? $"combat_log_{combat.CombatId}.json");
        var document = new
        {
            combat.CombatId,
            combat.EncounterId,
            combat.Status,
            Events = combat.Log
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, jsonOptions));
        return path;
    }

    public string ExportMetrics(PlaytestMetricsReport report, string outputDirectory, string? fileName = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, fileName ?? $"playtest_metrics_seed_{report.RunSeed}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(report, jsonOptions));
        return path;
    }
}
