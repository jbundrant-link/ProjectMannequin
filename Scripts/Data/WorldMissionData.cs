using System.Collections.Generic;

namespace ProjectMannequin.Data;

public enum ArchiveWorldAvailability
{
    Playable,
    InDevelopment,
    Locked,
}

public enum StageEncounterKind
{
    Horde,
    Elite,
    Boss,
}

public enum BossEncounterType
{
    Classic,
    IsolatedDuel,
    TagTeam,
}

public enum EnemySpawnPolicy
{
    Shared,
    PerPlayer,
}

public enum EnemyEntryEdge
{
    Left,
    Right,
    FarLane,
    NearLane,
}

public enum EnemyEntryProfile
{
    Auto,
    WalkIn,
    DropIn,
    Foreground,
    Background,
    Ambush,
}

public enum StageVisualLayerKind
{
    Far,
    Midground,
    Gameplay,
    Foreground,
}

public enum StageTextureSampling
{
    Linear,
    Nearest,
}

public enum StageHazardBehavior
{
    StaticPulse,
    LinearSweep,
    FallingStrike,
}

[System.Flags]
public enum StageHazardTargetMask
{
    None = 0,
    Players = 1,
    Enemies = 2,
    All = Players | Enemies,
}

public sealed class ArchiveWorldData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string CombatIdentity { get; set; } = "";
    public string StageName { get; set; } = "";
    public ArchiveWorldAvailability Availability { get; set; }
}

public sealed class WorldRunData
{
    public string WorldId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<StageMissionData> Stages { get; set; } = new();
}

public sealed class RouteChoiceData
{
    public string Label { get; set; } = "";
    public string TargetEncounterId { get; set; } = "";
}

public sealed class EnemySpawnData
{
    public string ArchetypeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public float OffsetX { get; set; }
    public float LaneZ { get; set; }
    public EnemyEntryEdge EntryEdge { get; set; } = EnemyEntryEdge.Right;
    public EnemyEntryProfile EntryProfile { get; set; } = EnemyEntryProfile.Auto;
    public int SpawnDelayFrames { get; set; }
    public int WarningLeadFrames { get; set; } = 30;
    public float EntryDistance { get; set; } = 1.8f;
    public float EntryHeight { get; set; } = 4.0f;
    public float SpawnHeight { get; set; }
}

public sealed class StageWaveData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int StartDelayFrames { get; set; }
    public int MaxActiveEnemies { get; set; }
    public int HealthReward { get; set; }
    public int MeterReward { get; set; }
    public List<EnemySpawnData> Spawns { get; set; } = new();
    public bool UseRandomPool { get; set; }
    public List<string> RandomArchetypePool { get; set; } = new();
    public int RandomSpawnCount { get; set; } = 10;
}

public sealed class StageEncounterData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public StageEncounterKind Kind { get; set; }
    public float TriggerX { get; set; }
    public float ArenaMinX { get; set; }
    public float ArenaMaxX { get; set; }
    // Optional encounter-specific belt depth. When disabled, mission-wide lane
    // bounds remain authoritative. Transitions interpolate deterministically.
    public bool UsesLaneBounds { get; set; }
    public float LaneMinZ { get; set; }
    public float LaneMaxZ { get; set; }
    public int LaneTransitionFrames { get; set; } = 30;
    public float CameraLockX { get; set; }
    public float CameraOrthographicSize { get; set; }
    public bool LocksFightRoom { get; set; } = true;
    public int CameraTransitionFrames { get; set; } = 30;

    /// <summary>
    /// Frames the cinematic build-up (Phase 1) holds before the UI reveal /
    /// READY beat. Player input stays frozen for this whole window.
    /// </summary>
    public int BossIntroCinematicFrames { get; set; } = 150;

    /// <summary>
    /// Frames the UI-reveal + READY call-to-action (Phases 2-3) holds after the
    /// cinematic before the FIGHT release unlocks the player.
    /// </summary>
    public int BossIntroReadyFrames { get; set; } = 108;
    public int EliteIntroReadyFrames { get; set; } = 36;
    public int EliteIntroFightFrames { get; set; } = 72;
    public int GateReleaseDelayFrames { get; set; } = 75;
    public float PlayerBoundaryInset { get; set; } = 0.12f;
    public int MaxActiveEnemies { get; set; } = 3;
    public int MaxSimultaneousAttackers { get; set; } = 2;
    public int MaxAttackersPerPlayer { get; set; } = 1;
    public float LightColorR { get; set; } = 1.0f;
    public float LightColorG { get; set; } = 1.0f;
    public float LightColorB { get; set; } = 1.0f;
    public float LightEnergyMultiplier { get; set; } = 1.0f;
    public float LightTransitionSeconds { get; set; } = 0.45f;
    public BossEncounterType BossType { get; set; } = BossEncounterType.Classic;
    public EnemySpawnPolicy SpawnPolicy { get; set; } = EnemySpawnPolicy.Shared;

    /// <summary>
    /// For IsolatedDuel boss encounters that spawn per player: the boss archetype
    /// each player faces, indexed by player slot (PlayerId - 1). When set, each
    /// player fights a distinct boss instead of a copy of the same one. Empty =
    /// every player faces the wave's authored boss archetype.
    /// </summary>
    public List<string> IsolatedDuelBossArchetypeIds { get; set; } = new();
    public int MergeAtPhase { get; set; } = 0;
    public float PerPlayerHPMultiplier { get; set; } = 1.0f;
    public bool VerticalSection { get; set; }
    public float VerticalCeiling { get; set; } = 8.0f;
    public List<RouteChoiceData> RouteChoices { get; set; } = new();

    // Boss-duel rules profile (opt-in; defaults keep beat-em-up behavior).
    public bool DuelFacingLock { get; set; }
    public float DuelLaneHalfDepth { get; set; } // 0 = no lane-depth constraint

    public List<StageWaveData> Waves { get; set; } = new();
    public List<EnemySpawnData> Spawns { get; set; } = new();
    
    // Hazards & Props
    public List<StagePropData> Props { get; set; } = new();
    public List<StageHazardZoneData> HazardZones { get; set; } = new();

    // Roguelike Horde Randomization
    public bool UseRandomPool { get; set; }
    public List<string> RandomArchetypePool { get; set; } = new();
    public int RandomSpawnCount { get; set; } = 10;

    public void Validate(string missionId)
    {
        if (ArenaMinX >= ArenaMaxX)
            Godot.GD.PushError($"[Validation] Encounter {Id} in {missionId} has invalid Arena bounds (MinX >= MaxX).");
        
        if (Waves.Count == 0 && Spawns.Count == 0 && !UseRandomPool)
            Godot.GD.PushWarning($"[Validation] Encounter {Id} in {missionId} has no waves or spawns defined.");
            
        if (Kind == StageEncounterKind.Boss && string.IsNullOrEmpty(DisplayName))
            Godot.GD.PushWarning($"[Validation] Boss encounter {Id} in {missionId} missing DisplayName.");
    }
}

public sealed class StageBackgroundPanelData
{
    public string TexturePath { get; set; } = "";
    public string DestroyedTexturePath { get; set; } = "";
    public List<string> DestructionTexturePaths { get; set; } = new();
    public List<string> DestructionBurstTexturePaths { get; set; } = new();
    public List<float> DestructionBurstAnchorXs { get; set; } = new();
    public float DestructionBurstPositionY { get; set; } = 2.4f;
    public float DestructionBurstPositionZ { get; set; } = -4.2f;
    public float DestructionBurstPixelSize { get; set; }
    public float DestructionOverlayPositionY { get; set; } = 1.5f;
    public float DestructionOverlayPositionZ { get; set; } = -4.3f;
    public float DestructionOverlayPixelSize { get; set; }
    public StageVisualLayerKind Layer { get; set; } = StageVisualLayerKind.Midground;
    public StageTextureSampling Sampling { get; set; } = StageTextureSampling.Linear;
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float PositionY { get; set; } = 1.0f;
    public float PositionZ { get; set; } = -6.0f;
    public float ParallaxFactorX { get; set; } = 1.0f;
    public float Opacity { get; set; } = 1.0f;
    public float ScaleYMultiplier { get; set; } = 1.0f;
    public float CropTopFraction { get; set; }
    public float CropBottomFraction { get; set; }
    public float DestroyedTintR { get; set; } = 0.72f;
    public float DestroyedTintG { get; set; } = 0.68f;
    public float DestroyedTintB { get; set; } = 0.72f;
    public bool FlipH { get; set; }
}

public enum StagePickupType
{
    Meter,
    Health,
    Score,
}

public sealed class StageAftermathVisualData
{
    public string DecalTexturePath { get; set; } = "";
    public float DecalSizeX { get; set; }
    public float DecalSizeZ { get; set; }
    public float DecalOpacity { get; set; } = 0.72f;
    public string FragmentSpritePath { get; set; } = "";
    public float FragmentPixelSize { get; set; }
    public float FragmentGroundOffsetPixels { get; set; }
    public float FragmentOffsetX { get; set; }
    public float FragmentOffsetZ { get; set; }
    public bool FragmentFlipH { get; set; }
}

public sealed class StagePropData
{
    public string Id { get; set; } = "";
    public string ArchetypeId { get; set; } = "prop_crate";
    public float PositionX { get; set; }
    public float PositionZ { get; set; }
    public int Health { get; set; } = 100;
    public bool IsThrowable { get; set; } = true;
    public bool SpawnsPickupOnBreak { get; set; } = true;
    public StagePickupType DropType { get; set; } = StagePickupType.Meter;
    public float DropChance { get; set; } = 1.0f;
    public bool ExplodesOnBreak { get; set; }
    public StageHazardTargetMask ExplosionTargets { get; set; } = StageHazardTargetMask.All;
    public float ExplosionRadius { get; set; }
    public int ExplosionDamage { get; set; }
    public float ExplosionKnockback { get; set; }
    public int ExplosionHitstunFrames { get; set; }
    public string SpritePath { get; set; } = "res://Assets/Sprites/Hazards/crate.png";
    public float SpritePixelSize { get; set; }
    public float SpriteGroundOffsetPixels { get; set; }
    public StageAftermathVisualData? AftermathVisual { get; set; }
}

public sealed class StageHazardZoneData
{
    public string Id { get; set; } = "";
    public StageHazardBehavior Behavior { get; set; } = StageHazardBehavior.StaticPulse;
    public StageHazardTargetMask Targets { get; set; } = StageHazardTargetMask.Players;
    public string SpritePath { get; set; } = "";
    public float SpritePixelSize { get; set; }
    public float SpriteGroundOffsetPixels { get; set; }
    public float SpriteTravelHeight { get; set; }
    public float SpriteAnchorX { get; set; } = 0.5f;
    public float SpriteAnchorZ { get; set; } = 0.5f;
    public bool SpriteFlipH { get; set; }
    public string FieldTexturePath { get; set; } = "";
    public bool FieldFlipH { get; set; }
    public StageAftermathVisualData? AftermathVisual { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinZ { get; set; } = -3.0f;
    public float MaxZ { get; set; } = 3.0f;
    public int WarningLeadFrames { get; set; } = 60;
    public int ActivationDelayFrames { get; set; }
    // Zero means remain active after the warning (legacy behavior).
    public int ActiveFrames { get; set; }
    // Zero means do not repeat. When set, includes warning + active + off time.
    public int RepeatIntervalFrames { get; set; }
    public float MovementOffsetX { get; set; }
    public float MovementOffsetZ { get; set; }
    public string WarningText { get; set; } = "WARNING: INCOMING HAZARD";
    public float DamagePerSecond { get; set; } = 50.0f;
    public float KnockbackX { get; set; }
    public float KnockbackZ { get; set; }
    public int HitstunFrames { get; set; }
    public bool ActiveDuringBoss { get; set; } = true;
}

public sealed class StageMissionData
{
    public string Id { get; set; } = "";
    public string WorldId { get; set; } = "";
    public string WorldDisplayName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int StageNumber { get; set; } = 1;
    public string StageTitle { get; set; } = "";
    public string StageSubtitle { get; set; } = "";
    public float ParTimeSeconds { get; set; } = 240.0f;
    public int RankScoreS { get; set; } = 20000;
    public int RankScoreA { get; set; } = 15000;
    public int RankScoreB { get; set; } = 10000;
    public int RankScoreC { get; set; } = 6000;
    public int StageIntroFrames { get; set; }
    public int StageIntroReadyFrame { get; set; }
    // Legacy one-mission routes remain final/form-completion missions by default.
    public bool IsFinalStage { get; set; } = true;
    public bool RequiresFormSwapToComplete { get; set; } = true;
    public string BossFormId { get; set; } = "";
    public string StageTexturePath { get; set; } = "";
    public float StageTexturePixelSize { get; set; } = 0.0135f;
    public float StageTextureScaleX { get; set; } = 3.1f;
    public float StageTextureScaleY { get; set; } = 1.0f;
    public float StageTexturePositionY { get; set; } = 1.5f;
    public int StageTextureCropTopPixels { get; set; }
    public int StageTextureCropBottomPixels { get; set; }

    // Walkable floor art. The road region of the stage image is painted onto
    // the receding 3D ground plane so characters stay grounded across the full
    // lane depth. FloorTextureTopFraction is the fraction of image height that
    // is buildings/backdrop (skipped); the remainder below is the road.
    public string FloorTexturePath { get; set; } = "";
    public float FloorTextureTopFraction { get; set; } = 0.62f;
    public float FloorTextureTileWidth { get; set; } = 18.0f;
    public float StageMinX { get; set; }
    public float StageMaxX { get; set; }
    public float LaneMinZ { get; set; }
    public float LaneMaxZ { get; set; }
    public float CameraViewportWidth { get; set; } = 16.0f;
    public float CameraScrollSpeed { get; set; } = 4.6f;
    public float CameraFollowThresholdX { get; set; } = 0.45f;
    public float CameraBaseSize { get; set; } = 8.8f;
    public float CameraMaxSize { get; set; } = 12.4f;
    public float CameraHorizontalPadding { get; set; } = 3.0f;
    public float CameraFollowSharpness { get; set; } = 8.0f;
    public float CameraZoomSharpness { get; set; } = 7.0f;
    public float CameraCinematicSize { get; set; } = 7.6f;
    public float CameraCinematicRecoverySeconds { get; set; } = 0.38f;
    public float ThreePlayerShakeScale { get; set; } = 0.72f;
    public float FourPlayerShakeScale { get; set; } = 0.52f;
    public float PlayerLeftScreenMargin { get; set; } = 1.15f;
    public float PlayerRightScreenMargin { get; set; } = 1.65f;
    public float PartySoftSeparationX { get; set; } = 10.0f;
    public float PartyHardSeparationX { get; set; } = 13.0f;
    public float PartyCatchUpSpeed { get; set; } = 8.0f;
    public float EnemyEntryPadding { get; set; } = 2.4f;
    public float FarColorR { get; set; } = 0.035f;
    public float FarColorG { get; set; } = 0.045f;
    public float FarColorB { get; set; } = 0.065f;
    public float FloorColorR { get; set; } = 0.16f;
    public float FloorColorG { get; set; } = 0.18f;
    public float FloorColorB { get; set; } = 0.20f;
    public float LaneAccentR { get; set; } = 0.22f;
    public float LaneAccentG { get; set; } = 0.78f;
    public float LaneAccentB { get; set; } = 0.84f;
    public float DefaultEnemyGravity { get; set; } = -80.0f;
    public List<StageBackgroundPanelData> BackgroundPanels { get; set; } = new();
    public List<StageEncounterData> Encounters { get; set; } = new();

    public void Validate()
    {
        if (StageMinX >= StageMaxX)
            Godot.GD.PushError($"[Validation] Mission {Id} has invalid stage bounds (MinX >= MaxX).");
        if (LaneMinZ >= LaneMaxZ)
            Godot.GD.PushError($"[Validation] Mission {Id} has invalid lane bounds (MinZ >= MaxZ).");
            
        foreach (var encounter in Encounters)
        {
            encounter.Validate(Id);
        }
    }
}
