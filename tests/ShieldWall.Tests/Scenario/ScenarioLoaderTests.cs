using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using ShieldWall.GameMaster.Services;

namespace ShieldWall.Tests.Scenario;

public sealed class ScenarioLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ScenarioLoader _loader;

    public ScenarioLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"shieldwall-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _loader = new ScenarioLoader(NullLogger<ScenarioLoader>.Instance);
    }

    [Fact]
    public void Load_ValidScenario_LoadsSuccessfully()
    {
        var path = WriteJson("valid.json", ValidScenarioJson);

        var scenario = _loader.Load(path);

        Assert.NotNull(scenario);
        Assert.Equal(3, scenario.Alerts.Count);
        Assert.Single(scenario.CompoundThreats);
    }

    [Fact]
    public void Load_UnsortedAlerts_ThrowsInvalidOperationException()
    {
        var json = """
            {
              "metadata": { "totalAlerts": 2, "durationMinutes": 60, "phases": [] },
              "alerts": [
                { "alertId": "SA-0001", "broadcastOffsetSeconds": 20, "sector": "A-1",
                  "type": "perimeter", "rawSeverity": 5, "confidenceScore": 0.8, "source": "Radar",
                  "groundTruth": { "correctClassification": "medium", "correctAction": "monitor",
                                   "isCompoundMember": false, "compoundGroupId": null } },
                { "alertId": "SA-0002", "broadcastOffsetSeconds": 10, "sector": "B-2",
                  "type": "cyber", "rawSeverity": 3, "confidenceScore": 0.5, "source": "Sensor",
                  "groundTruth": { "correctClassification": "low", "correctAction": "monitor",
                                   "isCompoundMember": false, "compoundGroupId": null } }
              ],
              "compoundThreats": []
            }
            """;
        var path = WriteJson("unsorted.json", json);

        Assert.Throws<InvalidOperationException>(() => _loader.Load(path));
    }

    [Fact]
    public void Load_DuplicateAlertIds_ThrowsInvalidOperationException()
    {
        var json = """
            {
              "metadata": { "totalAlerts": 2, "durationMinutes": 60, "phases": [] },
              "alerts": [
                { "alertId": "SA-0001", "broadcastOffsetSeconds": 10, "sector": "A-1",
                  "type": "perimeter", "rawSeverity": 5, "confidenceScore": 0.8, "source": "Radar",
                  "groundTruth": { "correctClassification": "medium", "correctAction": "monitor",
                                   "isCompoundMember": false, "compoundGroupId": null } },
                { "alertId": "SA-0001", "broadcastOffsetSeconds": 20, "sector": "B-2",
                  "type": "cyber", "rawSeverity": 3, "confidenceScore": 0.5, "source": "Sensor",
                  "groundTruth": { "correctClassification": "low", "correctAction": "monitor",
                                   "isCompoundMember": false, "compoundGroupId": null } }
              ],
              "compoundThreats": []
            }
            """;
        var path = WriteJson("duplicate.json", json);

        Assert.Throws<InvalidOperationException>(() => _loader.Load(path));
    }

    [Fact]
    public void Load_MissingFile_ThrowsInvalidOperationException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "does-not-exist.json");

        Assert.Throws<InvalidOperationException>(() => _loader.Load(nonExistentPath));
    }

    [Fact]
    public void Load_CompoundThreatReferencesUnknownAlert_ThrowsInvalidOperationException()
    {
        var json = """
            {
              "metadata": { "totalAlerts": 1, "durationMinutes": 60, "phases": [] },
              "alerts": [
                { "alertId": "SA-0001", "broadcastOffsetSeconds": 10, "sector": "A-1",
                  "type": "perimeter", "rawSeverity": 5, "confidenceScore": 0.8, "source": "Radar",
                  "groundTruth": { "correctClassification": "medium", "correctAction": "monitor",
                                   "isCompoundMember": false, "compoundGroupId": null } }
              ],
              "compoundThreats": [
                { "groupId": "CG-001", "description": "Bad group",
                  "memberAlertIds": ["SA-9999"],
                  "correctEscalatedLevel": "high", "correctAction": "escalate",
                  "windowSeconds": 60 }
              ]
            }
            """;
        var path = WriteJson("bad-compound.json", json);

        Assert.Throws<InvalidOperationException>(() => _loader.Load(path));
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string WriteJson(string fileName, string json)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, json);
        return path;
    }

    private const string ValidScenarioJson = """
        {
          "metadata": { "totalAlerts": 3, "durationMinutes": 60, "phases": [] },
          "alerts": [
            { "alertId": "SA-0001", "broadcastOffsetSeconds": 10, "sector": "A-1",
              "type": "perimeter", "rawSeverity": 5, "confidenceScore": 0.8, "source": "Radar",
              "groundTruth": { "correctClassification": "medium", "correctAction": "monitor",
                               "isCompoundMember": true, "compoundGroupId": "CG-001" } },
            { "alertId": "SA-0002", "broadcastOffsetSeconds": 20, "sector": "B-2",
              "type": "cyber", "rawSeverity": 3, "confidenceScore": 0.5, "source": "Sensor",
              "groundTruth": { "correctClassification": "low", "correctAction": "monitor",
                               "isCompoundMember": true, "compoundGroupId": "CG-001" } },
            { "alertId": "SA-0003", "broadcastOffsetSeconds": 30, "sector": "C-3",
              "type": "sensor", "rawSeverity": 7, "confidenceScore": 0.9, "source": "BioSensor",
              "groundTruth": { "correctClassification": "high", "correctAction": "escalate",
                               "isCompoundMember": false, "compoundGroupId": null } }
          ],
          "compoundThreats": [
            { "groupId": "CG-001", "description": "Multi-vector approach",
              "memberAlertIds": ["SA-0001", "SA-0002"],
              "correctEscalatedLevel": "high", "correctAction": "escalate", "windowSeconds": 60 }
          ]
        }
        """;
}
