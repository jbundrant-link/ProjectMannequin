using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.Progression;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Headless coverage for the Marvel Tōkon-style assist tag system. Verifies that
/// a called assist actually deals damage to an enemy (previously the ghost was
/// spawned but produced no hitboxes, so assists did nothing), that it connects
/// during its active window, and that it strikes each target only once.
///
/// Gated by <c>PROJECT_MANNEQUIN_ASSIST_TEST=1</c>.
/// </summary>
public static class AssistTests
{
    public static string Run()
    {
        var results = new List<(string Name, bool Passed)>();

        try
        {
            var caller = new CombatActor
            {
                ActorId = "assist_caller",
                TeamId = 1,
                PlayerId = 1,
                IsPlayerControlled = true,
                FacingRight = true,
            };
            caller.Initialize(TestRosterFactory.CreateBlankMannequin());
            caller.FormArchive.UnlockForm(TestRosterFactory.CreateWorldWarriorRyuForm());
            caller.SimPosition = Vector3.Zero;

            var target = new CombatActor
            {
                ActorId = "assist_target",
                TeamId = 2,
                PlayerId = 0,
                IsPlayerControlled = false,
                FacingRight = false,
            };
            target.Initialize(TestRosterFactory.CreateTrainingEnemy());
            target.SimPosition = new Vector3(2.5f, 0.0f, 0.0f);

            var actors = new List<CombatActor> { caller, target };
            var events = new List<CombatPresentationEvent>();
            var startHealth = target.Health;

            var assistData = AssistCatalog.GetAssistData("world_warrior_ryu_form");
            results.Add(("Ryu assist data exists", assistData is not null));

            var accepted = assistData is not null
                && caller.AssistSystem.TryCallAssist(caller, assistData, AssistDirection.Neutral, 0);
            results.Add(("Assist call accepted", accepted));

            // Before the active window the enemy must be untouched.
            caller.RefreshCombatBoxes(0);
            target.RefreshCombatBoxes(0);
            caller.AssistSystem.ResolveHits(caller, actors, 0, events);
            results.Add(("No damage before active window (startup)", target.Health == startHealth));

            var hitTick = -1;
            for (var tick = 1; tick <= 20; tick++)
            {
                caller.RefreshCombatBoxes(tick);
                target.RefreshCombatBoxes(tick);
                caller.AssistSystem.ResolveHits(caller, actors, tick, events);
                if (hitTick < 0 && target.Health < startHealth)
                {
                    hitTick = tick;
                }
            }

            results.Add(("Assist dealt damage to enemy", target.Health < startHealth));
            results.Add(("Assist connected during active window", hitTick >= GameConstants.AssistStartupFrames && hitTick <= 20));

            var assistHits = events.Count(e =>
                e.Type == CombatPresentationEventType.HitConnected
                && (e.Payload?.StartsWith("assist_ryu") ?? false));
            results.Add(("Assist struck the target exactly once", assistHits == 1));

            var damageDealt = startHealth - target.Health;
            results.Add(("Assist damage in expected range (~18)", damageDealt >= 10 && damageDealt <= 20));

            caller.Free();
            target.Free();
        }
        catch (System.Exception exception)
        {
            results.Add(($"Unexpected exception: {exception.Message}", false));
        }

        var passed = results.Count > 0 && results.All(result => result.Passed);
        var builder = new StringBuilder();
        builder.AppendLine(
            $"[AssistTests] SUMMARY passed={passed} "
            + $"({results.Count(result => result.Passed)}/{results.Count})");
        foreach (var (name, ok) in results)
        {
            builder.AppendLine($"  [{(ok ? "PASS" : "FAIL")}] {name}");
        }

        builder.AppendLine(
            $"=== {results.Count(result => result.Passed)} passed, "
            + $"{results.Count(result => !result.Passed)} failed ===");
        return builder.ToString();
    }
}
