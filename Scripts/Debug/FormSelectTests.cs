using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Headless coverage for in-fight form selection. Drives the form-swap state
/// machine directly (no UI) to verify that an explicit, non-next target form is
/// applied deterministically and that a non-equipped form is rejected, so the
/// in-combat selector can never bypass active-loadout rules.
///
/// Gated by <c>PROJECT_MANNEQUIN_FORM_SELECT_TEST=1</c>.
/// </summary>
public static class FormSelectTests
{
    public static string Run()
    {
        var results = new List<(string Name, bool Passed)>();

        try
        {
            var interpreter = new CommandInterpreter();
            var input = new PlayerInputBuffer();

            var actor = new CombatActor
            {
                ActorId = "form_select_test_p1",
                TeamId = 1,
                PlayerId = 1,
                IsPlayerControlled = true,
                FacingRight = true,
            };
            actor.Initialize(TestRosterFactory.CreateBlankMannequin());
            actor.FormArchive.UnlockForm(TestRosterFactory.CreateWorldWarriorRyuForm());
            actor.FormArchive.UnlockForm(GokuRosterFactory.CreateGokuArchiveForm());

            var actors = new List<CombatActor> { actor };
            var tick = 0;

            void Step(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    input.PushFrame(++tick, InputButtons.None);
                    actor.StateMachine.Update(tick, input, interpreter, actors);
                }
            }

            // Loadout = [blank_mannequin, world_warrior_ryu_form, goku_archive_form];
            // current = blank_mannequin, so the "next" equipped form is Ryu. Goku is
            // therefore a non-adjacent target that the old blind-cycle could not reach.
            results.Add(("Selector opens with 3 equipped forms", actor.StateMachine.CanOpenFormSelect));
            results.Add((
                "Baseline: next equipped form is Ryu",
                actor.FormArchive.GetNextEquippedForm(actor.CurrentForm.Id)?.Id == "world_warrior_ryu_form"));

            // Request the explicit NON-next target (Goku) and let the swap resolve.
            actor.StateMachine.RequestFormSwap("goku_archive_form");
            Step(GameConstants.FormSwapStartupFrames + GameConstants.FormSwapActiveFrames + 6);
            results.Add((
                "Swapped to explicit non-next form (Goku)",
                actor.CurrentForm.Id == "goku_archive_form"));

            // Clear the swap cooldown, then confirm a non-equipped form is rejected.
            Step(GameConstants.DefaultFormSwapCooldownFrames + 8);
            actor.StateMachine.RequestFormSwap("totally_not_a_form");
            Step(GameConstants.FormSwapStartupFrames + GameConstants.FormSwapActiveFrames + 6);
            results.Add((
                "Rejected swap to a non-equipped form",
                actor.CurrentForm.Id == "goku_archive_form"));

            actor.Free();
        }
        catch (System.Exception exception)
        {
            results.Add(($"Unexpected exception: {exception.Message}", false));
        }

        var passed = results.Count > 0 && results.All(result => result.Passed);
        var builder = new StringBuilder();
        builder.AppendLine(
            $"[FormSelectTests] SUMMARY passed={passed} "
            + $"({results.Count(result => result.Passed)}/{results.Count})");
        foreach (var (name, ok) in results)
        {
            builder.AppendLine($"  [{(ok ? "PASS" : "FAIL")}] {name}");
        }

        return builder.ToString();
    }
}
