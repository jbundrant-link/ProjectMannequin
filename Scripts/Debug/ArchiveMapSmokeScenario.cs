using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Progression;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Runtime UI lock for the Archive Map: core realm cards, four-stage route,
/// locked realm state, active checkpoint projection, and safe overwrite modal.
/// It never confirms replacement, so no user save is modified.
/// </summary>
public partial class ArchiveMapSmokeScenario : Node
{
    private MainMenu? _menu;
    private int _phase;
    private int _frames;
    private bool _coreWorldCards;
    private bool _trainingExcluded;
    private bool _lockedRealm;
    private bool _fourStageRoute;
    private bool _worldSelection;
    private bool _activeRoute;
    private bool _overwriteShown;
    private bool _safeDefaultFocus;
    private bool _cancelRestored;
    private bool _styledBackground;

    public void Initialize(MainMenu menu)
    {
        _menu = menu;
        ProcessMode = ProcessModeEnum.Always;
        PrepareCaptureOutput("PROJECT_MANNEQUIN_ARCHIVE_MAP_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_ARCHIVE_MAP_WORLD_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_ARCHIVE_MAP_CONFIRM_CAPTURE");
        GD.Print("[ArchiveMapSmoke] Driver active.");
    }

    public override void _Process(double delta)
    {
        if (_menu is null)
        {
            return;
        }

        _frames++;
        switch (_phase)
        {
            case 0:
                if (_frames < 6)
                {
                    return;
                }

                CaptureInitialMap();
                CaptureFrameIfRequested();
                _menu.SelectWorld("world_warrior_sector");
                _phase = 1;
                break;
            case 1:
                _worldSelection = _menu.SelectedWorldId == "world_warrior_sector"
                    && _menu.RenderedStageCount == 4
                    && Descendants<Label>(_menu).Any(label =>
                        label.Text.Contains("PAVILION CIRCUIT"));
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_ARCHIVE_MAP_WORLD_CAPTURE");
                var session = RunSessionManager.Instance;
                session.CurrentWorldId = "archive_nexus";
                session.CurrentStageIndex = 1;
                session.RemainingLives = 2;
                session.RunScore = 12345;
                _menu.SelectWorld("archive_nexus");
                _phase = 2;
                break;
            case 2:
                var labels = Descendants<Label>(_menu).Select(label => label.Text).ToList();
                _activeRoute = _menu.PrimaryActionText == "CONTINUE STAGE 2"
                    && labels.Any(text => text.Contains("✓ CLEARED"))
                    && labels.Any(text => text.Contains("● ACTIVE"))
                    && labels.Any(text => text.Contains("◇ LOCKED"));
                _menu.SelectWorld("astral_battlefront");
                FindButton("PrimaryWorldAction")?.EmitSignal(BaseButton.SignalName.Pressed);
                _phase = 3;
                break;
            case 3:
                _overwriteShown = _menu.IsConfirmationVisible;
                _safeDefaultFocus = GetViewport().GuiGetFocusOwner()?.Name == "CancelReplaceRun";
                CaptureFrameIfRequested("PROJECT_MANNEQUIN_ARCHIVE_MAP_CONFIRM_CAPTURE");
                FindButton("CancelReplaceRun")?.EmitSignal(BaseButton.SignalName.Pressed);
                _phase = 4;
                break;
            case 4:
                _cancelRestored = !_menu.IsConfirmationVisible;
                Finish();
                break;
        }

        if (_frames >= 120)
        {
            GD.PushError($"[ArchiveMapSmoke] Timed out phase={_phase}.");
            GetTree().Quit();
        }
    }

    private void CaptureInitialMap()
    {
        var worldButtons = Descendants<Button>(_menu!)
            .Where(button => button.Name.ToString().StartsWith("WorldButton_"))
            .ToList();
        _coreWorldCards = worldButtons.Count == 4;
        _trainingExcluded = worldButtons.All(button => !button.Name.ToString().Contains("training_room"));
        _lockedRealm = worldButtons.FirstOrDefault(button =>
            button.Name == "WorldButton_iron_fist_foundry") is { Disabled: true };
        _styledBackground = ResourceLoader.Exists(_menu!.ArchiveMapBackgroundPath)
            && Descendants<TextureRect>(_menu).Any(texture =>
                texture.Name == "ArchiveMapBackground");
        _fourStageRoute = _menu!.RenderedStageCount == 4
            && Descendants<Label>(_menu).Any(label => label.Text.Contains("INTAKE BOULEVARD"))
            && Descendants<Label>(_menu).Any(label => label.Text.Contains("KNIGHT'S RELIQUARY"));
    }

    private void CaptureFrameIfRequested()
    {
        CaptureFrameIfRequested("PROJECT_MANNEQUIN_ARCHIVE_MAP_CAPTURE");
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
            GD.PushError($"[ArchiveMapSmoke] Could not save capture '{path}' ({error}).");
        }
    }

    private Button? FindButton(string name)
    {
        return Descendants<Button>(_menu!).FirstOrDefault(button => button.Name == name);
    }

    private void Finish()
    {
        var capturesPassed = CaptureExistsIfRequested("PROJECT_MANNEQUIN_ARCHIVE_MAP_CAPTURE")
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_ARCHIVE_MAP_WORLD_CAPTURE")
            && CaptureExistsIfRequested("PROJECT_MANNEQUIN_ARCHIVE_MAP_CONFIRM_CAPTURE");
        var passed = _coreWorldCards
            && _trainingExcluded
            && _lockedRealm
            && _styledBackground
            && _fourStageRoute
            && _worldSelection
            && _activeRoute
            && _overwriteShown
            && _safeDefaultFocus
            && _cancelRestored
            && capturesPassed;
        GD.Print(
            $"[ArchiveMapSmoke] SUMMARY passed={passed} worlds={_coreWorldCards} "
            + $"noTraining={_trainingExcluded} locked={_lockedRealm} styled={_styledBackground} "
            + $"stages={_fourStageRoute} "
            + $"selection={_worldSelection} active={_activeRoute} overwrite={_overwriteShown} "
            + $"safeFocus={_safeDefaultFocus} cancel={_cancelRestored} captures={capturesPassed}");
        if (!passed)
        {
            GD.PushError("[ArchiveMapSmoke] Archive Map UI or overwrite protection regressed.");
        }

        GetTree().Quit();
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
