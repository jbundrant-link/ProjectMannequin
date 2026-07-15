using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Headless lifecycle lock for the persistent scene-flow layer. Starts on the
/// main menu, transitions to combat, and verifies fade, input lock, loading
/// message, scene replacement, reveal, and final idle state.
/// </summary>
public partial class SceneFlowSmokeScenario : Node
{
    private SceneFlowCoordinator? _coordinator;
    private int _frames;
    private bool _requested;
    private bool _sawFadeOut;
    private bool _sawLoading;
    private bool _sawHolding;
    private bool _sawFadeIn;
    private bool _sawInputLock;
    private bool _sawSceneChange;
    private bool _messageCorrect;
    private bool _reloadMode;
    private string _targetScenePath = "res://Scenes/Main.tscn";
    private string _expectedMessage = "LOADING ARCHIVE STAGE";
    private string _routeName = "change";
    private ulong _sourceSceneInstanceId;

    public void Initialize(SceneFlowCoordinator coordinator)
    {
        _coordinator = coordinator;
        ProcessMode = ProcessModeEnum.Always;
        GD.Print("[SceneFlowSmoke] Driver active.");
    }

    public override void _Process(double delta)
    {
        if (_coordinator is null)
        {
            return;
        }

        _frames++;
        if (!_requested && _frames >= 3)
        {
            var currentScene = GetTree().CurrentScene;
            _sourceSceneInstanceId = currentScene?.GetInstanceId() ?? 0;
            RequestConfiguredRoute(currentScene);
        }

        if (_requested)
        {
            _sawFadeOut |= _coordinator.Phase == SceneTransitionPhase.FadingOut
                && _coordinator.FadeOpacity > 0.0f;
            _sawLoading |= _coordinator.Phase == SceneTransitionPhase.Loading;
            _sawHolding |= _coordinator.Phase == SceneTransitionPhase.Holding;
            _sawFadeIn |= _coordinator.Phase == SceneTransitionPhase.FadingIn;
            _sawInputLock |= _coordinator.IsInputLocked;
            var currentScene = GetTree().CurrentScene;
            _sawSceneChange |= currentScene?.SceneFilePath == _targetScenePath
                && currentScene.GetInstanceId() != _sourceSceneInstanceId;
            _messageCorrect |= _coordinator.LoadingMessage == _expectedMessage;
        }

        if (_requested
            && _sawSceneChange
            && _sawFadeIn
            && _coordinator.Phase == SceneTransitionPhase.Idle)
        {
            Finish();
            return;
        }

        if (_frames >= 600)
        {
            GD.PushError(
                $"[SceneFlowSmoke] Timed out phase={_coordinator.Phase} "
                + $"scene={GetTree().CurrentScene?.SceneFilePath}.");
            GetTree().Quit();
        }
    }

    private void Finish()
    {
        var coordinator = _coordinator!;
        var passed = _requested
            && _sawFadeOut
            && _sawLoading
            && _sawHolding
            && _sawFadeIn
            && _sawInputLock
            && _sawSceneChange
            && _messageCorrect
            && !coordinator.IsInputLocked
            && coordinator.FadeOpacity <= 0.001f;
        GD.Print(
            $"[SceneFlowSmoke] SUMMARY passed={passed} requested={_requested} "
            + $"mode={_routeName} "
            + $"phases={_sawFadeOut}/{_sawLoading}/{_sawHolding}/{_sawFadeIn} "
            + $"locked={_sawInputLock} scene={_sawSceneChange} message={_messageCorrect} "
            + $"idle={coordinator.Phase == SceneTransitionPhase.Idle} "
            + $"opacity={coordinator.FadeOpacity:0.00}");
        if (!passed)
        {
            GD.PushError("[SceneFlowSmoke] Scene transition lifecycle regressed.");
        }

        GetTree().Quit();
    }

    private void RequestConfiguredRoute(Node? currentScene)
    {
        var requestedRoute = OS.GetEnvironment("PROJECT_MANNEQUIN_SCENE_FLOW_ROUTE")
            .Trim()
            .ToLowerInvariant();
        switch (requestedRoute)
        {
            case "hub":
                _routeName = "hub";
                _targetScenePath = "res://Scenes/UI/ArchiveHub.tscn";
                _expectedMessage = "OPENING FORM ARCHIVE";
                _requested = PressButtonByName(currentScene, "ArchiveHubButton");
                return;
            case "menu":
                _routeName = "menu";
                _targetScenePath = "res://Scenes/UI/MainMenu.tscn";
                _expectedMessage = "RETURNING TO ARCHIVE MAP";
                _requested = PressButton(currentScene, "Return to Main Menu");
                return;
            case "training":
                _routeName = "training";
                _targetScenePath = "res://Scenes/Main.tscn";
                _expectedMessage = "LOADING TRAINING ROOM";
                _requested = PressButton(currentScene, "Enter Training Room");
                return;
            case "world":
                var world = MvpWorldCatalog.Worlds.First(candidate =>
                    candidate.Availability == ArchiveWorldAvailability.Playable);
                _routeName = "world";
                _targetScenePath = "res://Scenes/Main.tscn";
                _expectedMessage = $"ENTERING {world.DisplayName.ToUpperInvariant()} // STAGE 1";
                _requested = PressButtonByName(currentScene, "PrimaryWorldAction");
                return;
        }

        _reloadMode = currentScene?.SceneFilePath == "res://Scenes/Main.tscn";
        _routeName = _reloadMode ? "reload" : "change";
        _targetScenePath = _reloadMode
            ? currentScene!.SceneFilePath
            : "res://Scenes/Main.tscn";
        _expectedMessage = _reloadMode ? "RELOADING STAGE" : "LOADING ARCHIVE STAGE";
        _requested = _reloadMode
            ? SceneFlowCoordinator.Reload(GetTree(), _expectedMessage)
            : _coordinator!.RequestTransition(_targetScenePath, _expectedMessage);
    }

    private bool PressButton(Node? root, string text)
    {
        var button = root is null
            ? null
            : Descendants<Button>(root).FirstOrDefault(candidate => candidate.Text == text);
        if (button is null)
        {
            GD.PushError($"[SceneFlowSmoke] Could not find route button '{text}'.");
            return false;
        }

        button.EmitSignal(BaseButton.SignalName.Pressed);
        return _coordinator?.IsTransitioning == true;
    }

    private bool PressButtonByName(Node? root, string name)
    {
        var button = root is null
            ? null
            : Descendants<Button>(root).FirstOrDefault(candidate => candidate.Name == name);
        if (button is null)
        {
            GD.PushError($"[SceneFlowSmoke] Could not find route button node '{name}'.");
            return false;
        }

        button.EmitSignal(BaseButton.SignalName.Pressed);
        return _coordinator?.IsTransitioning == true;
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
