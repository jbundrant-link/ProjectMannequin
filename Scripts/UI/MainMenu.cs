using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Progression;

namespace ProjectMannequin.UI;

public partial class MainMenu : Control
{
    [Export] public string CombatScenePath { get; set; } = "res://Scenes/Main.tscn";
    [Export] public string ArchiveHubScenePath { get; set; } = "res://Scenes/UI/ArchiveHub.tscn";
    [Export] public string ArchiveMapBackgroundPath { get; set; } =
        "res://Assets/UI/ArchiveMap/project_mannequin_archive_map_background_style_v1.png";

    private readonly Dictionary<string, Button> _worldButtons = new();
    private MvpProgressData _progress = new();
    private Label _activeRunTitle = null!;
    private Label _activeRunBody = null!;
    private Button _continueRunButton = null!;
    private Label _worldEyebrow = null!;
    private Label _worldTitle = null!;
    private Label _worldIdentity = null!;
    private Label _worldProgress = null!;
    private VBoxContainer _stageRoute = null!;
    private Label _deploymentHint = null!;
    private Button _primaryActionButton = null!;
    private Button _restartWorldButton = null!;
    private Button _inputDeviceButton = null!;
    private OptionsMenu _optionsMenu = null!;
    private Label _statusLabel = null!;
    private Control _confirmOverlay = null!;
    private Label _confirmTitle = null!;
    private Label _confirmBody = null!;
    private Button _confirmStartButton = null!;
    private Button _confirmCancelButton = null!;
    private string _selectedWorldId = "";
    private string _pendingWorldId = "";
    private int _lastJoypadCount = -1;

    public string SelectedWorldId => _selectedWorldId;
    public bool IsConfirmationVisible => _confirmOverlay.Visible;
    public int RenderedStageCount => _stageRoute?.GetChildren().Count(child => child is PanelContainer) ?? 0;
    public string PrimaryActionText => _primaryActionButton?.Text ?? "";

    public override void _Ready()
    {
        ConfigureCaptureViewport();
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_ARCHIVE_MAP_SMOKE_TEST") != "1"
            && OS.GetEnvironment("PROJECT_MANNEQUIN_SCENE_FLOW_SMOKE_TEST") != "1")
        {
            RunSessionManager.Instance.TryLoadCommittedRun();
        }

        _progress = MvpProgressStore.Load();
        BuildInterface();

        _optionsMenu = new OptionsMenu { Name = "MainMenuOptions" };
        AddChild(_optionsMenu);

        RefreshInputDeviceButton();
        var preferredWorld = IsCoreWorld(RunSessionManager.Instance.CurrentWorldId)
            ? RunSessionManager.Instance.CurrentWorldId
            : CoreWorlds().First(world => world.Availability == ArchiveWorldAvailability.Playable).Id;
        SelectWorld(preferredWorld);
        RefreshActiveRunPanel();
        FocusInitialControl();

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_ARCHIVE_MAP_SMOKE_TEST") == "1")
        {
            var smoke = new ProjectMannequin.DebugTools.ArchiveMapSmokeScenario();
            smoke.Initialize(this);
            AddChild(smoke);
        }
    }

    private void ConfigureCaptureViewport()
    {
        var widthText = OS.GetEnvironment("PROJECT_MANNEQUIN_VIEWPORT_WIDTH");
        var heightText = OS.GetEnvironment("PROJECT_MANNEQUIN_VIEWPORT_HEIGHT");
        if (!int.TryParse(widthText, out var width)
            || !int.TryParse(heightText, out var height)
            || width <= 0
            || height <= 0)
        {
            return;
        }

        var window = GetWindow();
        window.Mode = Window.ModeEnum.Windowed;
        window.Borderless = true;
        window.ContentScaleSize = new Vector2I(width, height);
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;
        window.Size = new Vector2I(width, height);
    }

    public override void _Process(double delta)
    {
        var joypadCount = Input.GetConnectedJoypads().Count;
        if (joypadCount == _lastJoypadCount)
        {
            return;
        }

        _lastJoypadCount = joypadCount;
        InputDevicePreferences.InvalidateCache();
        RefreshInputDeviceButton();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!_confirmOverlay.Visible
            || !UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Cancel))
        {
            return;
        }

        HideRunConfirmation();
        GetViewport().SetInputAsHandled();
    }

    public void SelectWorld(string worldId)
    {
        var world = CoreWorlds().FirstOrDefault(candidate => candidate.Id == worldId);
        if (world is null || world.Availability != ArchiveWorldAvailability.Playable)
        {
            return;
        }

        _selectedWorldId = worldId;
        RefreshWorldButtons();
        RefreshWorldDetail(world);
        RefreshDeploymentActions(world);
    }

    private void BuildInterface()
    {
        AddArchiveMapBackground();

        var rootMargin = new MarginContainer { Name = "ArchiveMapMargin" };
        rootMargin.SetAnchorsPreset(LayoutPreset.FullRect);
        rootMargin.AddThemeConstantOverride("margin_left", 38);
        rootMargin.AddThemeConstantOverride("margin_top", 28);
        rootMargin.AddThemeConstantOverride("margin_right", 38);
        rootMargin.AddThemeConstantOverride("margin_bottom", 28);
        AddChild(rootMargin);

        var root = new VBoxContainer { Name = "ArchiveMapRoot" };
        root.AddThemeConstantOverride("separation", 18);
        rootMargin.AddChild(root);
        root.AddChild(BuildHeader());

        var body = new HBoxContainer
        {
            Name = "ArchiveMapBody",
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        body.AddThemeConstantOverride("separation", 16);
        root.AddChild(body);
        body.AddChild(BuildWorldRail());
        body.AddChild(BuildRoutePanel());
        body.AddChild(BuildOperationsPanel());
        BuildConfirmationOverlay();
    }

    private void AddArchiveMapBackground()
    {
        if (ResourceLoader.Exists(ArchiveMapBackgroundPath))
        {
            var background = new TextureRect
            {
                Name = "ArchiveMapBackground",
                Texture = GD.Load<Texture2D>(ArchiveMapBackgroundPath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            background.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(background);
        }

        var wash = new ColorRect
        {
            Name = "ArchiveMapReadabilityWash",
            Color = new Color(0.025f, 0.018f, 0.040f, 0.46f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        wash.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(wash);
    }

    private Control BuildHeader()
    {
        var header = new HBoxContainer { Name = "Header" };
        var titleBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        titleBox.AddThemeConstantOverride("separation", -2);
        header.AddChild(titleBox);
        titleBox.AddChild(MakeLabel(
            "THE LIVING ARCHIVE  //  NAVIGATION SYSTEM",
            14,
            new Color(0.42f, 0.94f, 0.92f)));
        titleBox.AddChild(MakeLabel("ARCHIVE MAP", 42, Colors.White));
        titleBox.AddChild(MakeLabel(
            "Choose a realm. Survive four stages. Inherit its champion.",
            16,
            new Color(0.70f, 0.78f, 0.86f)));

        var playerBox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(300.0f, 0.0f),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        playerBox.AddChild(MakeLabel("PLAYER 1 INPUT", 12, new Color(0.56f, 0.86f, 1.0f)));
        _inputDeviceButton = MakeButton("", 300.0f, 42.0f, 16);
        _inputDeviceButton.Name = "InputDeviceButton";
        _inputDeviceButton.TooltipText = "Change the device controlling Player 1";
        _inputDeviceButton.Pressed += CycleInputDevice;
        playerBox.AddChild(_inputDeviceButton);
        header.AddChild(playerBox);
        return header;
    }

    private Control BuildWorldRail()
    {
        var panel = MakePanel(
            "RealmRail",
            Colors.Transparent,
            Colors.Transparent,
            borderWidth: 0);
        panel.CustomMinimumSize = new Vector2(300.0f, 0.0f);
        var margin = AddPanelMargin(panel, 16);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        margin.AddChild(box);
        box.AddChild(MakeLabel("ARCHIVE REALMS", 17, new Color(1.0f, 0.78f, 0.28f)));
        box.AddChild(MakeLabel("SELECT DESTINATION", 11, new Color(0.58f, 0.66f, 0.74f)));

        foreach (var world in CoreWorlds())
        {
            var capturedId = world.Id;
            var button = MakeButton("", 268.0f, 70.0f, 15);
            button.Name = $"WorldButton_{world.Id}";
            button.Alignment = HorizontalAlignment.Left;
            button.Disabled = world.Availability != ArchiveWorldAvailability.Playable;
            button.Pressed += () => SelectWorld(capturedId);
            _worldButtons[world.Id] = button;
            box.AddChild(button);
        }

        box.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        box.AddChild(MakeLabel(
            "D-PAD / WASD  NAVIGATE\nACCEPT  SELECT",
            11,
            new Color(0.48f, 0.57f, 0.66f)));
        return panel;
    }

    private Control BuildRoutePanel()
    {
        var panel = MakePanel(
            "RoutePanel",
            Colors.Transparent,
            Colors.Transparent,
            borderWidth: 0);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.CustomMinimumSize = new Vector2(560.0f, 0.0f);
        var margin = AddPanelMargin(panel, 20);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        margin.AddChild(box);
        _worldEyebrow = MakeLabel("REALM // READY", 12, new Color(0.42f, 0.94f, 0.92f));
        _worldTitle = MakeLabel("ARCHIVE NEXUS", 31, Colors.White);
        _worldIdentity = MakeLabel("", 15, new Color(0.72f, 0.80f, 0.88f));
        _worldProgress = MakeLabel("", 13, new Color(1.0f, 0.82f, 0.36f));
        box.AddChild(_worldEyebrow);
        box.AddChild(_worldTitle);
        box.AddChild(_worldIdentity);
        box.AddChild(_worldProgress);
        box.AddChild(new HSeparator());
        var routeCanvas = new Control
        {
            Name = "RouteCanvas",
            CustomMinimumSize = new Vector2(0.0f, 330.0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        box.AddChild(routeCanvas);
        var spine = new ArchiveMapRouteSpine
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        spine.SetAnchorsPreset(LayoutPreset.FullRect);
        routeCanvas.AddChild(spine);
        _stageRoute = new VBoxContainer
        {
            Name = "StageRoute",
        };
        _stageRoute.SetAnchorsPreset(LayoutPreset.FullRect);
        _stageRoute.OffsetLeft = 24.0f;
        _stageRoute.AddThemeConstantOverride("separation", 6);
        routeCanvas.AddChild(_stageRoute);
        return panel;
    }

    private Control BuildOperationsPanel()
    {
        var panel = MakePanel(
            "OperationsPanel",
            Colors.Transparent,
            Colors.Transparent,
            borderWidth: 0);
        panel.CustomMinimumSize = new Vector2(310.0f, 0.0f);
        var margin = AddPanelMargin(panel, 16);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        margin.AddChild(box);
        _activeRunTitle = MakeLabel("ACTIVE RUN", 15, new Color(0.42f, 0.94f, 0.92f));
        _activeRunBody = MakeLabel("", 13, new Color(0.76f, 0.84f, 0.91f));
        _activeRunBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _continueRunButton = MakeButton("RETURN TO ACTIVE RUN", 278.0f, 42.0f, 15);
        _continueRunButton.Name = "ContinueRunButton";
        _continueRunButton.Pressed += ContinueRun;
        box.AddChild(_activeRunTitle);
        box.AddChild(_activeRunBody);
        box.AddChild(_continueRunButton);
        box.AddChild(new HSeparator());
        box.AddChild(MakeLabel("DEPLOYMENT", 15, new Color(1.0f, 0.78f, 0.28f)));
        _deploymentHint = MakeLabel("", 13, new Color(0.68f, 0.76f, 0.84f));
        _deploymentHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _deploymentHint.CustomMinimumSize = new Vector2(0.0f, 54.0f);
        box.AddChild(_deploymentHint);
        _primaryActionButton = MakeButton("BEGIN RUN", 278.0f, 48.0f, 17);
        _primaryActionButton.Name = "PrimaryWorldAction";
        _primaryActionButton.Pressed += ActivateSelectedWorld;
        box.AddChild(_primaryActionButton);
        _restartWorldButton = MakeButton("RESTART WORLD", 278.0f, 38.0f, 14);
        _restartWorldButton.Name = "RestartWorldButton";
        _restartWorldButton.Pressed += () => RequestStartWorld(_selectedWorldId, restartCurrent: true);
        box.AddChild(_restartWorldButton);
        box.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        var hubButton = MakeButton("FORM ARCHIVE / TRAINING", 278.0f, 42.0f, 14);
        hubButton.Name = "ArchiveHubButton";
        hubButton.Pressed += StartArchiveHub;
        box.AddChild(hubButton);

        if (_progress.SecretEndingUnlocked)
        {
            var secretButton = MakeButton("HOLLOW ARCHIVE  //  ???", 278.0f, 42.0f, 14);
            secretButton.Pressed += () => RequestStartWorld(HollowArchiveMission.WorldId);
            box.AddChild(secretButton);
        }

        var optionsButton = MakeButton("OPTIONS", 278.0f, 38.0f, 14);
        optionsButton.Name = "OptionsButton";
        optionsButton.Pressed += OpenOptions;
        box.AddChild(optionsButton);

        _statusLabel = MakeLabel("", 12, new Color(0.68f, 0.75f, 0.82f));
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statusLabel.CustomMinimumSize = new Vector2(0.0f, 48.0f);
        box.AddChild(_statusLabel);
        var quitButton = MakeButton("QUIT", 278.0f, 38.0f, 14);
        quitButton.Pressed += () => GetTree().Quit();
        box.AddChild(quitButton);
        return panel;
    }

    private void BuildConfirmationOverlay()
    {
        _confirmOverlay = new Control
        {
            Name = "RunReplacementConfirmation",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _confirmOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_confirmOverlay);
        var dim = new ColorRect
        {
            Color = new Color(0.0f, 0.0f, 0.0f, 0.78f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        _confirmOverlay.AddChild(dim);
        var panel = MakePanel(
            "ConfirmationPanel",
            new Color(0.035f, 0.047f, 0.070f, 1.0f),
            new Color(1.0f, 0.56f, 0.24f, 0.95f),
            borderWidth: 3);
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.OffsetLeft = -320.0f;
        panel.OffsetTop = -155.0f;
        panel.OffsetRight = 320.0f;
        panel.OffsetBottom = 155.0f;
        _confirmOverlay.AddChild(panel);
        var margin = AddPanelMargin(panel, 24);
        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        box.AddThemeConstantOverride("separation", 14);
        margin.AddChild(box);
        _confirmTitle = MakeLabel("REPLACE ACTIVE RUN?", 26, new Color(1.0f, 0.70f, 0.30f));
        _confirmTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _confirmBody = MakeLabel("", 16, new Color(0.88f, 0.91f, 0.95f));
        _confirmBody.HorizontalAlignment = HorizontalAlignment.Center;
        _confirmBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(_confirmTitle);
        box.AddChild(_confirmBody);
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 14);
        _confirmStartButton = MakeButton("START NEW RUN", 220.0f, 46.0f, 15);
        _confirmStartButton.Name = "ConfirmReplaceRun";
        _confirmStartButton.Pressed += ConfirmStartWorld;
        _confirmCancelButton = MakeButton("KEEP CURRENT RUN", 220.0f, 46.0f, 15);
        _confirmCancelButton.Name = "CancelReplaceRun";
        _confirmCancelButton.Pressed += HideRunConfirmation;
        buttons.AddChild(_confirmStartButton);
        buttons.AddChild(_confirmCancelButton);
        box.AddChild(buttons);
    }

    private void RefreshWorldButtons()
    {
        foreach (var world in CoreWorlds())
        {
            var button = _worldButtons[world.Id];
            if (world.Availability != ArchiveWorldAvailability.Playable)
            {
                button.Text = $"  {world.DisplayName.ToUpperInvariant()}\n  SEALED  //  ACCESS DENIED";
                ApplyWorldButtonStyle(button, WorldAccent(world.Id), selected: false, locked: true);
                continue;
            }

            var view = BuildWorldView(world);
            var status = view.Status switch
            {
                ArchiveMapWorldStatus.Active => "ACTIVE RUN",
                ArchiveMapWorldStatus.Completed => "WORLD CLEARED",
                _ => "READY",
            };
            button.Text = $"  {world.DisplayName.ToUpperInvariant()}\n  {status}  //  {view.ClearedStages}/4  //  BEST {view.BestRank}";
            ApplyWorldButtonStyle(
                button,
                WorldAccent(world.Id),
                selected: world.Id == _selectedWorldId,
                locked: false);
        }
    }

    private void RefreshWorldDetail(ArchiveWorldData world)
    {
        var view = BuildWorldView(world);
        var accent = WorldAccent(world.Id);
        _worldEyebrow.Text = view.Status switch
        {
            ArchiveMapWorldStatus.Active => "REALM // ACTIVE RUN",
            ArchiveMapWorldStatus.Completed => "REALM // WORLD CLEARED",
            _ => "REALM // READY FOR DEPLOYMENT",
        };
        _worldEyebrow.Modulate = accent;
        _worldTitle.Text = view.DisplayName.ToUpperInvariant();
        _worldIdentity.Text = view.CombatIdentity;
        _worldProgress.Text = $"ROUTE PROGRESS  {view.ClearedStages}/4    BEST RANK  {view.BestRank}";
        foreach (var child in _stageRoute.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var stage in view.Stages)
        {
            _stageRoute.AddChild(BuildStageNode(stage, accent));
        }
    }

    private void RefreshDeploymentActions(ArchiveWorldData world)
    {
        var session = RunSessionManager.Instance;
        var selectedIsActive = session.HasActiveRun && session.CurrentWorldId == world.Id;
        var completed = _progress.CompletedWorldIds.Contains(world.Id);
        _restartWorldButton.Visible = selectedIsActive;
        if (selectedIsActive)
        {
            _primaryActionButton.Text = $"CONTINUE STAGE {session.CurrentStageIndex + 1}";
            _deploymentHint.Text = "Resume from the committed stage boundary with health, meter, lives, and build intact.";
        }
        else if (completed)
        {
            _primaryActionButton.Text = "REPLAY WORLD";
            _deploymentHint.Text = "Begin a fresh four-stage run. Existing best ranks, scores, and times remain recorded.";
        }
        else
        {
            _primaryActionButton.Text = "BEGIN WORLD RUN";
            _deploymentHint.Text = "Start at Stage 1. Progress autosaves only after a stage is cleared.";
        }

        _primaryActionButton.Disabled = world.Availability != ArchiveWorldAvailability.Playable;
        _statusLabel.Text = selectedIsActive
            ? $"Checkpoint ready  //  {session.RemainingLives} lives  //  score {session.RunScore:000000}"
            : session.HasActiveRun
                ? "Starting this realm will ask before replacing the active checkpoint."
                : "No active checkpoint. Select BEGIN WORLD RUN to deploy.";
        RefreshActiveRunPanel();
    }

    private void RefreshActiveRunPanel()
    {
        var session = RunSessionManager.Instance;
        var visible = session.HasActiveRun && IsCoreWorld(session.CurrentWorldId);
        _activeRunTitle.Visible = true;
        _activeRunBody.Visible = true;
        _continueRunButton.Visible = visible && session.CurrentWorldId != _selectedWorldId;
        if (!visible)
        {
            _activeRunTitle.Text = "NO ACTIVE RUN";
            _activeRunBody.Text = "Choose a realm to create a stage-boundary checkpoint.";
            return;
        }

        var worldName = MvpWorldCatalog.FindWorld(session.CurrentWorldId)?.DisplayName
            ?? session.CurrentWorldId;
        _activeRunTitle.Text = "ACTIVE RUN  //  CHECKPOINT SECURE";
        _activeRunBody.Text = $"{worldName}\nStage {session.CurrentStageIndex + 1}/4  •  {session.RemainingLives} lives\nRun score {session.RunScore:000000}";
    }

    private Control BuildStageNode(ArchiveMapStageView stage, Color accent)
    {
        var statusColor = StageStatusColor(stage.Status, accent);
        var panel = MakePanel(
            $"StageNode_{stage.StageNumber}",
            new Color(statusColor.R * 0.10f, statusColor.G * 0.10f, statusColor.B * 0.10f, 0.88f),
            new Color(statusColor.R, statusColor.G, statusColor.B, 0.82f),
            borderWidth: stage.Status == ArchiveMapStageStatus.Active ? 3 : 1);
        panel.CustomMinimumSize = new Vector2(0.0f, 76.0f);
        var margin = AddPanelMargin(panel, 10);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        margin.AddChild(row);
        var index = MakeLabel(stage.StageNumber.ToString("00"), 25, statusColor);
        index.CustomMinimumSize = new Vector2(46.0f, 0.0f);
        index.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(index);
        var copy = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        copy.AddThemeConstantOverride("separation", 1);
        var name = MakeLabel(stage.Title.ToUpperInvariant(), 17, Colors.White);
        var subtitle = MakeLabel(stage.Subtitle, 12, new Color(0.65f, 0.73f, 0.81f));
        subtitle.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        copy.AddChild(name);
        copy.AddChild(subtitle);
        row.AddChild(copy);
        var stats = new VBoxContainer { CustomMinimumSize = new Vector2(142.0f, 0.0f) };
        var status = MakeLabel(StageStatusText(stage.Status), 12, statusColor);
        status.HorizontalAlignment = HorizontalAlignment.Right;
        var record = MakeLabel(
            stage.HasRecord
                ? $"RANK {stage.BestRank}  •  {stage.BestScore:000000}\n{FormatFrames(stage.BestTimeFrames)}  /  PAR {FormatSeconds(stage.ParTimeSeconds)}"
                : $"NO RECORD\nPAR {FormatSeconds(stage.ParTimeSeconds)}",
            11,
            new Color(0.73f, 0.80f, 0.87f));
        record.HorizontalAlignment = HorizontalAlignment.Right;
        stats.AddChild(status);
        stats.AddChild(record);
        row.AddChild(stats);
        return panel;
    }

    private void ActivateSelectedWorld()
    {
        var session = RunSessionManager.Instance;
        if (session.HasActiveRun && session.CurrentWorldId == _selectedWorldId)
        {
            ContinueRun();
            return;
        }

        RequestStartWorld(_selectedWorldId);
    }

    private void RequestStartWorld(string worldId, bool restartCurrent = false)
    {
        var session = RunSessionManager.Instance;
        if (ArchiveMapModel.RequiresRunReplacement(
                session.CurrentWorldId,
                worldId,
                restartingSelectedWorld: restartCurrent))
        {
            ShowRunConfirmation(worldId, restartCurrent);
            return;
        }

        BeginNewRun(worldId);
    }

    private void ShowRunConfirmation(string worldId, bool restartCurrent)
    {
        _pendingWorldId = worldId;
        var session = RunSessionManager.Instance;
        var activeName = MvpWorldCatalog.FindWorld(session.CurrentWorldId)?.DisplayName
            ?? session.CurrentWorldId;
        var targetName = MvpWorldCatalog.FindWorld(worldId)?.DisplayName
            ?? (worldId == HollowArchiveMission.WorldId ? "The Hollow Archive" : worldId);
        _confirmTitle.Text = restartCurrent ? "RESTART ACTIVE WORLD?" : "REPLACE ACTIVE RUN?";
        _confirmBody.Text =
            $"Current checkpoint: {activeName}, Stage {session.CurrentStageIndex + 1}/4.\n\n"
            + $"Starting {targetName} creates a new Stage 1 checkpoint. "
            + "Uncommitted stage progress will be discarded; permanent records remain safe.";
        _confirmStartButton.Text = restartCurrent ? "RESTART WORLD" : "START NEW RUN";
        _confirmOverlay.Visible = true;
        _confirmCancelButton.CallDeferred("grab_focus");
    }

    private void HideRunConfirmation()
    {
        _confirmOverlay.Visible = false;
        _pendingWorldId = "";
        _primaryActionButton.CallDeferred("grab_focus");
    }

    private void ConfirmStartWorld()
    {
        var worldId = _pendingWorldId;
        _confirmOverlay.Visible = false;
        _pendingWorldId = "";
        BeginNewRun(worldId);
    }

    private void BeginNewRun(string worldId)
    {
        if (!MvpMissionSelection.TrySelectWorld(worldId))
        {
            _statusLabel.Text = "That archive realm is not playable yet.";
            return;
        }

        RunSessionManager.Instance.StartNewRun(worldId);
        var worldName = MvpWorldCatalog.FindWorld(worldId)?.DisplayName
            ?? (worldId == HollowArchiveMission.WorldId ? "Hollow Archive" : "Archive Realm");
        SceneFlowCoordinator.TransitionTo(
            GetTree(),
            CombatScenePath,
            $"ENTERING {worldName} // STAGE 1");
    }

    private void ContinueRun()
    {
        var session = RunSessionManager.Instance;
        if (!session.HasActiveRun || !MvpMissionSelection.TrySelectWorld(session.CurrentWorldId))
        {
            _statusLabel.Text = "The saved run could not be restored.";
            return;
        }

        var worldName = MvpWorldCatalog.FindWorld(session.CurrentWorldId)?.DisplayName
            ?? session.CurrentWorldId;
        SceneFlowCoordinator.TransitionTo(
            GetTree(),
            CombatScenePath,
            $"RESTORING {worldName} // STAGE {session.CurrentStageIndex + 1}");
    }

    private void StartArchiveHub()
    {
        SceneFlowCoordinator.TransitionTo(
            GetTree(),
            ArchiveHubScenePath,
            "OPENING FORM ARCHIVE");
    }

    private void CycleInputDevice()
    {
        InputDevicePreferences.CycleP1Device();
        // Relabel prompts and move lists for the newly selected device.
        InputGlyphs.Invalidate();
        RefreshInputDeviceButton();
    }

    private void RefreshInputDeviceButton()
    {
        if (_inputDeviceButton is not null)
        {
            _inputDeviceButton.Text = $"P1  //  {InputDevicePreferences.CurrentP1Label().ToUpperInvariant()}";
        }
    }

    private ArchiveMapWorldView BuildWorldView(ArchiveWorldData world)
    {
        return ArchiveMapModel.Build(
            world,
            WorldRunCatalog.CreateRun(world.Id),
            _progress,
            RunSessionManager.Instance.CurrentWorldId,
            RunSessionManager.Instance.CurrentStageIndex);
    }

    private void FocusInitialControl()
    {
        var session = RunSessionManager.Instance;
        if (session.HasActiveRun && session.CurrentWorldId != _selectedWorldId && _continueRunButton.Visible)
        {
            _continueRunButton.GrabFocus();
            return;
        }

        if (_worldButtons.TryGetValue(_selectedWorldId, out var selected) && !selected.Disabled)
        {
            selected.GrabFocus();
        }
    }

    private static IEnumerable<ArchiveWorldData> CoreWorlds()
    {
        return MvpWorldCatalog.Worlds.Where(world => world.Id != "training_room");
    }

    private static bool IsCoreWorld(string worldId)
    {
        return !string.IsNullOrWhiteSpace(worldId)
            && CoreWorlds().Any(world => world.Id == worldId);
    }

    private static string StageStatusText(ArchiveMapStageStatus status)
    {
        return status switch
        {
            ArchiveMapStageStatus.Active => "● ACTIVE",
            ArchiveMapStageStatus.Cleared => "✓ CLEARED",
            ArchiveMapStageStatus.Replay => "◆ REPLAY",
            ArchiveMapStageStatus.Available => "● AVAILABLE",
            _ => "◇ LOCKED",
        };
    }

    private static Color StageStatusColor(ArchiveMapStageStatus status, Color accent)
    {
        return status switch
        {
            ArchiveMapStageStatus.Active => accent,
            ArchiveMapStageStatus.Cleared => new Color(0.38f, 0.94f, 0.64f),
            ArchiveMapStageStatus.Replay => new Color(1.0f, 0.78f, 0.28f),
            ArchiveMapStageStatus.Available => new Color(0.74f, 0.84f, 0.94f),
            _ => new Color(0.39f, 0.45f, 0.52f),
        };
    }

    private static Color WorldAccent(string worldId)
    {
        return worldId switch
        {
            "archive_nexus" => new Color(0.42f, 0.94f, 0.92f),
            "world_warrior_sector" => new Color(1.0f, 0.43f, 0.30f),
            "astral_battlefront" => new Color(0.76f, 0.58f, 1.0f),
            _ => new Color(0.58f, 0.62f, 0.68f),
        };
    }

    private static string FormatFrames(int frames)
    {
        return frames <= 0 ? "--:--" : FormatSeconds(frames / (float)GameConstants.TickRate);
    }

    private static string FormatSeconds(float seconds)
    {
        var whole = Math.Max(0, Mathf.RoundToInt(seconds));
        return $"{whole / 60:00}:{whole % 60:00}";
    }

    private static Label MakeLabel(string text, int fontSize, Color color)
    {
        var label = new Label { Text = text, Modulate = color };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_outline_color", new Color(0.0f, 0.0f, 0.0f, 0.82f));
        label.AddThemeConstantOverride("outline_size", 3);
        return label;
    }

    private void OpenOptions()
    {
        _optionsMenu.Open();
    }

    private static Button MakeButton(string text, float width, float height, int fontSize)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, height),
            FocusMode = FocusModeEnum.All,
        };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeStyleboxOverride(
            "normal",
            PanelStyle(new Color(0.07f, 0.09f, 0.13f, 0.96f), new Color(0.24f, 0.33f, 0.42f), 1));
        button.AddThemeStyleboxOverride(
            "hover",
            PanelStyle(new Color(0.10f, 0.14f, 0.19f, 0.98f), new Color(0.56f, 0.86f, 1.0f), 2));
        button.AddThemeStyleboxOverride(
            "focus",
            PanelStyle(new Color(0.10f, 0.15f, 0.20f, 0.99f), new Color(1.0f, 0.78f, 0.28f), 3));
        button.AddThemeStyleboxOverride(
            "pressed",
            PanelStyle(new Color(0.13f, 0.18f, 0.23f, 1.0f), new Color(0.42f, 0.94f, 0.92f), 2));
        return button;
    }

    private static void ApplyWorldButtonStyle(Button button, Color accent, bool selected, bool locked)
    {
        var background = locked
            ? new Color(0.045f, 0.05f, 0.06f, 0.82f)
            : selected
                ? new Color(accent.R * 0.15f, accent.G * 0.15f, accent.B * 0.15f, 0.98f)
                : new Color(0.06f, 0.08f, 0.11f, 0.94f);
        var border = locked
            ? new Color(0.25f, 0.27f, 0.30f, 0.72f)
            : selected
                ? accent
                : new Color(0.20f, 0.29f, 0.36f, 0.84f);
        button.AddThemeStyleboxOverride("normal", PanelStyle(background, border, selected ? 3 : 1));
        button.AddThemeColorOverride(
            "font_color",
            locked ? new Color(0.46f, 0.49f, 0.54f) : Colors.White);
    }

    private static PanelContainer MakePanel(
        string name,
        Color background,
        Color border,
        int borderWidth = 1)
    {
        var panel = new PanelContainer { Name = name };
        panel.AddThemeStyleboxOverride("panel", PanelStyle(background, border, borderWidth));
        return panel;
    }

    private static StyleBoxFlat PanelStyle(Color background, Color border, int width)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = width,
            BorderWidthTop = width,
            BorderWidthRight = width,
            BorderWidthBottom = width,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            CornerRadiusBottomLeft = 7,
            CornerRadiusBottomRight = 7,
            ContentMarginLeft = 8.0f,
            ContentMarginTop = 7.0f,
            ContentMarginRight = 8.0f,
            ContentMarginBottom = 7.0f,
        };
    }

    private static MarginContainer AddPanelMargin(PanelContainer panel, int marginSize)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", marginSize);
        margin.AddThemeConstantOverride("margin_top", marginSize);
        margin.AddThemeConstantOverride("margin_right", marginSize);
        margin.AddThemeConstantOverride("margin_bottom", marginSize);
        panel.AddChild(margin);
        return margin;
    }
}

public partial class ArchiveMapRouteSpine : Control
{
    public override void _Draw()
    {
        var top = 38.0f;
        var bottom = Mathf.Max(top, Size.Y - 38.0f);
        var x = 10.0f;
        DrawLine(
            new Vector2(x, top),
            new Vector2(x, bottom),
            new Color(0.24f, 0.98f, 0.95f, 0.20f),
            8.0f,
            true);
        DrawLine(
            new Vector2(x, top),
            new Vector2(x, bottom),
            new Color(0.42f, 0.94f, 0.92f, 0.88f),
            2.0f,
            true);

        for (var index = 0; index < 4; index++)
        {
            var y = Mathf.Lerp(top, bottom, index / 3.0f);
            var points = new Vector2[]
            {
                new(x, y - 7.0f),
                new(x + 7.0f, y),
                new(x, y + 7.0f),
                new(x - 7.0f, y),
            };
            DrawColoredPolygon(points, new Color(0.12f, 0.08f, 0.16f, 1.0f));
            DrawPolyline(
                new Vector2[] { points[0], points[1], points[2], points[3], points[0] },
                new Color(1.0f, 0.82f, 0.36f, 0.96f),
                2.0f,
                true);
        }
    }
}
