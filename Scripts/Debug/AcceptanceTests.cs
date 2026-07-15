using System.Text;
using ProjectMannequin.Combat;
using ProjectMannequin.Data;
using ProjectMannequin.Presentation;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// The Task 12 fighting-game acceptance pass, expressed as verifiable checks:
/// the three reference bosses (Archive Knight, Ryu, Goku) are duel-ready, the
/// Phase 5 feature slice resolves correctly (Reflect Guard pushback, Phase Burst
/// push, Rush Throw ramp, deterministic Animation Driver), and authored move
/// data sits inside sane game-feel ranges. Failures print the offending value.
///
/// Run with the environment flag PROJECT_MANNEQUIN_ACCEPTANCE_TEST=1.
/// </summary>
public static class AcceptanceTests
{
    public static string Run()
    {
        var log = new StringBuilder();
        var passed = 0;
        var failed = 0;

        void Check(bool condition, string label, string detail = "")
        {
            if (condition)
            {
                passed++;
                log.Append("  PASS ").Append(label).Append('\n');
            }
            else
            {
                failed++;
                log.Append("  FAIL ").Append(label).Append('\n');
                if (detail.Length > 0)
                {
                    log.Append("       ").Append(detail).Append('\n');
                }
            }
        }

        log.Append("=== Acceptance Tests ===\n");

        // Three duel-ready bosses.
        var bosses = new[]
        {
            TestRosterFactory.CreateTestBoss(),
            TestRosterFactory.CreateWorldWarriorRyuBoss(),
            GokuRosterFactory.CreateGokuBoss(),
        };

        foreach (var boss in bosses)
        {
            var readiness = FightingLayerAudit.AuditBoss(boss);
            Check(
                readiness.IsReady,
                $"{readiness.DisplayName} is duel-ready (kit/phases/super/guard/AI)",
                $"moves={readiness.MoveCount} phases={readiness.PhaseCount} supers={readiness.SuperCount} "
                    + $"guard={readiness.HasGuardGauge} ai={readiness.HasCpuProfile}");
        }

        // Phase 5 feature slice: Reflect Guard pushes an attacker away.
        var reflectPlayer = HitResolution.ResolveReflectPushback(-1.0f, isBoss: false);
        var reflectBoss = HitResolution.ResolveReflectPushback(-1.0f, isBoss: true);
        Check(reflectPlayer > 0.0f && reflectBoss > 0.0f,
            "Reflect Guard produces outward pushback", $"player={reflectPlayer} boss={reflectBoss}");

        // Phase Burst pushes a target away from the boss on transition.
        var burstRight = PhaseBurst.ResolvePushVelocity(95.0f, 100.0f, 25.0f);
        var burstLeft = PhaseBurst.ResolvePushVelocity(100.0f, 95.0f, 25.0f);
        Check(burstRight > 0.0f && burstLeft < 0.0f,
            "Phase Burst pushes targets outward on transition", $"right={burstRight} left={burstLeft}");

        // Rush Throw ramps up the longer a target holds guard, beating passivity.
        var rushIdle = AiModules.ResolveRushThrowChance(0.6f, 0, 0.35f, 60);
        var rushHeld = AiModules.ResolveRushThrowChance(0.6f, 60, 0.35f, 60);
        Check(rushHeld > rushIdle && rushHeld <= 0.96f,
            "Rush Throw chance rises against passive blocking", $"idle={rushIdle} held={rushHeld}");

        // Animation Driver is a pure function of combat state (cannot change combat).
        Check(
            AnimationDriver.ResolveClip(CombatActorState.Attacking) == AnimationClipKind.Attack
                && AnimationDriver.ResolveClip(CombatActorState.Knockdown) == AnimationClipKind.Knockdown
                && AnimationDriver.ResolveClip(CombatActorState.Idle) == AnimationClipKind.Idle,
            "Animation Driver maps combat state deterministically");

        // Game feel: authored data within tuning ranges.
        var feelIssues = FightingLayerAudit.AuditGameFeel(RosterCatalog.ReferenceForms());
        Check(feelIssues.Count == 0,
            "reference roster move data is within game-feel ranges",
            string.Join(" | ", feelIssues));

        // Ryu translation-session regressions accepted through interactive QA.
        var ryu = bosses[1];
        var hadouken = ryu.FindMove("ryu_hadouken_light");
        var hadoukenSpawn = hadouken?.ProjectileSpawns.FirstOrDefault();
        Check(
            hadoukenSpawn is not null
                && System.MathF.Abs(hadoukenSpawn.OffsetY - 2.65f) < 0.001f
                && System.MathF.Abs(hadoukenSpawn.CollisionOffsetY - -1.15f) < 0.001f,
            "Ryu Hadouken remains centered between his hands",
            $"visualY={hadoukenSpawn?.OffsetY} collisionOffsetY={hadoukenSpawn?.CollisionOffsetY}");

        var atlasMoves = ryu.Moves
            .Where(move => !string.IsNullOrWhiteSpace(move.AnimationAtlasPath))
            .ToList();
        Check(
            atlasMoves.Count >= 20
                && atlasMoves.All(move => System.MathF.Abs(move.AnimationGroundOffsetPixels - 120.0f) < 0.001f),
            "Ryu v5 attacks retain corrected ground anchoring",
            $"atlasMoves={atlasMoves.Count} offsets={string.Join(",", atlasMoves.Select(move => move.AnimationGroundOffsetPixels).Distinct())}");

        var jab = ryu.FindMove("ryu_jab");
        var strong = ryu.FindMove("ryu_strong");
        var mediumHadouken = ryu.FindMove("ryu_hadouken_medium");
        Check(
            jab is not null
                && strong is not null
                && mediumHadouken is not null
                && jab.CanCancelInto(strong)
                && strong.CanCancelInto(mediumHadouken),
            "Ryu jab to strong to Hadouken cancel route remains authored");

        // Every reference boss exposes at least neutral + pressure variety and a super.
        foreach (var boss in bosses)
        {
            var normals = boss.Moves.Count(move => !move.IsSuper);
            var supers = boss.Moves.Count(move => move.IsSuper);
            Check(normals >= 2 && supers >= 1,
                $"{boss.DisplayName} has readable neutral/pressure/super beats",
                $"normals={normals} supers={supers}");
        }

        var report = FightingLayerAudit.BuildReport(bosses, RosterCatalog.ReferenceForms());
        Check(report.Contains("Acceptance Audit"), "acceptance audit report builds");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }
}
