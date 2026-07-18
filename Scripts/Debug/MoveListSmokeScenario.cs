using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// End-to-end regression for the pause-menu Move List restored from the Ryu
/// translation session. It verifies keyboard pause routing, live Ryu rows,
/// actual S/C command labels, nested Escape behavior, and resume.
/// </summary>
public partial class MoveListSmokeScenario : Node
{
    private GameSimulation? _simulation;
    private PauseMenu? _pauseMenu;
    private int _phase;
    private int _processFrames;
    private int _phaseStartedFrame;
    private bool _pauseOpened;
    private bool _moveListButtonWorked;
    private bool _titleCorrect;
    private bool _hadoukenListed;
    private bool _hadoukenCommandCorrect;
    private bool _crouchCommandCorrect;
    private bool _legendCorrect;
    private bool _loadoutWorked;
    private bool _formLoadoutWorked;
    private bool _artifactsWorked;
    private bool _styledPlate;
    private bool _backReturnedToPause;
    private bool _resumed;

    public void Initialize(GameSimulation simulation, PauseMenu pauseMenu)
    {
        _simulation = simulation;
        _pauseMenu = pauseMenu;
        ProcessMode = ProcessModeEnum.Always;
        PrepareCaptureOutput("PROJECT_MANNEQUIN_PAUSE_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_MOVE_LIST_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_MOVE_CARDS_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_FORM_LOADOUT_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_ARTIFACTS_CAPTURE");
        GD.Print("[MoveListSmoke] Driver active.");
    }

    public override void _Process(double delta)
    {
        if (_simulation is null || _pauseMenu is null)
        {
            return;
        }

        _processFrames++;
        switch (_phase)
        {
            case 0:
                var player = _simulation.Actors.FirstOrDefault(actor =>
                    actor.IsPlayerControlled && actor.PlayerId == 1);
                if (player is null)
                {
                    return;
                }

                player.SetForm(TestRosterFactory.CreateWorldWarriorRyuForm(), resetHealth: true);
                SendEscape();
                _phase = 1;
                _phaseStartedFrame = _processFrames;
                break;
            case 1:
                if (_processFrames < _phaseStartedFrame + 6)
                {
                    return;
                }

                var pausePanel = FindByName<Control>(_pauseMenu, "PausePanel");
                _pauseOpened = GetTree().Paused
                    && pausePanel?.Visible == true
                    && _pauseMenu.Layer >= 30;
                _styledPlate = ResourceLoader.Exists(_pauseMenu.PausePlatePath)
                    && Descendants<TextureRect>(_pauseMenu).Any(texture =>
                        texture.Name == "PausePlate");
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_PAUSE_CAPTURE");
                var moveListButton = Descendants<Button>(_pauseMenu)
                    .FirstOrDefault(button => button.Text == "Move List");
                moveListButton?.EmitSignal(BaseButton.SignalName.Pressed);
                _phase = 2;
                _phaseStartedFrame = _processFrames;
                break;
            case 2:
                if (_processFrames < _phaseStartedFrame + 6)
                {
                    return;
                }

                CaptureMoveList();
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_MOVE_LIST_CAPTURE");
                SendEscape();
                _phase = 3;
                _phaseStartedFrame = _processFrames;
                break;
            case 3:
                if (_processFrames < _phaseStartedFrame + 2)
                {
                    return;
                }

                var moveListPanel = FindByName<Control>(_pauseMenu, "MoveListPanel");
                var rootPanel = FindByName<Control>(_pauseMenu, "PausePanel");
                _backReturnedToPause = GetTree().Paused
                    && moveListPanel?.Visible == false
                    && rootPanel?.Visible == true;
                PressButton("Move Cards");
                _phase = 4;
                _phaseStartedFrame = _processFrames;
                break;
            case 4:
                if (_processFrames < _phaseStartedFrame + 6)
                {
                    return;
                }

                _loadoutWorked = FindByName<Control>(_pauseMenu, "LoadoutPanel")?.Visible == true;
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_MOVE_CARDS_CAPTURE");
                SendEscape();
                _phase = 5;
                _phaseStartedFrame = _processFrames;
                break;
            case 5:
                if (_processFrames < _phaseStartedFrame + 2)
                {
                    return;
                }

                PressButton("Form Loadout");
                _phase = 6;
                _phaseStartedFrame = _processFrames;
                break;
            case 6:
                if (_processFrames < _phaseStartedFrame + 6)
                {
                    return;
                }

                _formLoadoutWorked = FindByName<Control>(_pauseMenu, "FormLoadoutPanel")?.Visible == true;
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_FORM_LOADOUT_CAPTURE");
                SendEscape();
                _phase = 7;
                _phaseStartedFrame = _processFrames;
                break;
            case 7:
                if (_processFrames < _phaseStartedFrame + 2)
                {
                    return;
                }

                PressButton("Artifacts");
                _phase = 8;
                _phaseStartedFrame = _processFrames;
                break;
            case 8:
                if (_processFrames < _phaseStartedFrame + 6)
                {
                    return;
                }

                _artifactsWorked = FindByName<Control>(_pauseMenu, "ArtifactsPanel")?.Visible == true;
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_ARTIFACTS_CAPTURE");
                SendEscape();
                _phase = 9;
                _phaseStartedFrame = _processFrames;
                break;
            case 9:
                if (_processFrames < _phaseStartedFrame + 2)
                {
                    return;
                }

                SendEscape();
                _phase = 10;
                break;
            case 10:
                var panel = FindByName<Control>(_pauseMenu, "PausePanel");
                _resumed = !GetTree().Paused && panel?.Visible == false;
                Finish();
                break;
        }

        if (_processFrames >= 180)
        {
            GD.PushError($"[MoveListSmoke] Timed out in phase={_phase}.");
            GetTree().Paused = false;
            GetTree().Quit();
        }
    }

    private void CaptureMoveList()
    {
        var panel = FindByName<Control>(_pauseMenu!, "MoveListPanel");
        var labels = panel is null
            ? new List<string>()
            : Descendants<Label>(panel).Select(label => label.Text.Trim()).ToList();
        var hadoukenCommand = KeyboardCommandFormatter.ToKeyboard("236LP");
        var crouchCommand = KeyboardCommandFormatter.ToKeyboard("2LP");

        _moveListButtonWorked = panel?.Visible == true;
        _titleCorrect = labels.Any(text => text.StartsWith("MOVE LIST", System.StringComparison.Ordinal)
            && text.Contains("RYU", System.StringComparison.OrdinalIgnoreCase));
        _hadoukenListed = labels.Contains("Light Hadouken");
        _hadoukenCommandCorrect = hadoukenCommand == "↓ ↘ →  +  J"
            && labels.Contains(hadoukenCommand);
        _crouchCommandCorrect = crouchCommand == "C  +  J"
            && labels.Contains(crouchCommand);
        _legendCorrect = labels.Any(text => text.Contains("↓ = S", System.StringComparison.Ordinal)
            && text.Contains("Crouching normals = C", System.StringComparison.Ordinal));
    }

    private void Finish()
    {
        var passed = _pauseOpened
            && _moveListButtonWorked
            && _titleCorrect
            && _hadoukenListed
            && _hadoukenCommandCorrect
            && _crouchCommandCorrect
            && _legendCorrect
            && _loadoutWorked
            && _formLoadoutWorked
            && _artifactsWorked
            && _styledPlate
            && _backReturnedToPause
            && _resumed
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_PAUSE_CAPTURE")
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_MOVE_LIST_CAPTURE")
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_MOVE_CARDS_CAPTURE")
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_FORM_LOADOUT_CAPTURE")
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_ARTIFACTS_CAPTURE");
        GD.Print(
            $"[MoveListSmoke] SUMMARY passed={passed} pause={_pauseOpened} "
            + $"button={_moveListButtonWorked} title={_titleCorrect} hadouken={_hadoukenListed} "
            + $"motionLabel={_hadoukenCommandCorrect} crouchLabel={_crouchCommandCorrect} "
            + $"legend={_legendCorrect} cards={_loadoutWorked} forms={_formLoadoutWorked} "
            + $"artifacts={_artifactsWorked} styled={_styledPlate} "
            + $"back={_backReturnedToPause} resumed={_resumed}");
        if (!passed)
        {
            GD.PushError("[MoveListSmoke] Pause Move List or command labels regressed.");
        }

        GetTree().Paused = false;
        GetTree().Quit();
    }

    private void PressButton(string text)
    {
        Descendants<Button>(_pauseMenu!)
            .FirstOrDefault(button => button.Text == text)
            ?.EmitSignal(BaseButton.SignalName.Pressed);
    }

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
            GD.PushError($"[MoveListSmoke] Could not save capture '{path}' ({error}).");
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

    private void SendEscape()
    {
        _pauseMenu!._UnhandledInput(new InputEventKey
        {
            Pressed = true,
            PhysicalKeycode = Key.Escape,
        });
    }

    private static T? FindByName<T>(Node root, string name) where T : Node
    {
        return Descendants<T>(root).FirstOrDefault(node => node.Name == name);
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
