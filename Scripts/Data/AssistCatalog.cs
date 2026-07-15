using System.Collections.Generic;
using ProjectMannequin.Data;

namespace ProjectMannequin.Progression;

/// <summary>
/// Maps form IDs to their directional assist attack definitions.
/// Each form the player has unlocked becomes a callable assist.
/// </summary>
public static class AssistCatalog
{
    private static readonly Dictionary<string, AssistCallData> _catalog = new();

    static AssistCatalog()
    {
        // Ryu's assists — classic Street Fighter projectile/anti-air
        _catalog["world_warrior_ryu_form"] = new AssistCallData
        {
            FormId = "world_warrior_ryu_form",
            FormDisplayName = "Ryu",
            NeutralAssist = new MoveData
            {
                Id = "assist_ryu_hadouken",
                DisplayName = "Hadouken Assist",
                InputCommand = "",
                StartupFrames = 8,
                ActiveFrames = 6,
                RecoveryFrames = 12,
                Damage = 18,
                HitstunFrames = 22,
                BlockstunFrames = 12,
                HitStopFrames = 5,
            },
            ForwardAssist = new MoveData
            {
                Id = "assist_ryu_tatsu",
                DisplayName = "Tatsumaki Assist",
                InputCommand = "",
                StartupFrames = 6,
                ActiveFrames = 12,
                RecoveryFrames = 14,
                Damage = 22,
                HitstunFrames = 24,
                BlockstunFrames = 14,
                HitStopFrames = 6,
                PushbackX = 4.0f,
            },
            DownAssist = new MoveData
            {
                Id = "assist_ryu_shoryuken",
                DisplayName = "Shoryuken Assist",
                InputCommand = "",
                StartupFrames = 4,
                ActiveFrames = 8,
                RecoveryFrames = 20,
                Damage = 28,
                HitstunFrames = 28,
                BlockstunFrames = 8,
                HitStopFrames = 8,
                LaunchY = 8.0f,
                IsLauncher = true,
            },
        };

        // Goku's assists — DBZ energy attacks
        _catalog["goku_boss_form"] = new AssistCallData
        {
            FormId = "goku_boss_form",
            FormDisplayName = "Goku",
            NeutralAssist = new MoveData
            {
                Id = "assist_goku_kamehameha",
                DisplayName = "Kamehameha Assist",
                InputCommand = "",
                StartupFrames = 12,
                ActiveFrames = 10,
                RecoveryFrames = 16,
                Damage = 30,
                HitstunFrames = 28,
                BlockstunFrames = 16,
                HitStopFrames = 8,
            },
            ForwardAssist = new MoveData
            {
                Id = "assist_goku_ki_blast",
                DisplayName = "Ki Blast Assist",
                InputCommand = "",
                StartupFrames = 4,
                ActiveFrames = 4,
                RecoveryFrames = 10,
                Damage = 12,
                HitstunFrames = 16,
                BlockstunFrames = 10,
                HitStopFrames = 3,
            },
            DownAssist = new MoveData
            {
                Id = "assist_goku_dragon_rush",
                DisplayName = "Dragon Rush Assist",
                InputCommand = "",
                StartupFrames = 6,
                ActiveFrames = 10,
                RecoveryFrames = 18,
                Damage = 24,
                HitstunFrames = 26,
                BlockstunFrames = 10,
                HitStopFrames = 6,
                LaunchY = 6.0f,
                PushbackX = 3.0f,
                IsLauncher = true,
            },
        };
    }

    public static AssistCallData? GetAssistData(string formId)
    {
        return _catalog.TryGetValue(formId, out var data) ? data : null;
    }

    public static void Register(string formId, AssistCallData data)
    {
        _catalog[formId] = data;
    }
}
