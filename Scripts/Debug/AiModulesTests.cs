using System.Collections.Generic;
using System.Text;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic tests for the modular boss-AI mechanics: Rush Throw held-block
/// scaling, Phase Burst recovery pacing, and phase move-pool fairness. Runs
/// headless via PROJECT_MANNEQUIN_AI_TEST=1.
/// </summary>
public static class AiModulesTests
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== AI Modules Tests ===");
        var passed = 0;
        var failed = 0;

        void Check(string name, bool condition)
        {
            if (condition)
            {
                passed++;
                sb.AppendLine($"  PASS {name}");
            }
            else
            {
                failed++;
                sb.AppendLine($"  FAIL {name}");
            }
        }

        bool Approx(float a, float b) => Mathf.IsEqualApprox(a, b);

        // Rush Throw held-block scaling.
        Check(
            "rush throw at zero block equals base",
            Approx(AiModules.ResolveRushThrowChance(0.6f, 0, 0.35f, 60), 0.6f));
        Check(
            "rush throw ramps to base plus bonus",
            Approx(AiModules.ResolveRushThrowChance(0.6f, 60, 0.35f, 60), 0.95f));
        Check(
            "rush throw ramp saturates",
            Approx(AiModules.ResolveRushThrowChance(0.6f, 120, 0.35f, 60), 0.95f));
        Check(
            "rush throw clamps at one",
            Approx(AiModules.ResolveRushThrowChance(0.9f, 60, 0.5f, 60), 1.0f));
        Check(
            "rush throw with zero ramp is immediate",
            Approx(AiModules.ResolveRushThrowChance(0.6f, 5, 0.35f, 0), 0.95f));

        // Phase Burst recovery pacing.
        Check(
            "phase neutral adds transition and recovery",
            AiModules.ResolvePhaseNeutralUntil(100, 36, 24) == 160);
        Check(
            "phase neutral clamps negative recovery",
            AiModules.ResolvePhaseNeutralUntil(100, 36, -10) == 136);

        // Phase move-pool fairness.
        Check(
            "empty phase pool allows any move",
            AiModules.IsMoveLegalForPhase("ryu_hadouken_light", new List<string>()));
        Check(
            "move in phase pool is legal",
            AiModules.IsMoveLegalForPhase("boss_cleave", new List<string> { "boss_cleave" }));
        Check(
            "move outside phase pool is illegal",
            !AiModules.IsMoveLegalForPhase("boss_super", new List<string> { "boss_cleave" }));

        // Per-phase module enablement.
        Check("no disabled list enables every module", AiModules.IsModuleEnabled(AiModules.ModuleThrow, null));
        Check(
            "empty disabled list enables every module",
            AiModules.IsModuleEnabled(AiModules.ModuleAntiAir, new List<string>()));
        Check(
            "listed module is disabled",
            !AiModules.IsModuleEnabled(AiModules.ModuleThrow, new List<string> { AiModules.ModuleThrow }));
        Check(
            "unlisted module stays enabled",
            AiModules.IsModuleEnabled(AiModules.ModuleAntiAir, new List<string> { AiModules.ModuleThrow }));

        sb.AppendLine($"=== {passed} passed, {failed} failed ===");
        return sb.ToString();
    }
}
