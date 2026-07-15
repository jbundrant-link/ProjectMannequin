using System;
using Godot;
using ProjectMannequin.DebugTools;

namespace ProjectMannequin.Core;

public enum SceneTransitionPhase
{
    Idle,
    FadingOut,
    Loading,
    Holding,
    FadingIn,
}

/// <summary>
/// Persistent player-facing scene transition layer. It owns input lock, fade,
/// loading copy, scene replacement, and reveal so menu, stage, retry, and
/// results flows never expose an abrupt scene cut or a partially loaded frame.
/// </summary>
public partial class SceneFlowCoordinator : CanvasLayer
{
    private const float DefaultFadeOutSeconds = 0.24f;
    private const float DefaultHoldSeconds = 0.12f;
    private const float DefaultFadeInSeconds = 0.30f;
    private const int MaximumLoadingFrames = 300;

    private Control _root = null!;
    private ColorRect _fade = null!;
    private VBoxContainer _loadingCard = null!;
    private Label _messageLabel = null!;
    private Label _pulseLabel = null!;
    private SceneTransitionPhase _phase;
    private string _targetScenePath = "";
    private string _loadingMessage = "NOW LOADING";
    private float _phaseElapsed;
    private float _opacity;
    private int _loadingFrames;
    private int _pulseStep;

    public static SceneFlowCoordinator? Instance { get; private set; }

    public SceneTransitionPhase Phase => _phase;
    public bool IsTransitioning => _phase != SceneTransitionPhase.Idle;
    public bool IsInputLocked => IsTransitioning;
    public float FadeOpacity => _opacity;
    public string LoadingMessage => _loadingMessage;
    public string TargetScenePath => _targetScenePath;

    public event Action<string>? TransitionCompleted;

    public override void _Ready()
    {
        if (Instance is not null && Instance != this)
        {
            QueueFree();
            return;
        }

        Instance = this;
        Layer = 1000;
        ProcessMode = ProcessModeEnum.Always;
        BuildInterface();
        SetOpacity(0.0f);
        _root.Visible = false;

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_SCENE_FLOW_SMOKE_TEST") == "1")
        {
            var smoke = new SceneFlowSmokeScenario();
            smoke.Initialize(this);
            AddChild(smoke);
        }
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!IsInputLocked)
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (_phase == SceneTransitionPhase.Idle)
        {
            return;
        }

        var frameDelta = Mathf.Max(0.0f, (float)delta);
        _phaseElapsed += frameDelta;
        UpdateLoadingPulse();

        switch (_phase)
        {
            case SceneTransitionPhase.FadingOut:
                SetOpacity(Mathf.Clamp(
                    _phaseElapsed / DefaultFadeOutSeconds,
                    0.0f,
                    1.0f));
                if (_phaseElapsed >= DefaultFadeOutSeconds)
                {
                    BeginSceneLoad();
                }
                break;
            case SceneTransitionPhase.Loading:
                _loadingFrames++;
                SetOpacity(1.0f);
                if (IsTargetSceneCurrent())
                {
                    SetPhase(SceneTransitionPhase.Holding);
                }
                else if (_loadingFrames >= MaximumLoadingFrames)
                {
                    FailTransition($"Timed out loading '{_targetScenePath}'.");
                }
                break;
            case SceneTransitionPhase.Holding:
                SetOpacity(1.0f);
                if (_phaseElapsed >= DefaultHoldSeconds)
                {
                    SetPhase(SceneTransitionPhase.FadingIn);
                }
                break;
            case SceneTransitionPhase.FadingIn:
                SetOpacity(1.0f - Mathf.Clamp(
                    _phaseElapsed / DefaultFadeInSeconds,
                    0.0f,
                    1.0f));
                if (_phaseElapsed >= DefaultFadeInSeconds)
                {
                    CompleteTransition();
                }
                break;
        }
    }

    public bool RequestTransition(string scenePath, string loadingMessage = "NOW LOADING")
    {
        if (IsTransitioning || string.IsNullOrWhiteSpace(scenePath))
        {
            return false;
        }

        _targetScenePath = scenePath;
        _loadingMessage = string.IsNullOrWhiteSpace(loadingMessage)
            ? "NOW LOADING"
            : loadingMessage.Trim().ToUpperInvariant();
        _messageLabel.Text = _loadingMessage;
        _loadingFrames = 0;
        _pulseStep = 0;
        _root.Visible = true;
        SetOpacity(0.0f);
        SetPhase(SceneTransitionPhase.FadingOut);
        return true;
    }

    public static bool TransitionTo(
        SceneTree tree,
        string scenePath,
        string loadingMessage = "NOW LOADING")
    {
        if (Instance is not null)
        {
            return Instance.RequestTransition(scenePath, loadingMessage);
        }

        return tree.ChangeSceneToFile(scenePath) == Error.Ok;
    }

    public static bool Reload(
        SceneTree tree,
        string loadingMessage = "RELOADING STAGE")
    {
        var scenePath = tree.CurrentScene?.SceneFilePath ?? "";
        if (Instance is not null && !string.IsNullOrWhiteSpace(scenePath))
        {
            return Instance.RequestTransition(scenePath, loadingMessage);
        }

        return tree.ReloadCurrentScene() == Error.Ok;
    }

    private void BeginSceneLoad()
    {
        SetOpacity(1.0f);
        SetPhase(SceneTransitionPhase.Loading);
        _loadingFrames = 0;
        var error = GetTree().ChangeSceneToFile(_targetScenePath);
        if (error != Error.Ok)
        {
            FailTransition($"Could not load '{_targetScenePath}' ({error}).");
        }
    }

    private bool IsTargetSceneCurrent()
    {
        var currentPath = GetTree().CurrentScene?.SceneFilePath ?? "";
        return string.Equals(
            currentPath,
            _targetScenePath,
            StringComparison.OrdinalIgnoreCase);
    }

    private void CompleteTransition()
    {
        var completedPath = _targetScenePath;
        SetOpacity(0.0f);
        _root.Visible = false;
        _targetScenePath = "";
        SetPhase(SceneTransitionPhase.Idle);
        TransitionCompleted?.Invoke(completedPath);
    }

    private void FailTransition(string message)
    {
        GD.PushError($"[SceneFlow] {message}");
        _targetScenePath = "";
        SetPhase(SceneTransitionPhase.FadingIn);
    }

    private void SetPhase(SceneTransitionPhase phase)
    {
        _phase = phase;
        _phaseElapsed = 0.0f;
    }

    private void SetOpacity(float opacity)
    {
        _opacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
        _fade.Color = new Color(0.012f, 0.018f, 0.032f, _opacity);
        var cardAlpha = Mathf.Clamp((_opacity - 0.28f) / 0.42f, 0.0f, 1.0f);
        _loadingCard.Modulate = new Color(1.0f, 1.0f, 1.0f, cardAlpha);
    }

    private void UpdateLoadingPulse()
    {
        var nextStep = Mathf.FloorToInt(_phaseElapsed * 4.0f) % 4;
        if (nextStep == _pulseStep)
        {
            return;
        }

        _pulseStep = nextStep;
        _pulseLabel.Text = new string('•', _pulseStep + 1);
    }

    private void BuildInterface()
    {
        _root = new Control
        {
            Name = "SceneTransitionRoot",
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _fade = new ColorRect
        {
            Name = "Fade",
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_fade);

        _loadingCard = new VBoxContainer
        {
            Name = "LoadingCard",
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _loadingCard.SetAnchorsPreset(Control.LayoutPreset.Center);
        _loadingCard.OffsetLeft = -420.0f;
        _loadingCard.OffsetTop = -95.0f;
        _loadingCard.OffsetRight = 420.0f;
        _loadingCard.OffsetBottom = 95.0f;
        _loadingCard.AddThemeConstantOverride("separation", 8);
        _root.AddChild(_loadingCard);

        var eyebrow = MakeLabel(
            "THE LIVING ARCHIVE",
            16,
            new Color(0.42f, 0.94f, 0.92f));
        _loadingCard.AddChild(eyebrow);

        var title = MakeLabel("PROJECT MANNEQUIN", 42, Colors.White);
        _loadingCard.AddChild(title);

        _messageLabel = MakeLabel(_loadingMessage, 20, new Color(1.0f, 0.78f, 0.28f));
        _loadingCard.AddChild(_messageLabel);

        _pulseLabel = MakeLabel("•", 20, new Color(0.56f, 0.86f, 1.0f));
        _loadingCard.AddChild(_pulseLabel);
    }

    private static Label MakeLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = color,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride(
            "font_outline_color",
            new Color(0.0f, 0.0f, 0.0f, 0.94f));
        label.AddThemeConstantOverride("outline_size", 6);
        return label;
    }
}
