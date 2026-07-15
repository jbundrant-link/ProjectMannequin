using System.Collections.Generic;
using System.Linq;

namespace ProjectMannequin.Data;

/// <summary>
/// The hidden "Hollow Archive" boss-rush, unlocked once every lore fragment has
/// been collected (see <c>MvpProgressStore.SecretEndingUnlocked</c>). It reuses a
/// real stage and the Archive's most complete recorded champion as a single
/// final challenge, dropping the player straight into the boss encounter.
/// </summary>
public static class HollowArchiveMission
{
    public const string WorldId = "hollow_archive";

    public static StageMissionData Create()
    {
        var mission = MvpWorldCatalog.CreateAstralBattlefrontMission();
        mission.Id = "hollow_archive_mvp";
        mission.WorldId = WorldId;
        mission.WorldDisplayName = "The Hollow Archive";
        mission.DisplayName = "The Hollow Archive";
        mission.BossFormId = ""; // everything is already unlocked to reach here.

        var byId = mission.Encounters.ToDictionary(encounter => encounter.Id);
        var gateway = byId.GetValueOrDefault("astral_breach_01");
        var spire = byId.GetValueOrDefault("astral_breach_03");
        var champion = mission.Encounters.FirstOrDefault(
            encounter => encounter.Kind == StageEncounterKind.Boss);
        if (gateway is null || spire is null || champion is null)
        {
            return mission; // fall back to the full mission if the layout changed.
        }

        // Branch point: after the gate, ascend the vertical spire or breach the
        // vault straight to the boss. Both routes end at the champion.
        gateway.DisplayName = "The Hollow Gate";
        gateway.RouteChoices = new List<RouteChoiceData>
        {
            new() { Label = "Ascend the Spire (vertical climb)", TargetEncounterId = spire.Id },
            new() { Label = "Breach the Vault (straight to the boss)", TargetEncounterId = champion.Id },
        };

        // The spire is a vertical-section climb with aerial enemies at staggered
        // heights, so the wall-run / wall-jump ascent has combat purpose.
        spire.DisplayName = "The Ascendant Spire";
        spire.VerticalSection = true;
        spire.VerticalCeiling = 12.0f;
        spire.Waves = new List<StageWaveData>();
        spire.UseRandomPool = false;
        spire.MaxActiveEnemies = 3;
        spire.Spawns = new List<EnemySpawnData>
        {
            AerialSpawn("astral_saibaman", "Rising Saibaman", 4.0f, EnemyEntryEdge.Right, 3.0f, 30),
            AerialSpawn("astral_ki_captain", "Sky Sentinel", 6.5f, EnemyEntryEdge.Left, -2.5f, 90),
            AerialSpawn("astral_frieza_scout", "Spire Watcher", 9.0f, EnemyEntryEdge.Right, 3.5f, 150),
        };

        champion.DisplayName = "The Archive Champion";

        mission.Encounters = new List<StageEncounterData> { gateway, spire, champion };
        return mission;
    }

    private static EnemySpawnData AerialSpawn(
        string archetype,
        string displayName,
        float height,
        EnemyEntryEdge edge,
        float offsetX,
        int delayFrames)
    {
        return new EnemySpawnData
        {
            ArchetypeId = archetype,
            DisplayName = displayName,
            EntryEdge = edge,
            EntryProfile = EnemyEntryProfile.WalkIn,
            OffsetX = offsetX,
            LaneZ = 0.0f,
            SpawnHeight = height,
            SpawnDelayFrames = delayFrames,
        };
    }
}
