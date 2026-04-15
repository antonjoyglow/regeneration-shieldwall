namespace ShieldWall.Shared.Models;

/// <summary>
/// A team's predictive submission for the hero mission phase.
/// Teams predict the next sector to be targeted based on observed patterns.
/// </summary>
/// <param name="TeamId">The team submitting the prediction.</param>
/// <param name="PredictedSector">The grid sector predicted to be targeted next (e.g., "Delta-3").</param>
/// <param name="Confidence">Confidence level in the prediction, in the range 0.0–1.0.</param>
/// <param name="Timestamp">UTC timestamp when the prediction was made.</param>
public record TeamPrediction(
    string TeamId,
    string PredictedSector,
    double Confidence,
    DateTime Timestamp);
