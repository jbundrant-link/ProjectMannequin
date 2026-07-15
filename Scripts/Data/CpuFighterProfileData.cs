namespace ProjectMannequin.Data;

public sealed class CpuFighterProfileData
{
    public string Id { get; set; } = "standard_cpu";
    public int ReactionFrames { get; set; } = 8;
    public int DecisionIntervalFrames { get; set; } = 12;
    public int GuardHoldFrames { get; set; } = 22;
    public int MovementCommitmentFrames { get; set; } = 18;

    public float PreferredRangeMin { get; set; } = 1.05f;
    public float PreferredRangeMax { get; set; } = 2.35f;
    public float LaneTolerance { get; set; } = 0.55f;
    public float DashDistance { get; set; } = 4.2f;

    public float Aggression { get; set; } = 0.68f;
    public float GuardChance { get; set; } = 0.66f;
    public float AntiAirChance { get; set; } = 0.72f;
    public float PunishChance { get; set; } = 0.76f;
    public float RetreatChance { get; set; } = 0.28f;
    public float JumpEvadeChance { get; set; } = 0.12f;
    public float MistakeChance { get; set; } = 0.10f;

    // Rush Throw module: guard-break throw vs a blocking opponent, scaled by how
    // long block has been held.
    public float RushThrowBaseChance { get; set; } = 0.6f;
    public float RushThrowMaxBonus { get; set; } = 0.35f;
    public int RushThrowRampFrames { get; set; } = 60;

    // Z-Reflect on wake-up / hitstun-escape to punish mindless mashing.
    public float WakeupParryChance { get; set; } = 0.25f;
}
