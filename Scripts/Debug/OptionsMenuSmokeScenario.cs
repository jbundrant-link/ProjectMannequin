using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Settings;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// End-to-end regression for the pause-menu Options surface.
/// </summary>
/// <remarks>
/// <see cref="OptionsModel"/> is unit-tested, so this scenario covers only what
/// unit tests cannot: that the overlay is reachable from pause, renders its
/// rows, actually owns input while it is up, and commits to the store on close.
/// </remarks>
public partial class OptionsMenuSmokeScenario : Node
{
    private PauseMenu? _pauseMenu;
    private OptionsMenu? _optionsMenu;
    private int _phase;
    private int _processFrames;
    private int _phaseStartedFrame;
    private bool _pauseOpened;
    private bool _optionsOpened;
    private bool _pausePlateHidden;
    private bool _rowsRendered;
    private bool _selectionMoved;
    private bool _adjustChangedValue;
    private bool _closedBackToPause;
    private bool _eraseArmedLabelShown;
    private bool _eraseDisarmedOnNavigate;
    private bool _appliedToStore;
    private float _volumeBeforeAdjust;

    public void Initialize(PauseMenu pauseMenu)
    {
        _pauseMenu = pauseMenu;
        ProcessMode = ProcessModeEnum.Always;
        PrepareCaptureOutput("PROJECT_MANNEQUIN_OPTIONS_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_OPTIONS_ERASE_CAPTURE");
        GD.Print("[OptionsSmoke] Driver active.");
    }

    public override void _Process(double delta)
    {
        if (_pauseMenu is null)
        {
            return;
        }

        _processFrames++;
        switch (_phase)
        {
            case 0:
                SendEscape();
                Advance();
                break;

            case 1:
                if (!Settled(6))
                {
                    return;
                }

                _pauseOpened = GetTree().Paused
                    && FindByName<Control>(_pauseMenu, "PausePanel")?.Visible == true;
                PressButton("Options");
                Advance();
                break;

            case 2:
                if (!Settled(6))
                {
                    return;
                }

                _optionsMenu = Descendants<OptionsMenu>(_pauseMenu).FirstOrDefault();
                _optionsOpened = _optionsMenu?.IsOpen == true
                    && FindByName<Control>(_pauseMenu, "OptionsRoot")?.Visible == true;
                _pausePlateHidden =
                    FindByName<Control>(_pauseMenu, "PausePanel")?.Visible == false;

                var rowContainer = FindByName<VBoxContainer>(_pauseMenu, "Rows");
                _rowsRendered = rowContainer is not null
                    && rowContainer.GetChildCount() > 0
                    && Descendants<Label>(rowContainer).Any(label =>
                        label.Text == "Master Volume");

                CaptureFrameIfRequested("PROJECT_MANNEQUIN_OPTIONS_CAPTURE");
                _volumeBeforeAdjust = SettingsStore.Current.MasterVolume;

                // Down then Left must reach the overlay, not the pause buttons
                // sitting behind it.
                SendKey(Key.Down);
                Advance();
                break;

            case 3:
                if (!Settled(3))
                {
                    return;
                }

                _selectionMoved = FirstSelectedLabel() != "Master Volume";
                SendKey(Key.Left);
                Advance();
                break;

            case 4:
                if (!Settled(3))
                {
                    return;
                }

                // Row 2 is Music Volume; Left must have stepped it down.
                _adjustChangedValue = ValueTextFor("Music Volume") is { } text
                    && text != "100%";

                // Up from the top wraps straight onto the destructive row,
                // which is last, so this needs two presses rather than fifteen.
                SendKey(Key.Up);
                SendKey(Key.Up);
                SendKey(Key.Enter);
                Advance();
                break;

            case 5:
                if (!Settled(4))
                {
                    return;
                }

                _eraseArmedLabelShown = SelectedRowLabel() is { } armedLabel
                    && armedLabel.Contains("CONFIRM");
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_OPTIONS_ERASE_CAPTURE");

                // Navigating away must visibly disarm it again.
                SendKey(Key.Down);
                Advance();
                break;

            case 6:
                if (!Settled(3))
                {
                    return;
                }

                _eraseDisarmedOnNavigate = _rowLabels().All(label => !label.Contains("CONFIRM"));
                SendKey(Key.Escape);
                Advance();
                break;

            case 7:
                if (!Settled(6))
                {
                    return;
                }

                _closedBackToPause = _optionsMenu?.IsOpen == false
                    && FindByName<Control>(_pauseMenu, "PausePanel")?.Visible == true
                    && GetTree().Paused;
                _appliedToStore =
                    SettingsStore.Current.MusicVolume < _volumeBeforeAdjust;
                Finish();
                break;
        }
    }

    private void Advance()
    {
        _phase++;
        _phaseStartedFrame = _processFrames;
    }

    private bool Settled(int frames) => _processFrames >= _phaseStartedFrame + frames;

    private System.Collections.Generic.IEnumerable<string> _rowLabels()
    {
        var rowContainer = FindByName<VBoxContainer>(_pauseMenu!, "Rows");
        if (rowContainer is null)
        {
            yield break;
        }

        foreach (var row in rowContainer.GetChildren().OfType<PanelContainer>())
        {
            var label = Descendants<Label>(row).FirstOrDefault()?.Text;
            if (label is not null)
            {
                yield return label;
            }
        }
    }

    private string? SelectedRowLabel() => FirstSelectedLabel();

    private string? FirstSelectedLabel()
    {
        // The selected row is the only one carrying a highlight stylebox.
        var rowContainer = FindByName<VBoxContainer>(_pauseMenu!, "Rows");
        if (rowContainer is null)
        {
            return null;
        }

        foreach (var row in rowContainer.GetChildren().OfType<PanelContainer>())
        {
            if (row.HasThemeStyleboxOverride("panel"))
            {
                return Descendants<Label>(row).FirstOrDefault()?.Text;
            }
        }

        return null;
    }

    private string? ValueTextFor(string label)
    {
        var rowContainer = FindByName<VBoxContainer>(_pauseMenu!, "Rows");
        if (rowContainer is null)
        {
            return null;
        }

        foreach (var row in rowContainer.GetChildren().OfType<PanelContainer>())
        {
            var labels = Descendants<Label>(row).ToList();
            if (labels.Count >= 2 && labels[0].Text == label)
            {
                return labels[1].Text;
            }
        }

        return null;
    }

    private void Finish()
    {
        var passed = _pauseOpened
            && _optionsOpened
            && _pausePlateHidden
            && _rowsRendered
            && _selectionMoved
            && _adjustChangedValue
            && _eraseArmedLabelShown
            && _eraseDisarmedOnNavigate
            && _closedBackToPause
            && _appliedToStore
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_OPTIONS_CAPTURE")
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_OPTIONS_ERASE_CAPTURE");
        GD.Print(
            $"[OptionsSmoke] SUMMARY passed={passed} pause={_pauseOpened} "
            + $"opened={_optionsOpened} plateHidden={_pausePlateHidden} "
            + $"rows={_rowsRendered} moved={_selectionMoved} "
            + $"adjusted={_adjustChangedValue} eraseArmed={_eraseArmedLabelShown} "
            + $"eraseDisarmed={_eraseDisarmedOnNavigate} "
            + $"closed={_closedBackToPause} applied={_appliedToStore}");
        if (!passed)
        {
            GD.PushError("[OptionsSmoke] Pause Options surface regressed.");
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

    private void SendEscape() => SendKey(Key.Escape);

    private void SendKey(Key key)
    {
        var keyEvent = new InputEventKey
        {
            Pressed = true,
            PhysicalKeycode = key,
        };

        // Mirror real dispatch order: the overlay taps _Input first and marks
        // the event handled whenever it is up, so the pause menu only sees
        // events raised while the overlay was already closed.
        var optionsMenu = _optionsMenu
            ?? Descendants<OptionsMenu>(_pauseMenu!).FirstOrDefault();
        var overlayOwnedEvent = optionsMenu?.IsOpen == true;
        optionsMenu?._Input(keyEvent);
        if (!overlayOwnedEvent)
        {
            _pauseMenu!._UnhandledInput(keyEvent);
        }
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
            GD.PushError($"[OptionsSmoke] Could not save capture '{path}' ({error}).");
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
