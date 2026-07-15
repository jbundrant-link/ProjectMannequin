using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Progression;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Runtime results-modal lock for stage clear, world complete, and game over.
/// Verifies rank/tally/buttons, safe focus, confirmations, one-shot next-stage
/// commits, and immediate final checkpoint retirement.
/// </summary>
public partial class ResultsFlowSmokeScenario : Node
{
    private MvpHud? _hud;
    private ResultsFlowMode _mode;
    private string _variant = "stage";
    private int _frames;
    private int _phase;
    private int _shownFrame;
    private bool _visible;
    private bool _modeCorrect;
    private bool _rank;
    private bool _buttons;
    private bool _focus;
    private bool _route;
    private bool _confirmation;
    private bool _cancel;
    private bool _unlock;

    public void Initialize(MvpHud hud)
    {
        _hud = hud;
        ProcessMode = ProcessModeEnum.Always;
        _variant = OS.GetEnvironment("PROJECT_MANNEQUIN_RESULTS_FLOW_MODE").Trim().ToLowerInvariant();
        _mode = ParseMode(_variant);
        GD.Print($"[ResultsFlowSmoke] Driver active mode={_mode} variant={_variant}.");
    }

    public override void _Process(double delta)
    {
        if (_hud is null)
        {
            return;
        }

        _frames++;
        switch (_phase)
        {
            case 0:
                var captureRequested = !string.IsNullOrWhiteSpace(
                    OS.GetEnvironment("PROJECT_MANNEQUIN_RESULTS_FLOW_CAPTURE"));
                if (_frames < (captureRequested ? 130 : 4))
                {
                    return;
                }

                _hud.DebugShowResultsForSmoke(_mode);
                _shownFrame = _frames;
                _phase = 1;
                break;
            case 1:
                var visualCapture = !string.IsNullOrWhiteSpace(
                    OS.GetEnvironment("PROJECT_MANNEQUIN_RESULTS_FLOW_CAPTURE"));
                if (_frames < _shownFrame + (visualCapture ? 25 : 5))
                {
                    return;
                }

                CaptureModal();
                CaptureFrameIfRequested();
                DriveAction();
                _phase = 2;
                break;
            case 2:
                if (_frames < _shownFrame + 8)
                {
                    return;
                }

                CaptureActionOutcome();
                Finish();
                break;
        }

        if (_frames >= 240)
        {
            GD.PushError($"[ResultsFlowSmoke] Timed out phase={_phase} mode={_mode}.");
            GetTree().Quit();
        }
    }

    private void CaptureModal()
    {
        _visible = _hud!.ResultsPanelVisible;
        _modeCorrect = _hud.ResultsMode == _mode;
        _rank = _mode == ResultsFlowMode.GameOver
            ? _hud.ResultsRankText == "KO"
            : _hud.ResultsRankText == "S";
        var expected = ResultsFlowModel.ActionsFor(_mode).Select(action => action.Label).ToArray();
        _buttons = _hud.ResultsButtonTexts.SequenceEqual(expected);
        var primary = ResultsFlowModel.PrimaryActionFor(_mode);
        _focus = primary is not null
            && GetViewport().GuiGetFocusOwner()?.Name == $"ResultsAction_{primary.Value}";
        _unlock = _mode != ResultsFlowMode.WorldComplete
            || _hud.ResultsUnlockText.StartsWith("FORM ARCHIVED");
    }

    private void DriveAction()
    {
        switch (_mode)
        {
            case ResultsFlowMode.StageClear:
                var stageAction = _variant switch
                {
                    "stage_map" => ResultsAction.ArchiveMap,
                    "stage_retry" => ResultsAction.RetryStage,
                    _ => ResultsAction.NextStage,
                };
                var stageButton = FindButton($"ResultsAction_{stageAction}");
                stageButton?.EmitSignal(BaseButton.SignalName.Pressed);
                stageButton?.EmitSignal(BaseButton.SignalName.Pressed);
                break;
            case ResultsFlowMode.WorldComplete:
                FindButton(_variant == "world_map"
                        ? "ResultsAction_ArchiveMap"
                        : "ResultsAction_ReplayWorld")
                    ?.EmitSignal(BaseButton.SignalName.Pressed);
                break;
            case ResultsFlowMode.GameOver:
                FindButton(_variant == "gameover_retry"
                        ? "ResultsAction_RetryStage"
                        : "ResultsAction_RestartWorld")
                    ?.EmitSignal(BaseButton.SignalName.Pressed);
                break;
        }
    }

    private void CaptureActionOutcome()
    {
        switch (_mode)
        {
            case ResultsFlowMode.StageClear:
                var expectedStage = _variant == "stage_retry" ? 0 : 1;
                _route = RunSessionManager.Instance.CurrentStageIndex == expectedStage
                    && _hud!.ResultsActionCommitted
                    && SceneFlowCoordinator.Instance?.IsTransitioning == true;
                _confirmation = true;
                _cancel = true;
                break;
            case ResultsFlowMode.WorldComplete:
                _route = !RunSessionManager.Instance.HasActiveRun;
                if (_variant == "world_map")
                {
                    _confirmation = true;
                    _cancel = SceneFlowCoordinator.Instance?.IsTransitioning == true;
                }
                else
                {
                    _confirmation = _hud!.ResultsConfirmationVisible;
                    _focus = _focus && GetViewport().GuiGetFocusOwner()?.Name == "ResultsConfirmCancel";
                    FindButton("ResultsConfirmCancel")?.EmitSignal(BaseButton.SignalName.Pressed);
                    _cancel = !_hud.ResultsConfirmationVisible;
                }
                break;
            case ResultsFlowMode.GameOver:
                _route = RunSessionManager.Instance.HasActiveRun;
                if (_variant == "gameover_retry")
                {
                    _confirmation = true;
                    _cancel = _hud!.ResultsActionCommitted
                        && SceneFlowCoordinator.Instance?.IsTransitioning == true;
                }
                else
                {
                    _confirmation = _hud!.ResultsConfirmationVisible;
                    _focus = _focus && GetViewport().GuiGetFocusOwner()?.Name == "ResultsConfirmCancel";
                    FindButton("ResultsConfirmCancel")?.EmitSignal(BaseButton.SignalName.Pressed);
                    _cancel = !_hud.ResultsConfirmationVisible;
                }
                break;
        }
    }

    private void Finish()
    {
        var passed = _visible
            && _modeCorrect
            && _rank
            && _buttons
            && _focus
            && _route
            && _confirmation
            && _cancel
            && _unlock;
        GD.Print(
            $"[ResultsFlowSmoke] SUMMARY passed={passed} mode={_mode} variant={_variant} visible={_visible} "
            + $"modeOk={_modeCorrect} rank={_rank} buttons={_buttons} focus={_focus} "
            + $"route={_route} confirm={_confirmation} cancel={_cancel} unlock={_unlock}");
        if (!passed)
        {
            GD.PushError("[ResultsFlowSmoke] Results modal or transactional routing regressed.");
        }

        GetTree().Quit();
    }

    private Button? FindButton(string name)
    {
        return Descendants<Button>(_hud!).FirstOrDefault(button => button.Name == name);
    }

    private void CaptureFrameIfRequested()
    {
        var path = OS.GetEnvironment("PROJECT_MANNEQUIN_RESULTS_FLOW_CAPTURE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok)
        {
            GD.PushError($"[ResultsFlowSmoke] Could not save capture '{path}' ({error}).");
        }
    }

    private static ResultsFlowMode ParseMode(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "world" or "world_map" => ResultsFlowMode.WorldComplete,
            "gameover" or "gameover_retry" => ResultsFlowMode.GameOver,
            _ => ResultsFlowMode.StageClear,
        };
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in Descendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
