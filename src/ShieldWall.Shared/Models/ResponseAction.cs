using ShieldWall.Shared.Enums;

namespace ShieldWall.Shared.Models;

/// <summary>
/// A team's response decision for a classified alert.
/// Produced by <see cref="ShieldWall.Shared.Interfaces.IResponseEngine"/>.
/// </summary>
/// <param name="Action">The chosen response action.</param>
/// <param name="Justification">Reasoning for the selected action.</param>
/// <param name="Priority">Response urgency on a 1–5 scale (1 = highest priority).</param>
public record ResponseAction(
    ActionType Action,
    string Justification,
    int Priority);
