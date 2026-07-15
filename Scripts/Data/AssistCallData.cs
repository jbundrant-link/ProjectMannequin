using ProjectMannequin.Data;

namespace ProjectMannequin.Progression;

/// <summary>
/// Defines the directional assist variants for a specific archived form.
/// Marvel Tōkon-style: Forward = projectile, Down = anti-air, Neutral = unique.
/// </summary>
public sealed class AssistCallData
{
    public string FormId { get; set; } = "";
    public string FormDisplayName { get; set; } = "";

    /// <summary>Neutral assist — the form's signature move (e.g. Hadouken, Kamehameha).</summary>
    public MoveData NeutralAssist { get; set; } = new();

    /// <summary>Forward+Assist — a projectile-style assist that covers horizontal range.</summary>
    public MoveData ForwardAssist { get; set; } = new();

    /// <summary>Down+Assist — an anti-air assist that covers vertical space.</summary>
    public MoveData DownAssist { get; set; } = new();
}

public enum AssistDirection
{
    Neutral,
    Forward,
    Down,
}
