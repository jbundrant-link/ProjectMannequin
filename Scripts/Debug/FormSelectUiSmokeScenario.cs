using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Stage;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

public partial class FormSelectUiSmokeScenario : Node
{
    private GameSimulation? _simulation;
    private FormSelectOverlay? _overlay;
    private CombatActor? _player;
    private int _phase;
    private int _frames;
    private int _phaseStartedFrame;
    private bool _opened;
    private bool _currentSelected;
    private bool _gokuSelected;
    private bool _cancelled;
    private bool _reopened;
    private bool _confirmed;
    private bool _swapped;
    private bool _styledFrame;

    public void Initialize(GameSimulation simulation, FormSelectOverlay overlay)
    {
        _simulation = simulation;
        _overlay = overlay;
        ProcessMode = ProcessModeEnum.Always;
        PrepareCaptureOutput("PROJECT_MANNEQUIN_FORM_SELECT_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_FORM_SELECT_SELECTED_CAPTURE");
        GD.Print("[FormSelectUiSmoke] Driver active.");
    }

    public override void _Process(double delta)
    {
        if (_simulation is null || _overlay is null)
        {
            return;
        }

        _frames++;
        switch (_phase)
        {
            case 0:
                _player = _simulation.Actors.FirstOrDefault(actor =>
                    actor.IsPlayerControlled && actor.PlayerId == 1);
                if (_player is null)
                {
                    return;
                }

                if (_simulation.EncounterDirector?.State == ArcadeStageState.StageIntro)
                {
                    return;
                }

                _player.IsVulnerable = false;
                _player.State = CombatActorState.Idle;
                _player.FormArchive.UnlockForm(TestRosterFactory.CreateWorldWarriorRyuForm());
                _player.FormArchive.UnlockForm(GokuRosterFactory.CreateGokuArchiveForm());
                _player.FormArchive.SetActiveLoadout(new[]
                {
                    "blank_mannequin",
                    "world_warrior_ryu_form",
                    "goku_archive_form",
                });
                _player.AddMeter(_player.CurrentForm.MaxMeter);
                SendKey(Key.Q);
                AdvancePhase();
                break;
            case 1:
                if (!Waited(6)) return;
                _opened = _overlay.IsOpen && GetTree().Paused && _overlay.EntryCount == 3;
                _currentSelected = _overlay.SelectedFormId == "blank_mannequin";
                _styledFrame = ResourceLoader.Exists(_overlay.FormSelectFramePath)
                    && Descendants<TextureRect>(_overlay).Any(texture =>
                        texture.Name == "FormSelectFrame");
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_FORM_SELECT_CAPTURE");
                SendKey(Key.Right);
                AdvancePhase();
                break;
            case 2:
                if (!Waited(2)) return;
                SendKey(Key.Right);
                AdvancePhase();
                break;
            case 3:
                if (!Waited(6)) return;
                _gokuSelected = _overlay.SelectedFormId == "goku_archive_form"
                    && _overlay.PreviewName.Contains("Goku", System.StringComparison.OrdinalIgnoreCase);
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_FORM_SELECT_SELECTED_CAPTURE");
                SendKey(Key.Q);
                AdvancePhase();
                break;
            case 4:
                if (!Waited(3)) return;
                _cancelled = !_overlay.IsOpen
                    && !GetTree().Paused
                    && _player!.CurrentForm.Id == "blank_mannequin";
                SendKey(Key.Q);
                AdvancePhase();
                break;
            case 5:
                if (!Waited(4)) return;
                _reopened = _overlay.IsOpen
                    && GetTree().Paused
                    && _overlay.SelectedFormId == "blank_mannequin";
                SendKey(Key.Right);
                AdvancePhase();
                break;
            case 6:
                if (!Waited(2)) return;
                SendKey(Key.Right);
                AdvancePhase();
                break;
            case 7:
                if (!Waited(2)) return;
                SendKey(Key.Enter);
                _confirmed = !_overlay.IsOpen && !GetTree().Paused;
                if (_confirmed)
                {
                    _player!.State = CombatActorState.Idle;
                    var interpreter = new CommandInterpreter();
                    for (var offset = 1;
                         offset <= GameConstants.FormSwapStartupFrames
                             + GameConstants.FormSwapActiveFrames + 1;
                         offset++)
                    {
                        _player.StateMachine.Update(
                            _simulation.CurrentTick + offset,
                            input: null,
                            interpreter,
                            _simulation.Actors);
                    }
                }

                AdvancePhase();
                break;
            case 8:
                _swapped = _player!.CurrentForm.Id == "goku_archive_form";
                if (_swapped || Waited(GameConstants.FormSwapStartupFrames
                        + GameConstants.FormSwapActiveFrames + 30))
                {
                    Finish();
                }
                break;
        }

        if (_frames >= 360)
        {
            GD.PushError($"[FormSelectUiSmoke] Timed out phase={_phase}.");
            GetTree().Paused = false;
            GetTree().Quit();
        }
    }

    private void Finish()
    {
        var captures = CaptureExistsIfRequested("PROJECT_MANNEQUIN_FORM_SELECT_CAPTURE")
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_FORM_SELECT_SELECTED_CAPTURE");
        var passed = _opened
            && _currentSelected
            && _gokuSelected
            && _cancelled
            && _reopened
            && _confirmed
            && _swapped
            && _styledFrame
            && captures;
        GD.Print(
            $"[FormSelectUiSmoke] SUMMARY passed={passed} opened={_opened} "
            + $"current={_currentSelected} goku={_gokuSelected} cancel={_cancelled} "
            + $"reopen={_reopened} confirm={_confirmed} swapped={_swapped} "
            + $"styled={_styledFrame} captures={captures}");
        if (!passed)
        {
            GD.PushError("[FormSelectUiSmoke] Form-select UI or deterministic swap regressed.");
        }

        GetTree().Paused = false;
        GetTree().Quit();
    }

    private void SendKey(Key key)
    {
        _overlay!._Input(new InputEventKey
        {
            Pressed = true,
            PhysicalKeycode = key,
        });
    }

    private void AdvancePhase()
    {
        _phase++;
        _phaseStartedFrame = _frames;
    }

    private bool Waited(int frames) => _frames >= _phaseStartedFrame + frames;

    private void CaptureFrameIfRequested(string environmentVariable)
    {
        var path = OS.GetEnvironment(environmentVariable);
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
            GD.PushError($"[FormSelectUiSmoke] Could not save capture '{path}' ({error}).");
        }
    }

    private static void PrepareCaptureOutput(string environmentVariable)
    {
        var path = OS.GetEnvironment(environmentVariable);
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static bool CaptureExistsIfRequested(string environmentVariable)
    {
        var path = OS.GetEnvironment(environmentVariable);
        return string.IsNullOrWhiteSpace(path) || File.Exists(path);
    }

    private static System.Collections.Generic.IEnumerable<T> Descendants<T>(Node root)
        where T : Node
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