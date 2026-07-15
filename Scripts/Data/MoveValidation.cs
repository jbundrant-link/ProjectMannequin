using System.Collections.Generic;
using System.Linq;
using ProjectMannequin.Combat;

namespace ProjectMannequin.Data;

/// <summary>
/// Castagne-style authoring validation for attack data. Flags illegal frame
/// timings, impossible hitbox windows, invalid cancel targets, and unsafe meter
/// costs so bad move data fails loudly instead of producing silent combat bugs.
/// This is pure and deterministic so it can run in content loading, the
/// frame-data report, and unit tests.
/// </summary>
public static class MoveValidation
{
    public static IReadOnlyList<string> Validate(CharacterData form, MoveData move)
    {
        var issues = new List<string>();
        var label = $"{form.Id}.{move.Id}";

        // Frame timings.
        if (move.StartupFrames < 1)
        {
            issues.Add($"{label}: startup must be >= 1 (was {move.StartupFrames}).");
        }

        if (move.ActiveFrames < 0)
        {
            issues.Add($"{label}: active frames cannot be negative (was {move.ActiveFrames}).");
        }

        if (move.RecoveryFrames < 0)
        {
            issues.Add($"{label}: recovery frames cannot be negative (was {move.RecoveryFrames}).");
        }

        if (move.HitstunFrames < 0 || move.BlockstunFrames < 0)
        {
            issues.Add($"{label}: hitstun/blockstun cannot be negative.");
        }

        if (move.InvulnerableStartupFrames < 0)
        {
            issues.Add($"{label}: invulnerable startup cannot be negative.");
        }
        else if (move.InvulnerableStartupFrames > move.TotalFrames)
        {
            issues.Add(
                $"{label}: invulnerable startup ({move.InvulnerableStartupFrames}) exceeds total frames ({move.TotalFrames}).");
        }

        // Cancel windows.
        if (move.CancelStartFrame >= 0 || move.CancelEndFrame >= 0)
        {
            if (move.CancelStartFrame < 0 || move.CancelEndFrame < 0)
            {
                issues.Add(
                    $"{label}: cancel window half-defined (start={move.CancelStartFrame}, end={move.CancelEndFrame}).");
            }
            else if (move.CancelStartFrame > move.CancelEndFrame)
            {
                issues.Add(
                    $"{label}: cancel start ({move.CancelStartFrame}) after cancel end ({move.CancelEndFrame}).");
            }
            else if (move.CancelEndFrame > move.TotalFrames)
            {
                issues.Add(
                    $"{label}: cancel end ({move.CancelEndFrame}) beyond total frames ({move.TotalFrames}).");
            }
        }

        if (move.JumpCancelStartFrame >= 0 || move.JumpCancelEndFrame >= 0)
        {
            if (move.JumpCancelStartFrame < 0 || move.JumpCancelEndFrame < 0)
            {
                issues.Add($"{label}: jump-cancel window half-defined.");
            }
            else if (move.JumpCancelStartFrame > move.JumpCancelEndFrame)
            {
                issues.Add($"{label}: jump-cancel start after end.");
            }
            else if (move.JumpCancelEndFrame > move.TotalFrames)
            {
                issues.Add($"{label}: jump-cancel end beyond total frames.");
            }
        }

        // Impossible hitbox windows.
        foreach (var box in move.CombatBoxes.Where(
                     b => b.BoxType is CombatBoxType.Hitbox or CombatBoxType.Grabbox))
        {
            if (box.EndFrame < box.StartFrame)
            {
                issues.Add($"{label}: box '{box.Id}' end ({box.EndFrame}) before start ({box.StartFrame}).");
            }
            else if (box.EndFrame < 0 || box.StartFrame > move.TotalFrames)
            {
                issues.Add(
                    $"{label}: box '{box.Id}' active window [{box.StartFrame},{box.EndFrame}] outside move length {move.TotalFrames}.");
            }
        }

        // Invalid cancel targets.
        foreach (var targetId in move.CancelIntoMoveIds.Where(id => form.FindMove(id) is null))
        {
            issues.Add($"{label}: cancel-into target '{targetId}' is not a move on form '{form.Id}'.");
        }

        // Unsafe meter costs.
        if (move.MeterCost < 0)
        {
            issues.Add($"{label}: meter cost cannot be negative.");
        }
        else if (move.MeterCost > form.MaxMeter)
        {
            issues.Add($"{label}: meter cost ({move.MeterCost}) exceeds form max meter ({form.MaxMeter}).");
        }

        if (move.IsSuper && move.MeterCost <= 0)
        {
            issues.Add($"{label}: super '{move.Id}' has no meter cost.");
        }

        // Proration sanity.
        if (move.MinimumDamageScale <= 0.0f || move.MinimumDamageScale > 1.0f)
        {
            issues.Add(
                $"{label}: minimum damage scale ({move.MinimumDamageScale}) should be within (0, 1].");
        }

        if (move.ProrationScale <= 0.0f || move.ProrationScale > 1.0f)
        {
            issues.Add(
                $"{label}: proration scale ({move.ProrationScale}) should be within (0, 1].");
        }

        // Combo loop / infinite-combo risk.
        if (move.IsLauncher && move.CancelIntoMoveIds.Contains(move.Id))
        {
            issues.Add($"{label}: launcher cancels into itself (infinite-loop risk).");
        }

        if (move.IsLauncher && move.MinimumDamageScale >= 0.95f)
        {
            issues.Add(
                $"{label}: launcher minimum scale ({move.MinimumDamageScale}) is too high (infinite-combo risk).");
        }

        return issues;
    }

    public static IReadOnlyList<string> ValidateForm(CharacterData form)
    {
        return form.Moves.SelectMany(move => Validate(form, move)).ToList();
    }
}
