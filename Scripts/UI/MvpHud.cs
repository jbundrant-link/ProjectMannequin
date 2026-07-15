using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Progression;
using ProjectMannequin.Stage;

namespace ProjectMannequin.UI;

public partial class MvpHud : CanvasLayer
{
    private const int FloatingTextPoolSize = 24;
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly Queue<Label> _floatingTextPool = new();
    private readonly Queue<string> _notifications = new();
    private readonly HashSet<string> _seenEventKeys = new();
    private readonly Dictionary<EnemyEntryEdge, Label> _threatIndicators = new();
    private readonly Dictionary<EnemyEntryEdge, float> _threatIndicatorTimers = new();

    private Control _root = null!;
    private Label _objectiveLabel = null!;
    private Label _stageProgressLabel = null!;
    private Label _advanceLabel = null!;
    private Label _notificationLabel = null!;
    private Label _comboLabel = null!;
    private Label _playerNameLabel = null!;
    private Label _playerLivesLabel = null!;
    private Label _playerDetailLabel = null!;
    private Label _bossNameLabel = null!;
    private Label _bossDetailLabel = null!;
    private ProgressBar _playerHealthBar = null!;
    private ProgressBar _playerMeterBar = null!;
    private ProgressBar _bossHealthBar = null!;
    private ProgressBar _bossGuardBar = null!;
    private PanelContainer _playerPanel = null!;
    private VBoxContainer _bossPanel = null!;
    private Label _controlsLabel = null!;
    private PanelContainer _missionPanel = null!;
    private ColorRect _missionDim = null!;
    private Label _missionEyebrowLabel = null!;
    private Label _missionTitleLabel = null!;
    private Label _missionRankLabel = null!;
    private Label _missionSubtitleLabel = null!;
    private Label _missionBodyLabel = null!;
    private Label _missionUnlockLabel = null!;
    private HBoxContainer _missionButtons = null!;
    private PanelContainer _resultsConfirmationPanel = null!;
    private Label _resultsConfirmationTitle = null!;
    private Label _resultsConfirmationBody = null!;
    private Button _resultsConfirmationAccept = null!;
    private Button _resultsConfirmationCancel = null!;
    private TextureRect? _playerPortrait;
    private TextureRect? _bossPortrait;
    private ColorRect _cinematicWash = null!;
    private VBoxContainer _beamClashContainer = null!;
    private ProgressBar _beamClashBar = null!;
    private Label _beamClashLabel = null!;
    private PanelContainer _rewardPanel = null!;
    private Label _rewardTitleLabel = null!;
    private VBoxContainer _rewardOptionsBox = null!;
    private Label _rewardHintLabel = null!;
    private PanelContainer _deathPanel = null!;
    private Label _deathTitleLabel = null!;
    private Label _deathKilledByLabel = null!;
    private Label _deathTipLabel = null!;
    private Label _deathHintLabel = null!;
    private float _deathPanelSecondsRemaining;
    private ResultsFlowMode _resultsMode;
    private ResultsAction? _pendingConfirmedAction;
    private readonly List<Button> _resultButtons = new();
    private bool _resultActionCommitted;
    private bool _finalRunRetired;
    private float _resultsRevealElapsed;
    private string _formUnlockedThisStage = "";
    private bool _debugResultsFlowActive;

    private GameSimulation? _simulation;
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private float _notificationTimer;
    private float _comboTimer;
    private float _comboScale = 1.0f;
    private Color _comboColor = new(1.0f, 0.84f, 0.34f);
    private float _cinematicWashTimer;
    private bool _completionNoticeQueued;
    private bool _failureNoticeQueued;
    private float _bossIntroRevealT = 1.0f;
    private bool _bossIntroRevealActive;
    private float _bossIntroRevealElapsed;
    private float _bossIntroRevealDuration = 0.42f;

    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";
    [Export] public string MainMenuScenePath { get; set; } = "res://Scenes/UI/MainMenu.tscn";
    [Export] public string HudFramePath { get; set; } =
        "res://Assets/UI/Hud/project_mannequin_lifebar_frame_higgsfield_v1.png";
    [Export] public string PortraitSourcePath { get; set; } =
        "res://Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png";

    public float BossIntroRevealProgress => _bossIntroRevealT;
    public float HudOpacity => _root?.Modulate.A ?? 0.0f;
    public bool PlayerLifeBarVisible => _playerPanel?.Visible == true
        && _playerHealthBar?.Visible == true;
    public bool BossLifeBarVisible => _bossPanel?.Visible == true
        && _bossHealthBar?.Visible == true;
    public double PlayerHealthBarValue => _playerHealthBar?.Value ?? 0.0;
    public double BossHealthBarValue => _bossHealthBar?.Value ?? 0.0;
    public bool ResultsPanelVisible => _missionPanel?.Visible == true;
    public ResultsFlowMode ResultsMode => _resultsMode;
    public string ResultsRankText => _missionRankLabel?.Text ?? "";
    public bool ResultsConfirmationVisible => _resultsConfirmationPanel?.Visible == true;
    public string ResultsUnlockText => _missionUnlockLabel?.Text ?? "";
    public bool ResultsActionCommitted => _resultActionCommitted;
    public IReadOnlyList<string> ResultsButtonTexts => _resultButtons
        .Select(button => button.Text)
        .ToArray();

    public void DebugShowResultsForSmoke(ResultsFlowMode mode)
    {
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_RESULTS_FLOW_SMOKE_TEST") != "1")
        {
            return;
        }

        _debugResultsFlowActive = true;

        var director = _simulation?.EncounterDirector;
        if (director is null)
        {
            return;
        }

        if (mode == ResultsFlowMode.GameOver)
        {
            ShowGameOverResults(director);
            return;
        }

        if (mode == ResultsFlowMode.WorldComplete)
        {
            _formUnlockedThisStage = director.Mission.BossFormId;
        }

        ShowStageResults(
            director,
            new StageResultsData(
                director.Mission.Id,
                director.Mission.StageNumber,
                CombatScore: 8420,
                ClearBonus: director.Mission.IsFinalStage ? 10000 : 3500,
                TimeBonus: 4120,
                StageTotal: director.Mission.IsFinalStage ? 22540 : 16040,
                RunTotal: 48750,
                ActiveFrames: 7320,
                MaxCombo: 14,
                EnemiesDefeated: 9,
                Parries: 3,
                CounterHits: 4,
                DamageTaken: 86,
                Deaths: 0,
                Rank: StageRank.S));
    }

    public override void _Ready()
    {
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        BuildInterface();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        var director = _simulation?.EncounterDirector;
        if (director?.State == ArcadeStageState.AwaitingReward)
        {
            var choice = UiInputRouter.RewardChoice(inputEvent);
            if (choice >= 0 && choice < director.PendingRewardOptions.Count)
            {
                GetViewport().SetInputAsHandled();
                director.RequestRewardChoice(choice);
                return;
            }
        }

        if (director?.State == ArcadeStageState.AwaitingRouteChoice)
        {
            var routeChoice = UiInputRouter.RewardChoice(inputEvent);
            if (routeChoice >= 0 && routeChoice < director.PendingRouteChoices.Count)
            {
                GetViewport().SetInputAsHandled();
                director.RequestRouteChoice(routeChoice);
                return;
            }
        }

        if (_resultsConfirmationPanel.Visible)
        {
            if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Cancel))
            {
                HideResultsConfirmation();
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        // End-state shortcuts are scoped to the visible modal and use the same
        // one-shot action gate as buttons, preventing double stage advances.
        if (!_missionPanel.Visible || _resultsMode == ResultsFlowMode.Hidden)
        {
            return;
        }

        if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Restart))
        {
            var primary = ResultsFlowModel.PrimaryActionFor(_resultsMode);
            if (primary is not null)
            {
                RequestResultsAction(primary.Value);
                GetViewport().SetInputAsHandled();
            }
        }
        else if (UiInputRouter.IsPressed(inputEvent, LogicalUiAction.MainMenu)
            || UiInputRouter.IsPressed(inputEvent, LogicalUiAction.Cancel))
        {
            RequestResultsAction(ResultsAction.ArchiveMap);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (_simulation is null)
        {
            return;
        }

        CaptureEvents();
        UpdateHud((float)delta);
        UpdateFloatingTexts((float)delta);
    }

    private void BuildInterface()
    {
        _root = new Control
        {
            Name = "HudRoot",
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        BuildFloatingTextPool();

        AddHudFrame();
        AddPortraits();
        AddCinematicWash();
        AddThreatIndicators();

        _playerPanel = MakePanel(new Vector2(20, 20), new Vector2(370, 120));
        _playerPanel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _root.AddChild(_playerPanel);

        var playerBox = new VBoxContainer();
        playerBox.AddThemeConstantOverride("separation", 4);
        _playerPanel.AddChild(playerBox);

        var nameRow = new HBoxContainer();
        _playerNameLabel = MakeLabel("P1 Mannequin", 18, Colors.White);
        _playerLivesLabel = MakeLabel("LIVES 3", 18, Colors.Gold);
        nameRow.AddChild(_playerNameLabel);
        nameRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        nameRow.AddChild(_playerLivesLabel);
        playerBox.AddChild(nameRow);

        _playerHealthBar = MakeBar(new Color(0.22f, 0.90f, 0.82f), 16.0f);
        playerBox.AddChild(_playerHealthBar);

        _playerMeterBar = MakeBar(new Color(0.28f, 0.66f, 1.0f), 10.0f);
        playerBox.AddChild(_playerMeterBar);

        _playerDetailLabel = MakeLabel("", 14, new Color(0.82f, 0.88f, 0.94f));
        playerBox.AddChild(_playerDetailLabel);

        _bossPanel = new VBoxContainer
        {
            Name = "BossPanel",
            CustomMinimumSize = new Vector2(520, 86),
        };
        _bossPanel.AddThemeConstantOverride("separation", 5);
        _root.AddChild(_bossPanel);

        _bossNameLabel = MakeLabel("Archive Knight", 18, Colors.White);
        _bossNameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _bossPanel.AddChild(_bossNameLabel);

        _bossHealthBar = MakeBar(new Color(1.0f, 0.30f, 0.28f), 16.0f);
        _bossPanel.AddChild(_bossHealthBar);

        _bossGuardBar = MakeBar(new Color(0.95f, 0.72f, 0.22f), 9.0f);
        _bossPanel.AddChild(_bossGuardBar);

        _bossDetailLabel = MakeLabel("", 13, new Color(0.90f, 0.84f, 0.74f));
        _bossDetailLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _bossPanel.AddChild(_bossDetailLabel);

        _stageProgressLabel = MakeLabel("", 13, new Color(0.56f, 0.86f, 1.0f));
        _stageProgressLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _stageProgressLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        _stageProgressLabel.OffsetLeft = 20;
        _stageProgressLabel.OffsetTop = 112;
        _stageProgressLabel.OffsetRight = -20;
        _stageProgressLabel.OffsetBottom = 136;
        _root.AddChild(_stageProgressLabel);

        _objectiveLabel = MakeLabel("", 16, new Color(1.0f, 0.86f, 0.44f));
        _objectiveLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _objectiveLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        _objectiveLabel.OffsetLeft = 20;
        _objectiveLabel.OffsetTop = 138;
        _objectiveLabel.OffsetRight = -20;
        _objectiveLabel.OffsetBottom = 170;
        _root.AddChild(_objectiveLabel);

        _beamClashContainer = new VBoxContainer
        {
            Name = "BeamClashContainer",
            Visible = false,
        };
        _beamClashContainer.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _beamClashContainer.OffsetLeft = -200;
        _beamClashContainer.OffsetTop = 180;
        _beamClashContainer.OffsetRight = 200;
        _beamClashContainer.OffsetBottom = 260;
        _root.AddChild(_beamClashContainer);

        _beamClashLabel = MakeLabel("MASH!", 28, new Color(1.0f, 0.86f, 0.44f));
        _beamClashLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _beamClashContainer.AddChild(_beamClashLabel);

        _beamClashBar = MakeBar(new Color(1.0f, 0.8f, 0.2f), 24.0f);
        _beamClashBar.MinValue = -50;
        _beamClashBar.MaxValue = 50;
        _beamClashBar.Step = 1;
        _beamClashBar.Value = 0;
        _beamClashBar.ShowPercentage = false;
        var styleBoxBg = new StyleBoxFlat { BgColor = new Color(0.2f, 0.4f, 1.0f) };
        _beamClashBar.AddThemeStyleboxOverride("background", styleBoxBg);
        _beamClashContainer.AddChild(_beamClashBar);

        _advanceLabel = MakeLabel("GO  >", 34, new Color(0.34f, 0.94f, 0.88f));
        _advanceLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _advanceLabel.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        _advanceLabel.OffsetLeft = -210;
        _advanceLabel.OffsetTop = -42;
        _advanceLabel.OffsetRight = -34;
        _advanceLabel.OffsetBottom = 42;
        _root.AddChild(_advanceLabel);

        _notificationLabel = MakeLabel("", 28, Colors.White);
        _notificationLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _notificationLabel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _notificationLabel.OffsetLeft = -420;
        _notificationLabel.OffsetTop = 176;
        _notificationLabel.OffsetRight = 420;
        _notificationLabel.OffsetBottom = 240;
        _root.AddChild(_notificationLabel);

        _comboLabel = MakeLabel("", 30, new Color(1.0f, 0.84f, 0.34f));
        _comboLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _comboLabel.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        _comboLabel.OffsetLeft = -320;
        _comboLabel.OffsetTop = 48;
        _comboLabel.OffsetRight = -34;
        _comboLabel.OffsetBottom = 96;
        _root.AddChild(_comboLabel);

        _controlsLabel = MakeLabel("", 13, new Color(0.72f, 0.78f, 0.84f));
        _controlsLabel.Visible = false;
        _controlsLabel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        _controlsLabel.OffsetLeft = -860;
        _controlsLabel.OffsetTop = -34;
        _controlsLabel.OffsetRight = -20;
        _controlsLabel.OffsetBottom = -14;
        _root.AddChild(_controlsLabel);

        BuildResultsPanel();

        _rewardPanel = MakePanel(Vector2.Zero, new Vector2(680, 360));
        _rewardPanel.Visible = false;
        _rewardPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _rewardPanel.OffsetLeft = -340;
        _rewardPanel.OffsetTop = -180;
        _rewardPanel.OffsetRight = 340;
        _rewardPanel.OffsetBottom = 180;
        _root.AddChild(_rewardPanel);

        var rewardBox = new VBoxContainer();
        rewardBox.AddThemeConstantOverride("separation", 12);
        _rewardPanel.AddChild(rewardBox);

        _rewardTitleLabel = MakeLabel("CHOOSE A REWARD", 30, new Color(1.0f, 0.84f, 0.34f));
        _rewardTitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        rewardBox.AddChild(_rewardTitleLabel);

        _rewardOptionsBox = new VBoxContainer();
        _rewardOptionsBox.AddThemeConstantOverride("separation", 10);
        rewardBox.AddChild(_rewardOptionsBox);

        _rewardHintLabel = MakeLabel("Choose with 1 / 2 / 3 or X / Y / A.", 16, new Color(0.72f, 0.78f, 0.84f));
        _rewardHintLabel.HorizontalAlignment = HorizontalAlignment.Center;
        rewardBox.AddChild(_rewardHintLabel);

        BuildDeathPanel();
    }

    private void BuildResultsPanel()
    {
        _missionDim = new ColorRect
        {
            Name = "ResultsDim",
            Color = new Color(0.005f, 0.008f, 0.016f, 0.78f),
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false,
        };
        _missionDim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_missionDim);

        _missionPanel = MakePanel(Vector2.Zero, new Vector2(820, 530));
        _missionPanel.Name = "ResultsPanel";
        _missionPanel.Visible = false;
        _missionPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _missionPanel.AddThemeStyleboxOverride("panel", ResultPanelStyle(
            new Color(0.025f, 0.035f, 0.055f, 0.99f),
            new Color(0.42f, 0.78f, 0.92f, 0.95f),
            2));
        _root.AddChild(_missionPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        _missionPanel.AddChild(margin);

        var missionBox = new VBoxContainer();
        missionBox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(missionBox);

        _missionEyebrowLabel = MakeLabel("PROJECT MANNEQUIN  //  RESULTS", 13, new Color(0.42f, 0.94f, 0.92f));
        _missionEyebrowLabel.HorizontalAlignment = HorizontalAlignment.Center;
        missionBox.AddChild(_missionEyebrowLabel);

        _missionTitleLabel = MakeLabel("STAGE CLEAR", 34, Colors.White);
        _missionTitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        missionBox.AddChild(_missionTitleLabel);

        _missionRankLabel = MakeLabel("A", 82, new Color(1.0f, 0.82f, 0.30f));
        _missionRankLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _missionRankLabel.CustomMinimumSize = new Vector2(0.0f, 94.0f);
        _missionRankLabel.PivotOffset = new Vector2(380.0f, 47.0f);
        missionBox.AddChild(_missionRankLabel);

        _missionSubtitleLabel = MakeLabel("", 16, new Color(0.70f, 0.80f, 0.90f));
        _missionSubtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        missionBox.AddChild(_missionSubtitleLabel);
        missionBox.AddChild(new HSeparator());

        _missionBodyLabel = MakeLabel("", 16, new Color(0.84f, 0.89f, 0.95f));
        _missionBodyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _missionBodyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _missionBodyLabel.CustomMinimumSize = new Vector2(0.0f, 132.0f);
        missionBox.AddChild(_missionBodyLabel);

        _missionUnlockLabel = MakeLabel("", 17, new Color(1.0f, 0.78f, 0.30f));
        _missionUnlockLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _missionUnlockLabel.Visible = false;
        missionBox.AddChild(_missionUnlockLabel);

        _missionButtons = new HBoxContainer
        {
            Name = "ResultsButtons",
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _missionButtons.AddThemeConstantOverride("separation", 12);
        missionBox.AddChild(_missionButtons);

        var hint = MakeLabel(
            "ACCEPT / R  PRIMARY ACTION      CANCEL / M  ARCHIVE MAP",
            12,
            new Color(0.52f, 0.61f, 0.70f));
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        missionBox.AddChild(hint);

        _resultsConfirmationPanel = MakePanel(Vector2.Zero, new Vector2(610, 280));
        _resultsConfirmationPanel.Name = "ResultsConfirmationPanel";
        _resultsConfirmationPanel.Visible = false;
        _resultsConfirmationPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        _resultsConfirmationPanel.AddThemeStyleboxOverride("panel", ResultPanelStyle(
            new Color(0.035f, 0.043f, 0.065f, 1.0f),
            new Color(1.0f, 0.56f, 0.24f, 0.96f),
            3));
        _root.AddChild(_resultsConfirmationPanel);

        var confirmMargin = new MarginContainer();
        confirmMargin.AddThemeConstantOverride("margin_left", 26);
        confirmMargin.AddThemeConstantOverride("margin_top", 24);
        confirmMargin.AddThemeConstantOverride("margin_right", 26);
        confirmMargin.AddThemeConstantOverride("margin_bottom", 24);
        _resultsConfirmationPanel.AddChild(confirmMargin);
        var confirmBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        confirmBox.AddThemeConstantOverride("separation", 16);
        confirmMargin.AddChild(confirmBox);
        _resultsConfirmationTitle = MakeLabel("RESTART WORLD?", 27, new Color(1.0f, 0.70f, 0.30f));
        _resultsConfirmationTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _resultsConfirmationBody = MakeLabel("", 16, new Color(0.86f, 0.90f, 0.95f));
        _resultsConfirmationBody.HorizontalAlignment = HorizontalAlignment.Center;
        _resultsConfirmationBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        confirmBox.AddChild(_resultsConfirmationTitle);
        confirmBox.AddChild(_resultsConfirmationBody);
        var confirmButtons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        confirmButtons.AddThemeConstantOverride("separation", 12);
        _resultsConfirmationAccept = MakeResultButton("CONFIRM", ResultsAction.RestartWorld);
        _resultsConfirmationAccept.Name = "ResultsConfirmAccept";
        _resultsConfirmationCancel = MakeResultButton("KEEP CHECKPOINT", ResultsAction.ArchiveMap);
        _resultsConfirmationCancel.Name = "ResultsConfirmCancel";
        _resultsConfirmationAccept.Pressed += ConfirmPendingResultsAction;
        _resultsConfirmationCancel.Pressed += HideResultsConfirmation;
        confirmButtons.AddChild(_resultsConfirmationAccept);
        confirmButtons.AddChild(_resultsConfirmationCancel);
        confirmBox.AddChild(confirmButtons);
    }

    private void BuildDeathPanel()
    {
        _deathPanel = MakePanel(Vector2.Zero, new Vector2(760, 300));
        _deathPanel.Visible = false;
        _deathPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _deathPanel.OffsetLeft = -380;
        _deathPanel.OffsetTop = -150;
        _deathPanel.OffsetRight = 380;
        _deathPanel.OffsetBottom = 150;

        var background = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.08f, 0.92f),
            BorderColor = new Color(0.78f, 0.18f, 0.2f, 0.95f),
            ContentMarginLeft = 28,
            ContentMarginRight = 28,
            ContentMarginTop = 22,
            ContentMarginBottom = 22,
        };
        background.SetBorderWidthAll(3);
        background.SetCornerRadiusAll(8);
        _deathPanel.AddThemeStyleboxOverride("panel", background);
        _root.AddChild(_deathPanel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 14);
        _deathPanel.AddChild(box);

        _deathTitleLabel = MakeLabel("DEFEATED", 44, new Color(1.0f, 0.32f, 0.28f));
        _deathTitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(_deathTitleLabel);

        _deathKilledByLabel = MakeLabel("", 22, new Color(1.0f, 0.86f, 0.6f));
        _deathKilledByLabel.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(_deathKilledByLabel);

        _deathTipLabel = MakeLabel("", 18, new Color(0.82f, 0.9f, 0.98f));
        _deathTipLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _deathTipLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _deathTipLabel.CustomMinimumSize = new Vector2(700, 0);
        box.AddChild(_deathTipLabel);

        _deathHintLabel = MakeLabel("", 16, new Color(0.68f, 0.74f, 0.82f));
        _deathHintLabel.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(_deathHintLabel);
    }

    private void UpdateHud(float delta)
    {
        var player = _simulation!.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
        var boss = _simulation.Actors.FirstOrDefault(actor => actor.IsBoss || actor.IsElite);
        var director = _simulation.EncounterDirector;

        UpdateBossIntroReveal(delta);
        UpdatePlayerHud(player);
        UpdateBossHud(boss);
        UpdateStageFlow(director);
        UpdateObjective(player, boss, director);
        UpdateMissionPanel(player, director);
        UpdateResultsAnimation((float)delta);
        UpdateNotification(delta);
        UpdateDeathScreen(delta);
        UpdateComboDisplay(delta);
        UpdateCinematicWash(delta);
        UpdateThreatIndicators(delta);
        PositionPlayerPanel();
        PositionBossPanel();
        PositionPortraits();
        PositionMissionPanel();
    }

    private void AddHudFrame()
    {
        if (!ResourceLoader.Exists(HudFramePath))
        {
            GD.PushWarning($"HUD frame texture not found: {HudFramePath}");
            return;
        }

        var frame = new TextureRect
        {
            Name = "HiggsfieldHudFrame",
            Texture = GD.Load<Texture2D>(HudFramePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        frame.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(frame);
    }

    private void AddPortraits()
    {
        if (!ResourceLoader.Exists(PortraitSourcePath))
        {
            GD.PushWarning($"HUD portrait texture not found: {PortraitSourcePath}");
            return;
        }

        var source = GD.Load<Texture2D>(PortraitSourcePath);
        var sourceSize = source.GetSize();
        var portraitRegion = new Rect2(
            sourceSize.X * 0.29f,
            sourceSize.Y * 0.04f,
            sourceSize.X * 0.42f,
            sourceSize.Y * 0.47f);

        _playerPortrait = MakePortrait(source, portraitRegion, Colors.White);
        _playerPortrait.Name = "PlayerPortrait";
        _root.AddChild(_playerPortrait);

        _bossPortrait = MakePortrait(source, portraitRegion, new Color(1.0f, 0.68f, 0.68f));
        _bossPortrait.Name = "BossPortrait";
        _root.AddChild(_bossPortrait);
    }

    private void AddCinematicWash()
    {
        _cinematicWash = new ColorRect
        {
            Name = "CinematicWash",
            Color = new Color(0.02f, 0.025f, 0.06f, 0.0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        _cinematicWash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_cinematicWash);
    }

    private void UpdatePlayerHud(CombatActor? player)
    {
        if (player is null)
        {
            _playerNameLabel.Text = "P1 Offline";
            _playerHealthBar.Value = 0;
            _playerMeterBar.Value = 0;
            _playerDetailLabel.Text = "";
            return;
        }

        _playerNameLabel.Text = $"P{player.PlayerId} {player.CurrentForm.DisplayName}";
        _playerLivesLabel.Text = $"LIVES {ProjectMannequin.Progression.RunSessionManager.Instance.RemainingLives}";
        _playerHealthBar.MaxValue = player.CurrentForm.MaxHealth;
        _playerHealthBar.Value = player.Health;
        _playerMeterBar.MaxValue = player.CurrentForm.MaxMeter;
        _playerMeterBar.Value = player.Meter;

        var archivedForms = Mathf.Max(0, player.FormArchive.ActiveLoadout.Count - 1);
        var swap = player.FormArchive.ActiveLoadout.Count > 1
            ? $" | Swap {player.StateMachine.FormSwapCooldownFrames}f"
            : "";
        _playerDetailLabel.Text =
            $"Meter {player.Meter}/{player.CurrentForm.MaxMeter} | Archived {archivedForms}{swap}";
    }

    private void UpdateBossHud(CombatActor? boss)
    {
        _bossPanel.Visible = boss is not null && !boss.IsDead;
        if (_bossPortrait is not null)
        {
            _bossPortrait.Visible = _bossPanel.Visible;
        }

        if (boss is null)
        {
            return;
        }

        _bossNameLabel.Text = boss.CurrentForm.DisplayName;
        _bossHealthBar.MaxValue = boss.CurrentForm.MaxHealth;
        _bossHealthBar.Value = boss.Health;
        _bossGuardBar.Visible = boss.CurrentForm.MaxGuardGauge > 0;
        _bossGuardBar.MaxValue = Mathf.Max(1, boss.CurrentForm.MaxGuardGauge);
        _bossGuardBar.Value = boss.GuardGauge;
        var phase = boss.CurrentBossPhase;
        var phaseText = phase is null
            ? boss.IsElite ? "NAMED ELITE" : "ARCHIVE BOSS"
            : $"PHASE {boss.CurrentBossPhaseIndex + 1}/{boss.CurrentForm.BossPhases.Count}: {phase.DisplayName.ToUpperInvariant()}";
        var transitionText = boss.State == CombatActorState.CinematicLocked ? " | SHIFTING" : "";
        var guardText = boss.State == CombatActorState.GuardBreak
            ? " | RESOLVE BROKEN"
            : boss.CurrentForm.MaxGuardGauge > 0
                ? $" | RESOLVE {boss.GuardGauge}/{boss.CurrentForm.MaxGuardGauge}"
                : "";
        _bossDetailLabel.Text =
            $"{phaseText}{transitionText} | HP {boss.Health}/{boss.CurrentForm.MaxHealth}{guardText}";
    }

    private void UpdateStageFlow(ArcadeEncounterDirector? director)
    {
        if (director is null)
        {
            _stageProgressLabel.Text = "";
            _advanceLabel.Visible = false;
            return;
        }

        _stageProgressLabel.Text =
            $"SCORE {ProjectMannequin.Progression.RunSessionManager.Instance.ScoreManager.RunScore:000000}    " +
            $"TIME {FormatTime(ProjectMannequin.Progression.RunSessionManager.Instance.ScoreManager.ActiveGameplayFrames)}" +
            $" / PAR {FormatTime(Mathf.RoundToInt(director.Mission.ParTimeSeconds * GameConstants.TickRate))}    " +
            $"{director.Mission.WorldDisplayName.ToUpperInvariant()} // STAGE {director.Mission.StageNumber} // " +
            $"{director.Mission.DisplayName.ToUpperInvariant()}    " +
            $"ENCOUNTER {director.CurrentEncounterNumber}/{director.TotalEncounterCount}    " +
            $"HOSTILES {director.EnemiesRemaining}";

        _advanceLabel.Visible = director.ShowAdvancePrompt;
        if (_advanceLabel.Visible)
        {
            var pulse = 0.62f + Mathf.Sin(Time.GetTicksMsec() * 0.008f) * 0.28f;
            _advanceLabel.Modulate = new Color(0.34f, 0.94f, 0.88f, pulse);
        }
    }

    private void UpdateObjective(
        CombatActor? player,
        CombatActor? boss,
        ArcadeEncounterDirector? director)
    {
        if (player is null)
        {
            _objectiveLabel.Text = "Connect a controller or keyboard for Player 1.";
            return;
        }

        if (player.IsDead)
        {
            _objectiveLabel.Text = "Run ended. Retry the stage or return to the Archive Map.";
            return;
        }

        if (director?.State == ArcadeStageState.Complete)
        {
            _objectiveLabel.Text = director.CurrentObjective;
            return;
        }

        if (director is not null)
        {
            _objectiveLabel.Text = director.CurrentObjective;
            return;
        }

        if (boss is { IsDead: false })
        {
            _objectiveLabel.Text =
                $"Objective: Defeat {boss.CurrentForm.DisplayName} and archive its form.";
            return;
        }

        if (player.FormArchive.ActiveLoadout.Count > 1)
        {
            _objectiveLabel.Text = "Archive complete. Press Q to shapeshift into the unlocked boss form.";
            return;
        }

        _objectiveLabel.Text = "Clear the arena.";
    }

    private void UpdateMissionPanel(CombatActor? player, ArcadeEncounterDirector? director)
    {
        if (_debugResultsFlowActive)
        {
            return;
        }

        if (player is null)
        {
            HideResultsFlow();
            return;
        }

        if (player.IsDead)
        {
            ShowGameOverResults(director);

            if (!_failureNoticeQueued)
            {
                _failureNoticeQueued = true;
                _notifications.Enqueue("PLAYER DEFEATED");
            }

            return;
        }

        if (director?.State == ArcadeStageState.Complete)
        {
            var results = RunSessionManager.Instance.ScoreManager.LastStageResults;
            if (results is null)
            {
                return;
            }

            ShowStageResults(director, results);

            if (!_completionNoticeQueued)
            {
                _completionNoticeQueued = true;
                _notifications.Enqueue("STAGE COMPLETE");
            }

            return;
        }

        HideResultsFlow();
    }

    private void ShowStageResults(
        ArcadeEncounterDirector director,
        StageResultsData results)
    {
        var mode = director.Mission.IsFinalStage
            ? ResultsFlowMode.WorldComplete
            : ResultsFlowMode.StageClear;
        if (_resultsMode == mode && _missionPanel.Visible)
        {
            return;
        }

        if (mode == ResultsFlowMode.WorldComplete && !_finalRunRetired)
        {
            // Final results are already persisted by RunScoreManager. Retire the
            // checkpoint now so closing the app on this modal cannot resurrect
            // a completed Stage 4 run.
            RunSessionManager.Instance.CompleteRun();
            _finalRunRetired = true;
        }

        _resultsMode = mode;
        _resultActionCommitted = false;
        _resultsRevealElapsed = 0.0f;
        _missionEyebrowLabel.Text = mode == ResultsFlowMode.WorldComplete
            ? "PROJECT MANNEQUIN  //  WORLD ARCHIVED"
            : "PROJECT MANNEQUIN  //  STAGE RESULTS";
        _missionTitleLabel.Text = mode == ResultsFlowMode.WorldComplete
            ? "WORLD COMPLETE"
            : "STAGE CLEAR";
        _missionTitleLabel.Modulate = Colors.White;
        _missionRankLabel.Text = results.Rank.ToString();
        _missionRankLabel.Modulate = RankColor(results.Rank);
        _missionRankLabel.Visible = true;
        _missionSubtitleLabel.Text =
            $"{director.Mission.WorldDisplayName.ToUpperInvariant()}  //  "
            + $"STAGE {director.Mission.StageNumber}  //  "
            + director.Mission.StageTitle.ToUpperInvariant();
        _missionBodyLabel.Text =
            $"COMBAT SCORE  {results.CombatScore,7}        CLEAR BONUS  {results.ClearBonus,7}\n"
            + $"TIME BONUS   {results.TimeBonus,7}        CLEAR TIME   {FormatTime(results.ActiveFrames),7}\n"
            + $"MAX COMBO    {results.MaxCombo,7}        DAMAGE TAKEN {results.DamageTaken,7}\n"
            + $"ENEMIES      {results.EnemiesDefeated,7}        PARRIES      {results.Parries,7}\n\n"
            + $"STAGE TOTAL  {results.StageTotal,7}        RUN TOTAL     {results.RunTotal,7}";
        _missionUnlockLabel.Visible = mode == ResultsFlowMode.WorldComplete
            && !string.IsNullOrWhiteSpace(_formUnlockedThisStage);
        _missionUnlockLabel.Text = _missionUnlockLabel.Visible
            ? $"FORM ARCHIVED  //  {ResolveFormDisplayName(_formUnlockedThisStage).ToUpperInvariant()}"
            : mode == ResultsFlowMode.WorldComplete
                ? "CHAMPION RECORD SECURED"
                : "STAGE CHECKPOINT READY";
        _missionUnlockLabel.Visible = true;
        BuildResultsButtons(mode);
        ShowResultsPanel();
    }

    private void ShowGameOverResults(ArcadeEncounterDirector? director)
    {
        if (_resultsMode == ResultsFlowMode.GameOver && _missionPanel.Visible)
        {
            return;
        }

        _resultsMode = ResultsFlowMode.GameOver;
        _resultActionCommitted = false;
        _resultsRevealElapsed = 0.0f;
        _deathPanel.Visible = false;
        _missionEyebrowLabel.Text = "PROJECT MANNEQUIN  //  RUN INTERRUPTED";
        _missionTitleLabel.Text = "GAME OVER";
        _missionTitleLabel.Modulate = new Color(1.0f, 0.38f, 0.30f);
        _missionRankLabel.Text = "KO";
        _missionRankLabel.Modulate = new Color(1.0f, 0.34f, 0.28f);
        _missionRankLabel.Visible = true;
        _missionSubtitleLabel.Text = director is null
            ? "ARCHIVE CONNECTION LOST"
            : $"{director.Mission.WorldDisplayName.ToUpperInvariant()}  //  STAGE {director.Mission.StageNumber}";
        _missionBodyLabel.Text =
            "The current stage attempt has ended. Retry restores the committed "
            + "stage-entry health, meter, lives, score, and build. Restart World "
            + "creates a fresh Stage 1 checkpoint.";
        _missionUnlockLabel.Text = "YOUR COMMITTED CHECKPOINT REMAINS SAFE";
        _missionUnlockLabel.Visible = true;
        BuildResultsButtons(ResultsFlowMode.GameOver);
        ShowResultsPanel();
    }

    private void ShowResultsPanel()
    {
        _missionDim.Visible = true;
        _missionPanel.Visible = true;
        _resultsConfirmationPanel.Visible = false;
        _missionPanel.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        _missionRankLabel.Scale = Vector2.One * 0.68f;
        FocusPrimaryResultsButton();
    }

    private void HideResultsFlow()
    {
        if (_resultsMode == ResultsFlowMode.Hidden)
        {
            return;
        }

        _resultsMode = ResultsFlowMode.Hidden;
        _missionPanel.Visible = false;
        _missionDim.Visible = false;
        _resultsConfirmationPanel.Visible = false;
        _pendingConfirmedAction = null;
    }

    private void UpdateResultsAnimation(float delta)
    {
        if (!_missionPanel.Visible)
        {
            return;
        }

        _resultsRevealElapsed += delta;
        var fade = Mathf.Clamp(_resultsRevealElapsed / 0.24f, 0.0f, 1.0f);
        _missionPanel.Modulate = new Color(1.0f, 1.0f, 1.0f, fade);
        var t = Mathf.Clamp(_resultsRevealElapsed / 0.38f, 0.0f, 1.0f);
        var overshoot = 1.70158f;
        var shifted = t - 1.0f;
        var eased = 1.0f + (overshoot + 1.0f) * shifted * shifted * shifted
            + overshoot * shifted * shifted;
        _missionRankLabel.Scale = Vector2.One * Mathf.Lerp(0.68f, 1.0f, eased);
    }

    private void BuildResultsButtons(ResultsFlowMode mode)
    {
        foreach (var child in _missionButtons.GetChildren())
        {
            child.QueueFree();
        }

        _resultButtons.Clear();
        foreach (var spec in ResultsFlowModel.ActionsFor(mode))
        {
            var captured = spec.Action;
            var button = MakeResultButton(spec.Label, captured);
            button.Name = $"ResultsAction_{captured}";
            button.Pressed += () => RequestResultsAction(captured);
            if (spec.IsPrimary)
            {
                button.AddThemeColorOverride("font_color", new Color(0.90f, 1.0f, 0.96f));
            }

            _resultButtons.Add(button);
            _missionButtons.AddChild(button);
        }
    }

    private Button MakeResultButton(string text, ResultsAction action)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(180.0f, 46.0f),
            FocusMode = Control.FocusModeEnum.All,
        };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeStyleboxOverride("normal", ResultPanelStyle(
            new Color(0.065f, 0.085f, 0.12f, 0.98f),
            new Color(0.22f, 0.36f, 0.45f, 0.90f),
            1));
        button.AddThemeStyleboxOverride("hover", ResultPanelStyle(
            new Color(0.09f, 0.14f, 0.18f, 1.0f),
            new Color(0.42f, 0.94f, 0.92f),
            2));
        button.AddThemeStyleboxOverride("focus", ResultPanelStyle(
            new Color(0.11f, 0.15f, 0.20f, 1.0f),
            new Color(1.0f, 0.78f, 0.28f),
            3));
        button.AddThemeStyleboxOverride("pressed", ResultPanelStyle(
            new Color(0.12f, 0.18f, 0.22f, 1.0f),
            new Color(0.56f, 0.86f, 1.0f),
            2));
        return button;
    }

    private void FocusPrimaryResultsButton()
    {
        var primary = ResultsFlowModel.PrimaryActionFor(_resultsMode);
        var button = primary is null
            ? _resultButtons.FirstOrDefault()
            : _resultButtons.FirstOrDefault(candidate =>
                candidate.Name == $"ResultsAction_{primary.Value}");
        button?.CallDeferred("grab_focus");
    }

    private void RequestResultsAction(ResultsAction action)
    {
        if (_resultActionCommitted || SceneFlowCoordinator.Instance?.IsTransitioning == true)
        {
            return;
        }

        var spec = ResultsFlowModel.ActionsFor(_resultsMode)
            .FirstOrDefault(candidate => candidate.Action == action);
        if (spec is null)
        {
            return;
        }

        if (spec.RequiresConfirmation)
        {
            ShowResultsConfirmation(action);
            return;
        }

        ExecuteResultsAction(action);
    }

    private void ShowResultsConfirmation(ResultsAction action)
    {
        _pendingConfirmedAction = action;
        _resultsConfirmationTitle.Text = action == ResultsAction.ReplayWorld
            ? "REPLAY WORLD?"
            : "RESTART WORLD?";
        _resultsConfirmationBody.Text = action == ResultsAction.ReplayWorld
            ? "Begin a new Stage 1 run in this realm? Permanent best ranks, scores, times, forms, and lore remain safe."
            : "Discard the current run checkpoint and begin again at Stage 1? Permanent Archive records remain safe.";
        _resultsConfirmationAccept.Text = action == ResultsAction.ReplayWorld
            ? "BEGIN REPLAY"
            : "RESTART WORLD";
        _resultsConfirmationPanel.Visible = true;
        PositionMissionPanel();
        _resultsConfirmationCancel.CallDeferred("grab_focus");
    }

    private void HideResultsConfirmation()
    {
        _resultsConfirmationPanel.Visible = false;
        _pendingConfirmedAction = null;
        FocusPrimaryResultsButton();
    }

    private void ConfirmPendingResultsAction()
    {
        var action = _pendingConfirmedAction;
        _resultsConfirmationPanel.Visible = false;
        _pendingConfirmedAction = null;
        if (action is not null)
        {
            ExecuteResultsAction(action.Value);
        }
    }

    private void ExecuteResultsAction(ResultsAction action)
    {
        if (_resultActionCommitted)
        {
            return;
        }

        var director = _simulation?.EncounterDirector;
        var player = _simulation?.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
        if (director is null)
        {
            return;
        }

        _resultActionCommitted = true;
        foreach (var button in _resultButtons)
        {
            button.Disabled = true;
        }

        switch (action)
        {
            case ResultsAction.NextStage:
                if (player is not null && CommitNextStage(player, director))
                {
                    SceneFlowCoordinator.Reload(
                        GetTree(),
                        $"LOADING STAGE {director.Mission.StageNumber + 1}");
                    return;
                }
                break;
            case ResultsAction.RetryStage:
                RunSessionManager.Instance.RestoreStageCheckpoint();
                SceneFlowCoordinator.Reload(GetTree(), "RETRYING STAGE");
                return;
            case ResultsAction.ArchiveMap:
                if (_resultsMode == ResultsFlowMode.StageClear && player is not null)
                {
                    CommitNextStage(player, director);
                }
                else if (_resultsMode == ResultsFlowMode.GameOver)
                {
                    RunSessionManager.Instance.RestoreStageCheckpoint();
                }

                SceneFlowCoordinator.TransitionTo(
                    GetTree(),
                    MainMenuScenePath,
                    _resultsMode == ResultsFlowMode.WorldComplete
                        ? "WORLD ARCHIVED // RETURNING TO MAP"
                        : "RETURNING TO ARCHIVE MAP");
                return;
            case ResultsAction.ReplayWorld:
            case ResultsAction.RestartWorld:
                RunSessionManager.Instance.StartNewRun(director.Mission.WorldId);
                SceneFlowCoordinator.Reload(GetTree(), "RESTARTING WORLD // STAGE 1");
                return;
        }

        _resultActionCommitted = false;
        foreach (var button in _resultButtons)
        {
            button.Disabled = false;
        }
    }

    private static bool CommitNextStage(
        CombatActor player,
        ArcadeEncounterDirector director)
    {
        var stageCount = WorldRunCatalog.CreateRun(director.Mission.WorldId).Stages.Count;
        return RunSessionManager.Instance.AdvanceToNextStage(player, stageCount);
    }

    private static Color RankColor(StageRank rank)
    {
        return rank switch
        {
            StageRank.S => new Color(1.0f, 0.78f, 0.24f),
            StageRank.A => new Color(0.72f, 0.92f, 1.0f),
            StageRank.B => new Color(0.42f, 0.94f, 0.82f),
            StageRank.C => new Color(0.80f, 0.68f, 0.48f),
            _ => new Color(0.62f, 0.65f, 0.70f),
        };
    }

    private static StyleBoxFlat ResultPanelStyle(Color background, Color border, int width)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = width,
            BorderWidthTop = width,
            BorderWidthRight = width,
            BorderWidthBottom = width,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 10.0f,
            ContentMarginTop = 8.0f,
            ContentMarginRight = 10.0f,
            ContentMarginBottom = 8.0f,
        };
    }

    private void UpdateNotification(float delta)
    {
        if (_notificationTimer > 0.0f)
        {
            _notificationTimer -= delta;
            _notificationLabel.Modulate = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp(_notificationTimer, 0.0f, 1.0f));
            return;
        }

        if (_notifications.Count == 0)
        {
            _notificationLabel.Text = "";
            return;
        }

        _notificationLabel.Text = _notifications.Dequeue();
        _notificationLabel.Modulate = Colors.White;
        _notificationTimer = 2.7f;
    }

    private void UpdateComboDisplay(float delta)
    {
        if (_comboTimer <= 0.0f)
        {
            _comboLabel.Text = "";
            return;
        }

        _comboTimer -= delta;
        _comboScale = Mathf.MoveToward(_comboScale, 1.0f, delta * 3.8f);
        _comboLabel.Scale = Vector2.One * _comboScale;
        _comboLabel.Modulate = new Color(
            _comboColor.R,
            _comboColor.G,
            _comboColor.B,
            Mathf.Clamp(_comboTimer * 2.0f, 0.0f, 1.0f));
    }

    private void UpdateCinematicWash(float delta)
    {
        if (_cinematicWashTimer <= 0.0f)
        {
            _cinematicWash.Visible = false;
            return;
        }

        _cinematicWashTimer = Mathf.Max(0.0f, _cinematicWashTimer - delta);
        _cinematicWash.Visible = true;
        var alpha = Mathf.Clamp(_cinematicWashTimer * 0.85f, 0.0f, 0.42f);
        _cinematicWash.Color = new Color(0.02f, 0.025f, 0.06f, alpha);
    }

    private void AddThreatIndicators()
    {
        AddThreatIndicator(
            EnemyEntryEdge.Left,
            "< !",
            Control.LayoutPreset.CenterLeft,
            new Rect2(24, -28, 90, 56),
            HorizontalAlignment.Left);
        AddThreatIndicator(
            EnemyEntryEdge.Right,
            "! >",
            Control.LayoutPreset.CenterRight,
            new Rect2(-114, -28, 90, 56),
            HorizontalAlignment.Right);
        AddThreatIndicator(
            EnemyEntryEdge.FarLane,
            "! ^",
            Control.LayoutPreset.CenterTop,
            new Rect2(-45, 194, 90, 56),
            HorizontalAlignment.Center);
        AddThreatIndicator(
            EnemyEntryEdge.NearLane,
            "! v",
            Control.LayoutPreset.CenterBottom,
            new Rect2(-45, -112, 90, 56),
            HorizontalAlignment.Center);
    }

    private void AddThreatIndicator(
        EnemyEntryEdge edge,
        string text,
        Control.LayoutPreset preset,
        Rect2 offsets,
        HorizontalAlignment alignment)
    {
        var label = MakeLabel(text, 30, new Color(1.0f, 0.32f, 0.24f));
        label.Name = $"{edge}ThreatIndicator";
        label.HorizontalAlignment = alignment;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.Visible = false;
        label.SetAnchorsPreset(preset);
        label.OffsetLeft = offsets.Position.X;
        label.OffsetTop = offsets.Position.Y;
        label.OffsetRight = offsets.End.X;
        label.OffsetBottom = offsets.End.Y;
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        _root.AddChild(label);
        _threatIndicators[edge] = label;
        _threatIndicatorTimers[edge] = 0.0f;
    }

    private void UpdateThreatIndicators(float delta)
    {
        foreach (var pair in _threatIndicators)
        {
            var edge = pair.Key;
            var timer = Mathf.Max(0.0f, _threatIndicatorTimers[edge] - delta);
            _threatIndicatorTimers[edge] = timer;
            pair.Value.Visible = timer > 0.0f;
            pair.Value.Modulate = new Color(
                1.0f,
                1.0f,
                1.0f,
                Mathf.Clamp(timer * 2.5f, 0.0f, 1.0f));
        }
    }

    private void CaptureEvents()
    {
        if (_simulation is null)
        {
            return;
        }

        _presentationEventBuffer.Clear();
        _presentationEventCursor = _simulation.CopyPresentationEventsSince(
            _presentationEventCursor,
            _presentationEventBuffer);
        foreach (var presentationEvent in _presentationEventBuffer)
        {
            var key = $"{presentationEvent.Tick}:{presentationEvent.Type}:{presentationEvent.SourceActorId}:{presentationEvent.TargetActorId}:{presentationEvent.Payload}";
            if (!_seenEventKeys.Add(key))
            {
                continue;
            }

            if (_seenEventKeys.Count > 128)
            {
                _seenEventKeys.Clear();
            }

            HandlePresentationEvent(presentationEvent);
        }
    }

    private void HandlePresentationEvent(CombatPresentationEvent presentationEvent)
    {
        switch (presentationEvent.Type)
        {
            case CombatPresentationEventType.HitConnected:
            case CombatPresentationEventType.LauncherHit:
            case CombatPresentationEventType.CounterHit:
            case CombatPresentationEventType.PunishCounter:
                HandleHitEvent(presentationEvent);
                if (presentationEvent.Type == CombatPresentationEventType.CounterHit)
                {
                    _notifications.Enqueue("COUNTER HIT");
                }
                else if (presentationEvent.Type == CombatPresentationEventType.PunishCounter)
                {
                    _notifications.Enqueue("PUNISH COUNTER");
                }
                break;
            case CombatPresentationEventType.Parried:
                _notifications.Enqueue(presentationEvent.Payload == "perfect"
                    ? "PERFECT PARRY"
                    : "PARRY");
                break;
            case CombatPresentationEventType.GuardBroken:
                _notifications.Enqueue("RESOLVE BROKEN");
                break;
            case CombatPresentationEventType.GuardRecovered:
                _notifications.Enqueue("RESOLVE RECOVERED");
                break;
            case CombatPresentationEventType.ComboUpdated:
                if (int.TryParse(presentationEvent.Payload, out var comboHits) && comboHits >= 2)
                {
                    var praise = comboHits switch
                    {
                        >= 40 => "ARCHIVE BREAK!",
                        >= 20 => "SUPER!",
                        >= 10 => "GREAT!",
                        >= 5 => "NICE!",
                        _ => "CHAIN",
                    };
                    _comboColor = comboHits switch
                    {
                        >= 40 => new Color(1.0f, 0.35f, 0.90f),
                        >= 20 => new Color(1.0f, 0.34f, 0.18f),
                        >= 10 => new Color(1.0f, 0.68f, 0.18f),
                        >= 5 => new Color(0.38f, 0.96f, 0.88f),
                        _ => new Color(1.0f, 0.84f, 0.34f),
                    };
                    _comboLabel.Text = $"{comboHits} HITS  •  {praise}";
                    _comboLabel.Modulate = _comboColor;
                    _comboScale = comboHits >= 20 ? 1.22f : comboHits >= 10 ? 1.14f : 1.08f;
                    _comboLabel.Scale = Vector2.One * _comboScale;
                    _comboTimer = 1.35f;
                }
                break;
            case CombatPresentationEventType.FormUnlocked:
                _formUnlockedThisStage = presentationEvent.Payload;
                _notifications.Enqueue($"FORM ARCHIVED: {ResolveFormDisplayName(presentationEvent.Payload)}");
                _notifications.Enqueue("Press Q to shapeshift.");
                break;
            case CombatPresentationEventType.FormSwapCompleted:
                _notifications.Enqueue($"SHAPESHIFT COMPLETE: {presentationEvent.Payload}");
                break;
            case CombatPresentationEventType.FightRoomLocked:
                _notifications.Enqueue("AREA SEALED");
                break;
            case CombatPresentationEventType.FightRoomOpened:
                _notifications.Enqueue("ROUTE OPEN");
                break;
            case CombatPresentationEventType.WaveStarted:
                _notifications.Enqueue($"WAVE: {presentationEvent.Payload}");
                break;
            case CombatPresentationEventType.WaveCleared:
                _notifications.Enqueue($"WAVE CLEAR: {presentationEvent.Payload}");
                break;
            case CombatPresentationEventType.WaveRewardGranted:
                var rewards = presentationEvent.Payload.Split('|');
                var healthReward = rewards.Length > 0 ? rewards[0] : "0";
                var meterReward = rewards.Length > 1 ? rewards[1] : "0";
                _notifications.Enqueue($"WAVE BONUS: HP +{healthReward} / METER +{meterReward}");
                break;
            case CombatPresentationEventType.PlayerCatchUpStarted:
                _notifications.Enqueue("TEAM CATCH-UP");
                break;
            case CombatPresentationEventType.PlayerCatchUpWarped:
                _notifications.Enqueue("TEAM REGROUPED");
                break;
            case CombatPresentationEventType.EnemyEntryWarning:
                SetThreatIndicator(presentationEvent.Payload, 1.15f);
                break;
            case CombatPresentationEventType.EnemyEntered:
                SetThreatIndicator(presentationEvent.Payload, 0.18f);
                break;
            case CombatPresentationEventType.HazardZoneWarning:
                ShowImmediateNotification(presentationEvent.Payload, 1.0f);
                break;
            case CombatPresentationEventType.HazardZoneDamage:
                var hazardTarget = FindActor(presentationEvent.TargetActorId);
                if (hazardTarget != null)
                {
                    SpawnFloatingText(presentationEvent.Payload, hazardTarget, new Color(1.0f, 0.2f, 0.2f));
                }
                break;
            case CombatPresentationEventType.EncounterStarted:
                _notifications.Enqueue($"ENCOUNTER: {presentationEvent.Payload}");
                break;
            case CombatPresentationEventType.EncounterCleared:
                _notifications.Enqueue($"SECTOR CLEAR: {presentationEvent.Payload}");
                break;
            case CombatPresentationEventType.StageAdvanceOpened:
                _notifications.Enqueue("ADVANCE");
                break;
            case CombatPresentationEventType.HealthRestored:
                _notifications.Enqueue($"RECOVERY +{presentationEvent.Payload}");
                break;
            case CombatPresentationEventType.RewardOfferReady:
                ShowRewardOffer(presentationEvent.Payload);
                break;
            case CombatPresentationEventType.RewardChosen:
                HideRewardOffer();
                _notifications.Enqueue($"ACQUIRED: {presentationEvent.Payload}");
                break;
            case CombatPresentationEventType.RouteChoiceReady:
                ShowRouteOffer(presentationEvent.Payload);
                break;
            case CombatPresentationEventType.RouteChosen:
                HideRewardOffer();
                _notifications.Enqueue($"ROUTE: {presentationEvent.Payload}");
                break;
            case CombatPresentationEventType.CorruptionDrain:
                var corruptionTarget = FindActor(presentationEvent.SourceActorId);
                if (corruptionTarget != null)
                {
                    SpawnFloatingText($"-{presentationEvent.Payload}", corruptionTarget, new Color(0.72f, 0.24f, 0.86f));
                }
                break;
            case CombatPresentationEventType.EliteEnemySpawned:
                ShowImmediateNotification($"ELITE: {presentationEvent.Payload}", 1.2f);
                break;
            case CombatPresentationEventType.StageCompleted:
                _notifications.Enqueue(
                    $"{_simulation?.EncounterDirector?.Mission.DisplayName.ToUpperInvariant() ?? "MISSION"} COMPLETE");
                break;
            case CombatPresentationEventType.StageResultsReady:
                ShowImmediateNotification($"RANK {presentationEvent.Payload}", 1.6f);
                break;
            case CombatPresentationEventType.ExtraLifeAwarded:
                ShowImmediateNotification("EXTRA LIFE!", 1.8f);
                break;
            case CombatPresentationEventType.BeamClashStarted:
                _beamClashContainer.Visible = true;
                _beamClashBar.Value = 0;
                _cinematicWashTimer = 3.0f;
                _notifications.Clear();
                ShowImmediateNotification("BEAM CLASH!", 1.5f);
                break;
            case CombatPresentationEventType.BeamClashUpdated:
                var clashParts = presentationEvent.Payload.Split('|');
                if (clashParts.Length >= 2 && int.TryParse(clashParts[0], out var score))
                {
                    _beamClashBar.Value = score;
                }
                break;
            case CombatPresentationEventType.BeamClashResolved:
                _beamClashContainer.Visible = false;
                _notifications.Clear();
                if (int.TryParse(presentationEvent.Payload, out var finalScore))
                {
                    ShowImmediateNotification(finalScore >= 0 ? "P1 WINS CLASH!" : "P2 WINS CLASH!", 1.5f);
                }
                break;
            case CombatPresentationEventType.SuperStarted:
                _notifications.Clear();
                ShowImmediateNotification($"SUPER: {presentationEvent.Payload}", 1.5f);
                _cinematicWashTimer = 0.62f;
                break;
            case CombatPresentationEventType.BossPhaseChanged:
                if (presentationEvent.Payload == "BossActivated")
                {
                    _notifications.Enqueue("BOSS ENCOUNTER OPEN");
                }
                else
                {
                    var separatorIndex = presentationEvent.Payload.IndexOf('|');
                    var phaseName = separatorIndex >= 0
                        ? presentationEvent.Payload[(separatorIndex + 1)..]
                        : presentationEvent.Payload;
                    _notifications.Enqueue($"BOSS PHASE SHIFT: {phaseName}");
                }
                break;
            case CombatPresentationEventType.PlayerLifeLost:
                ShowDeathScreen(presentationEvent.Payload, isGameOver: false);
                break;
            case CombatPresentationEventType.GameOver:
                ShowDeathScreen(presentationEvent.Payload, isGameOver: true);
                break;
            case CombatPresentationEventType.ActorDefeated:
                if (FindActor(presentationEvent.SourceActorId) is { IsBoss: true } boss)
                {
                    _notifications.Enqueue("BOSS DEFEATED");
                    var bossFormId = _simulation?.EncounterDirector?.Mission.BossFormId ?? boss.CurrentForm.Id;
                    foreach (var fragmentId in ProjectMannequin.Data.LoreCatalog.GetFragmentsForBoss(bossFormId))
                    {
                        if (ProjectMannequin.Progression.MvpProgressStore.UnlockLore(fragmentId))
                        {
                            var fragment = ProjectMannequin.Data.LoreCatalog.GetFragment(fragmentId);
                            _notifications.Enqueue("LORE: " + (fragment?.Title ?? fragmentId));
                        }
                    }

                    if (ProjectMannequin.Progression.MvpProgressStore.TryUnlockSecretEnding())
                    {
                        _notifications.Enqueue("\u2605 SECRET REVEALED: A hidden challenger stirs in the Archive...");
                    }
                }
                break;
        }
    }

    private void ShowRewardOffer(string payload)
    {
        _rewardTitleLabel.Text = "CHOOSE A REWARD";
        foreach (var child in _rewardOptionsBox.GetChildren())
        {
            child.QueueFree();
        }

        var options = payload.Split('|', System.StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < options.Length; i++)
        {
            var parts = options[i].Split("::");
            var name = parts.Length > 0 ? parts[0] : options[i];
            var description = parts.Length > 1 ? parts[1] : "";
            var rarity = parts.Length > 2 ? parts[2] : "";
            var iconKey = parts.Length > 3 ? parts[3] : "Basic";
            _rewardOptionsBox.AddChild(BuildRewardRow(i + 1, name, description, rarity, iconKey));
        }

        _rewardPanel.Visible = true;
    }

    private Control BuildRewardRow(int index, string name, string description, string rarity, string iconKey)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 14);

        var icon = new TextureRect
        {
            Texture = ProjectMannequin.Presentation.ProceduralRewardIconFactory.GetIcon(iconKey),
            CustomMinimumSize = new Vector2(56, 56),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        row.AddChild(icon);

        var textBox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var padChoice = index switch { 1 => "X", 2 => "Y", _ => "A" };
        var nameLabel = MakeLabel($"[{index} / {padChoice}]  {name}   ({rarity})", 20, new Color(1.0f, 0.92f, 0.66f));
        textBox.AddChild(nameLabel);
        var descriptionLabel = MakeLabel(description, 15, new Color(0.82f, 0.88f, 0.94f));
        descriptionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textBox.AddChild(descriptionLabel);
        row.AddChild(textBox);

        return row;
    }

    private void ShowRouteOffer(string payload)
    {
        _rewardTitleLabel.Text = "CHOOSE A ROUTE";
        foreach (var child in _rewardOptionsBox.GetChildren())
        {
            child.QueueFree();
        }

        var routes = payload.Split('|', System.StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < routes.Length; i++)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 14);
            var padChoice = i switch { 0 => "X", 1 => "Y", _ => "A" };
            var label = MakeLabel($"[{i + 1} / {padChoice}]  {routes[i]}", 20, new Color(0.9f, 0.94f, 1.0f));
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            row.AddChild(label);
            _rewardOptionsBox.AddChild(row);
        }

        _rewardPanel.Visible = true;
    }

    private void HideRewardOffer()
    {
        _rewardPanel.Visible = false;
    }

    private void ShowDeathScreen(string payload, bool isGameOver)
    {
        var parts = (payload ?? string.Empty).Split('|');
        var moveId = parts.Length > 0 ? parts[0] : string.Empty;
        var moveName = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : string.Empty;

        _deathTitleLabel.Text = isGameOver ? "GAME OVER" : "DEFEATED";
        _deathTitleLabel.Modulate = isGameOver
            ? new Color(1.0f, 0.28f, 0.24f)
            : new Color(1.0f, 0.55f, 0.24f);
        _deathKilledByLabel.Text = string.IsNullOrEmpty(moveName)
            ? "Struck down by an unknown blow"
            : $"Killed by:  {moveName}";
        _deathTipLabel.Text = "Tip:  " + ProjectMannequin.Data.DeathTipsCatalog.GetTipForMove(moveId);

        if (isGameOver)
        {
            _deathHintLabel.Text = "Your run ends here.";
            _deathPanelSecondsRemaining = 3600.0f;
        }
        else
        {
            var lives = ProjectMannequin.Progression.RunSessionManager.Instance?.RemainingLives ?? 0;
            _deathHintLabel.Text = $"Lives remaining:  {lives}    \u2014    regroup and adapt!";
            _deathPanelSecondsRemaining = 4.0f;
        }

        _deathPanel.Visible = true;
    }

    private void UpdateDeathScreen(float delta)
    {
        if (!_deathPanel.Visible)
        {
            return;
        }

        _deathPanelSecondsRemaining -= delta;
        if (_deathPanelSecondsRemaining <= 0.0f)
        {
            _deathPanel.Visible = false;
        }
    }

    private void SetThreatIndicator(string payload, float duration)
    {
        var parts = payload.Split('|');
        if (parts.Length == 0
            || !System.Enum.TryParse<EnemyEntryEdge>(parts[0], out var edge)
            || !_threatIndicatorTimers.ContainsKey(edge))
        {
            return;
        }

        if (parts.Length > 1
            && System.Enum.TryParse<EnemyEntryProfile>(parts[1], out var profile)
            && profile is EnemyEntryProfile.DropIn or EnemyEntryProfile.Ambush)
        {
            edge = EnemyEntryEdge.FarLane;
        }

        _threatIndicatorTimers[edge] = Mathf.Max(
            _threatIndicatorTimers[edge],
            duration);
    }

    private static string ResolveFormDisplayName(string formId)
    {
        return formId switch
        {
            "archive_knight_form" => "Archive Knight",
            "world_warrior_ryu_form" => "Ryu",
            _ => formId.Replace('_', ' '),
        };
    }

    private void ShowImmediateNotification(string text, float duration)
    {
        _notificationLabel.Text = text;
        _notificationLabel.Modulate = Colors.White;
        _notificationTimer = duration;
    }

    /// <summary>
    /// Off-anchor slide amount for the HUD panels during the boss-intro reveal.
    /// 1 = fully off-screen (Phase 1 hidden), 0 = seated at the anchor, and a
    /// small negative value during the ease-out-back overshoot (spring bounce).
    /// </summary>
    private float BossIntroSlide => 1.0f - _bossIntroRevealT;

    /// <summary>
    /// Phase 1 hook (boss intro): instantly hide every gameplay HUD element
    /// (alpha 0 and panels pushed off their anchors) for the cinematic build-up.
    /// </summary>
    public void HideHudForIntro()
    {
        _bossIntroRevealActive = false;
        _bossIntroRevealElapsed = 0.0f;
        _bossIntroRevealT = 0.0f;
    }

    /// <summary>
    /// Phase 2 hook (boss intro): rapidly spring the HUD back into its anchor
    /// positions with an ease-out-back (bounce) curve for impact.
    /// </summary>
    public void RevealHudForIntro(float duration = 0.42f)
    {
        _bossIntroRevealActive = true;
        _bossIntroRevealElapsed = 0.0f;
        _bossIntroRevealDuration = Mathf.Max(0.05f, duration);
        _bossIntroRevealT = 0.0f;
    }

    private void UpdateBossIntroReveal(float delta)
    {
        if (_bossIntroRevealActive)
        {
            _bossIntroRevealElapsed += delta;
            var progress = Mathf.Clamp(_bossIntroRevealElapsed / _bossIntroRevealDuration, 0.0f, 1.0f);
            _bossIntroRevealT = EaseOutBack(progress);
            if (progress >= 1.0f)
            {
                _bossIntroRevealActive = false;
                _bossIntroRevealT = 1.0f;
            }
        }

        _root.Modulate = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp(_bossIntroRevealT, 0.0f, 1.0f));
    }

    private static float EaseOutBack(float progress)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1.0f;
        var q = progress - 1.0f;
        return 1.0f + c3 * q * q * q + c1 * q * q;
    }

    private void HandleHitEvent(CombatPresentationEvent presentationEvent)
    {
        var damage = ParseDamage(presentationEvent.Payload);
        var target = FindActor(presentationEvent.TargetActorId);
        if (target is null)
        {
            return;
        }

        SpawnFloatingText(
            damage > 0 ? damage.ToString() : "HIT",
            target,
            presentationEvent.Type switch
            {
                CombatPresentationEventType.LauncherHit => new Color(0.66f, 0.86f, 1.0f),
                CombatPresentationEventType.CounterHit => new Color(1.0f, 0.56f, 0.24f),
                CombatPresentationEventType.PunishCounter => new Color(1.0f, 0.30f, 0.22f),
                _ => new Color(1.0f, 0.84f, 0.34f),
            });
    }

    private static int ParseDamage(string payload)
    {
        var separatorIndex = payload.LastIndexOf('|');
        if (separatorIndex < 0 || separatorIndex >= payload.Length - 1)
        {
            return 0;
        }

        return int.TryParse(payload[(separatorIndex + 1)..], out var damage) ? damage : 0;
    }

    private static string FormatTime(int frames)
    {
        var totalSeconds = Mathf.Max(0, frames) / GameConstants.TickRate;
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private CombatActor? FindActor(string actorId)
    {
        return _simulation?.Actors.FirstOrDefault(actor => actor.ActorId == actorId);
    }

    private void SpawnFloatingText(string text, CombatActor target, Color color)
    {
        var label = AcquireFloatingTextLabel();
        label.Text = text;
        label.Modulate = color;
        label.Visible = true;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = ProjectActorToScreen(target) + new Vector2(-28.0f, -18.0f);

        _floatingTexts.Add(new FloatingText(label, 0.85f, new Vector2(0.0f, -62.0f)));
    }

    private void BuildFloatingTextPool()
    {
        for (var index = 0; index < FloatingTextPoolSize; index++)
        {
            var label = MakeLabel("", 26, Colors.White);
            label.Visible = false;
            label.MouseFilter = Control.MouseFilterEnum.Ignore;
            label.ZIndex = 40;
            _root.AddChild(label);
            _floatingTextPool.Enqueue(label);
        }
    }

    private Label AcquireFloatingTextLabel()
    {
        if (_floatingTextPool.Count > 0)
        {
            return _floatingTextPool.Dequeue();
        }

        // At extreme hit density recycle the oldest label rather than allowing
        // unbounded node growth or allocating during combat.
        var recycled = _floatingTexts[0].Label;
        _floatingTexts.RemoveAt(0);
        return recycled;
    }

    private Vector2 ProjectActorToScreen(CombatActor actor)
    {
        var camera = GetViewport().GetCamera3D();
        if (camera is null)
        {
            return GetViewport().GetVisibleRect().Size * 0.5f;
        }

        return camera.UnprojectPosition(actor.GlobalPosition + new Vector3(0.0f, 2.1f, 0.0f));
    }

    private void UpdateFloatingTexts(float delta)
    {
        for (var index = _floatingTexts.Count - 1; index >= 0; index--)
        {
            var floatingText = _floatingTexts[index];
            floatingText.TimeRemaining -= delta;
            floatingText.Label.Position += floatingText.Velocity * delta;
            floatingText.Label.Modulate = new Color(
                floatingText.Label.Modulate.R,
                floatingText.Label.Modulate.G,
                floatingText.Label.Modulate.B,
                Mathf.Clamp(floatingText.TimeRemaining / 0.85f, 0.0f, 1.0f));

            if (floatingText.TimeRemaining > 0.0f)
            {
                _floatingTexts[index] = floatingText;
                continue;
            }

            floatingText.Label.Visible = false;
            floatingText.Label.Text = "";
            _floatingTextPool.Enqueue(floatingText.Label);
            _floatingTexts.RemoveAt(index);
        }
    }

    private void PositionBossPanel()
    {
        var size = GetViewport().GetVisibleRect().Size;
        var panelWidth = Mathf.Clamp(size.X * 0.33f, 320.0f, 720.0f);
        _bossPanel.CustomMinimumSize = new Vector2(panelWidth, 108.0f);
        _bossPanel.Position = new Vector2(size.X - size.X * 0.124f - panelWidth, 18.0f)
            + new Vector2(BossIntroSlide * size.X * 0.5f, 0.0f);
    }

    private void PositionPlayerPanel()
    {
        var size = GetViewport().GetVisibleRect().Size;
        var panelWidth = Mathf.Clamp(size.X * 0.33f, 320.0f, 720.0f);
        _playerPanel.CustomMinimumSize = new Vector2(panelWidth, 104.0f);
        _playerPanel.Position = new Vector2(size.X * 0.124f, 18.0f)
            + new Vector2(-BossIntroSlide * size.X * 0.5f, 0.0f);
    }

    private void PositionPortraits()
    {
        if (_playerPortrait is null || _bossPortrait is null)
        {
            return;
        }

        var size = GetViewport().GetVisibleRect().Size;
        var portraitSize = Mathf.Min(size.X * 0.09f, size.Y * 0.16f);
        var marginX = size.X * 0.019f;
        var marginY = size.Y * 0.024f;

        var portraitSlide = new Vector2(BossIntroSlide * size.X * 0.5f, 0.0f);
        _playerPortrait.Position = new Vector2(marginX, marginY) - portraitSlide;
        _playerPortrait.Size = Vector2.One * portraitSize;
        _bossPortrait.Position = new Vector2(size.X - marginX - portraitSize, marginY) + portraitSlide;
        _bossPortrait.Size = Vector2.One * portraitSize;
    }

    private void PositionMissionPanel()
    {
        var size = GetViewport().GetVisibleRect().Size;
        _missionPanel.Position = new Vector2(
            (size.X - _missionPanel.CustomMinimumSize.X) * 0.5f,
            (size.Y - _missionPanel.CustomMinimumSize.Y) * 0.5f);
        _resultsConfirmationPanel.Position = new Vector2(
            (size.X - _resultsConfirmationPanel.CustomMinimumSize.X) * 0.5f,
            (size.Y - _resultsConfirmationPanel.CustomMinimumSize.Y) * 0.5f);
    }

    private static PanelContainer MakePanel(Vector2 position, Vector2 minimumSize)
    {
        var panel = new PanelContainer
        {
            Position = position,
            CustomMinimumSize = minimumSize,
        };
        panel.AddThemeConstantOverride("margin_left", 12);
        panel.AddThemeConstantOverride("margin_top", 10);
        panel.AddThemeConstantOverride("margin_right", 12);
        panel.AddThemeConstantOverride("margin_bottom", 10);
        return panel;
    }

    private static Label MakeLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            Modulate = color,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static ProgressBar MakeBar(Color color, float height = 16.0f)
    {
        var bar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(280.0f, height),
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            ShowPercentage = false,
        };

        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat
        {
            BgColor = new Color(0.025f, 0.035f, 0.045f, 0.84f),
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        });
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 2,
            CornerRadiusBottomLeft = 2,
        });
        return bar;
    }

    private static TextureRect MakePortrait(Texture2D source, Rect2 region, Color modulate)
    {
        return new TextureRect
        {
            Texture = new AtlasTexture
            {
                Atlas = source,
                Region = region,
            },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = modulate,
        };
    }

    private struct FloatingText
    {
        public FloatingText(Label label, float timeRemaining, Vector2 velocity)
        {
            Label = label;
            TimeRemaining = timeRemaining;
            Velocity = velocity;
        }

        public Label Label { get; }
        public float TimeRemaining { get; set; }
        public Vector2 Velocity { get; }
    }
}
