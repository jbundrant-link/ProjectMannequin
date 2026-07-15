using System.Collections.Generic;

namespace ProjectMannequin.Data;

/// <summary>
/// Provides an actionable "death as a teacher" tip for the move that killed the
/// player. Explicit entries cover signature boss and enemy attacks; anything
/// else falls back to keyword heuristics on the move id so the whole roster —
/// not just the six bosses — still teaches a counter-strategy.
/// </summary>
public static class DeathTipsCatalog
{
    private static readonly Dictionary<string, string> _tips = new()
    {
        // --- Boss signatures ---
        { "world_warrior_boss_shinku_hadouken", "The Shinku Hadouken deals massive chip damage. Jump over it, or Perfect Parry the first hit to avoid guard break." },
        { "world_warrior_boss_shoryuken", "Shoryuken is invincible on startup! Bait it out and punish the long recovery." },
        { "world_warrior_boss_tatsumaki", "Tatsumaki closes distance quickly. Block low to avoid the mixup, then counter-attack." },
        { "boss_goku_spirit_bomb", "The Spirit Bomb is unblockable! Interrupt him before he finishes charging, or use an Invincible Dash." },
        { "boss_goku_meteor_smash", "Meteor Smash tracks your position. Stay mobile and use Instinct Evade to dodge the final blow." },
        { "boss_goku_kamehameha", "A raw Kamehameha leaves him wide open. Dash under it or use a projectile-invulnerable move." },

        // --- Archive District constructs ---
        { "archive_scout_attack", "Scouts are fast but fragile. Beat their jab with a longer poke, or block and punish." },
        { "archive_raider_attack", "The Raider's cross has commitment. Block it and punish, or step back and whiff-punish." },
        { "archive_bruiser_attack", "The Bruiser's hammer is slow and telegraphed. Block, then punish the recovery — don't trade." },

        // --- Astral Battlefront enemies ---
        { "saibaman_headbutt", "The Saibaman's leaping headbutt is a committed jump. Anti-air it, or dash under and punish." },
        { "scout_ki_shot", "Scout Ki Shots are zoning tools. Jump over or dash through, then close the distance." },
        { "captain_beam", "The Ki Captain's Command Beam is slow to fire. Block it or dash under, then punish the recovery." },
        { "heavy_hammer", "The Armored Hammer has heavy commitment — don't trade jabs. Bait it, then punish the whiff." },
    };

    public static string GetTipForMove(string moveId)
    {
        if (string.IsNullOrWhiteSpace(moveId))
        {
            return "Stay mobile and read the enemy's rhythm before committing.";
        }

        if (_tips.TryGetValue(moveId, out var tip))
        {
            return tip;
        }

        // Keyword heuristics so the whole roster teaches something, not just the
        // explicitly-authored moves.
        var id = moveId.ToLowerInvariant();

        if (id.Contains("throw") || id.Contains("grab") || id.Contains("command_grab") || id.Contains("suplex"))
        {
            return "Throws beat blocking. Jump or strike them out of the grab startup, or tech it by pressing throw as it lands.";
        }

        if (id.Contains("hadouken") || id.Contains("kamehameha") || id.Contains("ki_blast")
            || id.Contains("ki_shot") || id.Contains("beam") || id.Contains("fireball") || id.Contains("projectile"))
        {
            return "That's a zoning projectile. Jump over it and punish the recovery, or dash through with an invincible move.";
        }

        if (id.Contains("shoryuken") || id.Contains("uppercut") || id.Contains("launcher")
            || id.Contains("rising") || id.Contains("anti_air") || id.Contains("dp"))
        {
            return "That's an anti-air / reversal. Stop jumping in predictably — bait it out and punish the landing recovery.";
        }

        if (id.Contains("charge") || id.Contains("rush") || id.Contains("headbutt")
            || id.Contains("dash") || id.Contains("lunge"))
        {
            return "A committed rush. Block it and punish, or interrupt the startup with a faster jab.";
        }

        if (id.Contains("hammer") || id.Contains("heavy") || id.Contains("cleave")
            || id.Contains("smash") || id.Contains("roundhouse") || id.Contains("fierce"))
        {
            return "Heavy swings are slow. Block, then punish the long recovery — don't try to trade.";
        }

        return "Study what hit you — block it, whiff-punish it, or interrupt its startup next time.";
    }
}
