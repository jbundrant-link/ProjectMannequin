using System.Linq;
using System.Text;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

public static class ResultsFlowTests
{
    public static string Run()
    {
        var log = new StringBuilder();
        var passed = 0;
        var failed = 0;

        void Check(bool condition, string label)
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
            }
        }

        log.AppendLine("=== Results Flow Tests ===");
        var stage = ResultsFlowModel.ActionsFor(ResultsFlowMode.StageClear);
        Check(stage.Select(action => action.Action).SequenceEqual(new[]
            {
                ResultsAction.NextStage,
                ResultsAction.RetryStage,
                ResultsAction.ArchiveMap,
            }), "stage clear exposes next retry and map actions");
        Check(ResultsFlowModel.PrimaryActionFor(ResultsFlowMode.StageClear) == ResultsAction.NextStage,
            "next stage is the stage-clear default");
        Check(ResultsFlowModel.CommitsNextStageBeforeMap(ResultsFlowMode.StageClear),
            "stage-clear map exit commits the next checkpoint");

        var world = ResultsFlowModel.ActionsFor(ResultsFlowMode.WorldComplete);
        Check(world.Select(action => action.Action).SequenceEqual(new[]
            {
                ResultsAction.ArchiveMap,
                ResultsAction.ReplayWorld,
            }), "world complete exposes map and replay actions");
        Check(ResultsFlowModel.PrimaryActionFor(ResultsFlowMode.WorldComplete) == ResultsAction.ArchiveMap,
            "archive map is the safe world-complete default");
        Check(world.Single(action => action.Action == ResultsAction.ReplayWorld).RequiresConfirmation,
            "world replay requires confirmation");
        Check(ResultsFlowModel.CompletesRunOnReveal(ResultsFlowMode.WorldComplete),
            "final results immediately retire the committed run checkpoint");

        var gameOver = ResultsFlowModel.ActionsFor(ResultsFlowMode.GameOver);
        Check(gameOver.Select(action => action.Action).SequenceEqual(new[]
            {
                ResultsAction.RetryStage,
                ResultsAction.RestartWorld,
                ResultsAction.ArchiveMap,
            }), "game over exposes retry restart and map actions");
        Check(ResultsFlowModel.PrimaryActionFor(ResultsFlowMode.GameOver) == ResultsAction.RetryStage,
            "retry stage is the game-over default");
        Check(gameOver.Single(action => action.Action == ResultsAction.RestartWorld).RequiresConfirmation,
            "restart world requires confirmation");
        Check(!ResultsFlowModel.CommitsNextStageBeforeMap(ResultsFlowMode.GameOver),
            "game-over map exit preserves current stage checkpoint");
        Check(ResultsFlowModel.ActionsFor(ResultsFlowMode.Hidden).Count == 0,
            "hidden state exposes no actions");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }
}
