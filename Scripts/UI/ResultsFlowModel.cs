using System.Collections.Generic;

namespace ProjectMannequin.UI;

public enum ResultsFlowMode
{
    Hidden,
    StageClear,
    WorldComplete,
    GameOver,
}

public enum ResultsAction
{
    NextStage,
    RetryStage,
    ArchiveMap,
    ReplayWorld,
    RestartWorld,
}

public sealed record ResultsActionSpec(
    ResultsAction Action,
    string Label,
    bool IsPrimary,
    bool RequiresConfirmation = false);

/// <summary>
/// Pure action grammar for end-state modals. Routing code consumes these specs
/// so keyboard, gamepad, mouse, and smoke tests all expose the same safe verbs.
/// </summary>
public static class ResultsFlowModel
{
    private static readonly IReadOnlyList<ResultsActionSpec> StageClearActions =
        new[]
        {
            new ResultsActionSpec(ResultsAction.NextStage, "NEXT STAGE", true),
            new ResultsActionSpec(ResultsAction.RetryStage, "RETRY STAGE", false),
            new ResultsActionSpec(ResultsAction.ArchiveMap, "ARCHIVE MAP", false),
        };

    private static readonly IReadOnlyList<ResultsActionSpec> WorldCompleteActions =
        new[]
        {
            new ResultsActionSpec(ResultsAction.ArchiveMap, "ARCHIVE MAP", true),
            new ResultsActionSpec(ResultsAction.ReplayWorld, "REPLAY WORLD", false, true),
        };

    private static readonly IReadOnlyList<ResultsActionSpec> GameOverActions =
        new[]
        {
            new ResultsActionSpec(ResultsAction.RetryStage, "RETRY STAGE", true),
            new ResultsActionSpec(ResultsAction.RestartWorld, "RESTART WORLD", false, true),
            new ResultsActionSpec(ResultsAction.ArchiveMap, "ARCHIVE MAP", false),
        };

    public static IReadOnlyList<ResultsActionSpec> ActionsFor(ResultsFlowMode mode)
    {
        return mode switch
        {
            ResultsFlowMode.StageClear => StageClearActions,
            ResultsFlowMode.WorldComplete => WorldCompleteActions,
            ResultsFlowMode.GameOver => GameOverActions,
            _ => System.Array.Empty<ResultsActionSpec>(),
        };
    }

    public static ResultsAction? PrimaryActionFor(ResultsFlowMode mode)
    {
        foreach (var action in ActionsFor(mode))
        {
            if (action.IsPrimary)
            {
                return action.Action;
            }
        }

        return null;
    }

    public static bool CommitsNextStageBeforeMap(ResultsFlowMode mode)
    {
        return mode == ResultsFlowMode.StageClear;
    }

    public static bool CompletesRunOnReveal(ResultsFlowMode mode)
    {
        return mode == ResultsFlowMode.WorldComplete;
    }
}
