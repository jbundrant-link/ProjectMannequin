namespace ProjectMannequin.Data;

public sealed class ArcadeEnemyProfileData
{
    public string Id { get; set; } = "standard_arcade_enemy";
    public float AttackRange { get; set; } = 1.15f;
    public float PositionTolerance { get; set; } = 0.28f;
    public float LaneTolerance { get; set; } = 0.42f;
    public float SlotLaneSpacing { get; set; } = 0.58f;
    public float RetreatDistance { get; set; } = 2.65f;
    public int RetreatFrames { get; set; } = 34;
    public int ReengageDelayFrames { get; set; } = 24;
    public float ApproachSpeedMultiplier { get; set; } = 0.82f;
    public float RetreatSpeedMultiplier { get; set; } = 0.72f;
}
