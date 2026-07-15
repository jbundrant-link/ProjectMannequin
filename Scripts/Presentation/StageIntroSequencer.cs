using Godot;
using ProjectMannequin.Core;

namespace ProjectMannequin.Presentation;

/// <summary>
/// Arcade stage-card and short named-elite warning presentation. Simulation
/// timing remains in ArcadeEncounterDirector; this layer only animates visuals.
/// </summary>
public partial class StageIntroSequencer : CanvasLayer
{
    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";

    private GameSimulation? _simulation;
    private Control _root = null!;
    private VBoxContainer _card = null!;
    private ColorRect _wash = null!;
    private Label _eyebrow = null!;
    private Label _stageNumber = null!;
    private Label _title = null!;
    private Label _subtitle = null!;
    private PanelContainer _elitePanel = null!;
    private Label _eliteLabel = null!;
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private float _introElapsed;
    private float _eliteElapsed;
    private bool _introVisible;

    public override void _Ready()
    {
        Layer = 18;
        ProcessMode = ProcessModeEnum.Always;
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        BuildInterface();
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_HIDE_STAGE_INTRO_FOR_CAPTURE") == "1")
        {
            _root.Visible = false;
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        CaptureEvents();
        UpdateIntro((float)delta);
        UpdateElite((float)delta);
    }

    private void BuildInterface()
    {
        _root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _wash = new ColorRect
        {
            Color = new Color(0.018f, 0.025f, 0.05f, 0.0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        _wash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_wash);

        _card = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _card.SetAnchorsPreset(Control.LayoutPreset.Center);
        _card.OffsetLeft = -520;
        _card.OffsetTop = -180;
        _card.OffsetRight = 520;
        _card.OffsetBottom = 180;
        _card.AddThemeConstantOverride("separation", 5);
        _root.AddChild(_card);

        _eyebrow = MakeLabel("PROJECT MANNEQUIN", 22, new Color(0.42f, 0.94f, 0.92f));
        _stageNumber = MakeLabel("STAGE 1", 48, new Color(1.0f, 0.78f, 0.28f));
        _title = MakeLabel("ARCHIVE DISTRICT", 72, Colors.White);
        _subtitle = MakeLabel("", 22, new Color(0.72f, 0.80f, 0.90f));
        _card.AddChild(_eyebrow);
        _card.AddChild(_stageNumber);
        _card.AddChild(_title);
        _card.AddChild(_subtitle);

        _elitePanel = new PanelContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _elitePanel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _elitePanel.OffsetLeft = -380;
        _elitePanel.OffsetTop = 180;
        _elitePanel.OffsetRight = 380;
        _elitePanel.OffsetBottom = 252;
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.035f, 0.02f, 0.90f),
            BorderColor = new Color(1.0f, 0.72f, 0.18f, 0.95f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        };
        _elitePanel.AddThemeStyleboxOverride("panel", style);
        _root.AddChild(_elitePanel);

        _eliteLabel = MakeLabel("NAMED ELITE", 28, new Color(1.0f, 0.82f, 0.34f));
        _elitePanel.AddChild(_eliteLabel);
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
            switch (presentationEvent.Type)
            {
                case CombatPresentationEventType.StageIntroStarted:
                    var parts = presentationEvent.Payload.Split('|');
                    _stageNumber.Text = parts.Length > 0 ? $"STAGE {parts[0]}" : "STAGE";
                    _title.Text = parts.Length > 1 ? parts[1].ToUpperInvariant() : "UNKNOWN SECTOR";
                    _subtitle.Text = parts.Length > 2 ? parts[2] : "";
                    _eyebrow.Text = "PROJECT MANNEQUIN  //  THE LIVING ARCHIVE";
                    _introElapsed = 0.0f;
                    _introVisible = true;
                    _wash.Visible = true;
                    _card.Visible = true;
                    _stageNumber.Visible = true;
                    _title.Visible = true;
                    _subtitle.Visible = true;
                    break;
                case CombatPresentationEventType.StageIntroReady:
                    _stageNumber.Text = "READY?";
                    _stageNumber.Modulate = new Color(1.0f, 0.78f, 0.28f);
                    _title.Visible = false;
                    _subtitle.Visible = false;
                    break;
                case CombatPresentationEventType.StageIntroFinished:
                    _introVisible = false;
                    break;
                case CombatPresentationEventType.EliteEnemySpawned:
                    ShowEliteBanner(
                        $"WARNING  //  {presentationEvent.Payload.ToUpperInvariant()}",
                        new Color(1.0f, 0.82f, 0.34f));
                    break;
                case CombatPresentationEventType.EliteIntroStarted:
                    ShowEliteBanner(
                        $"NAMED ELITE  //  {presentationEvent.Payload.ToUpperInvariant()}",
                        new Color(1.0f, 0.72f, 0.18f));
                    break;
                case CombatPresentationEventType.EliteIntroReady:
                    ShowEliteBanner(
                        $"{presentationEvent.Payload.ToUpperInvariant()}  //  READY?",
                        new Color(0.42f, 0.94f, 0.92f));
                    break;
                case CombatPresentationEventType.EliteIntroFight:
                    ShowEliteBanner("FIGHT!", new Color(1.0f, 0.34f, 0.26f));
                    break;
            }
        }
    }

    private void UpdateIntro(float delta)
    {
        if (!_wash.Visible)
        {
            return;
        }

        _introElapsed += delta;
        var alpha = _introVisible
            ? Mathf.Clamp(_introElapsed / 0.20f, 0.0f, 1.0f)
            : Mathf.MoveToward(_wash.Color.A, 0.0f, delta * 4.6f);
        _wash.Color = new Color(0.018f, 0.025f, 0.05f, alpha * 0.88f);
        _card.Modulate = new Color(1.0f, 1.0f, 1.0f, alpha);
        if (!_introVisible && alpha <= 0.001f)
        {
            _wash.Visible = false;
            _card.Visible = false;
        }
    }

    private void UpdateElite(float delta)
    {
        if (!_elitePanel.Visible)
        {
            return;
        }

        _eliteElapsed += delta;
        var alpha = _eliteElapsed < 0.18f
            ? _eliteElapsed / 0.18f
            : Mathf.Clamp(1.0f - (_eliteElapsed - 1.0f) / 0.35f, 0.0f, 1.0f);
        _elitePanel.Modulate = new Color(1.0f, 1.0f, 1.0f, alpha);
        if (_eliteElapsed >= 1.35f)
        {
            _elitePanel.Visible = false;
        }
    }

    private void ShowEliteBanner(string text, Color color)
    {
        _eliteLabel.Text = text;
        _eliteLabel.Modulate = color;
        _eliteElapsed = 0.0f;
        _elitePanel.Visible = true;
        _elitePanel.Modulate = Colors.White;
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
        label.AddThemeColorOverride("font_outline_color", new Color(0.0f, 0.0f, 0.0f, 0.96f));
        label.AddThemeConstantOverride("outline_size", 8);
        return label;
    }
}
