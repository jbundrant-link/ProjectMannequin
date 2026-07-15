using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.UI;

namespace ProjectMannequin.Presentation;

/// <summary>
/// Drives the cinematic, fighting-game-style boss fight introduction (the
/// "READY? / FIGHT!" hype sequence). It is a presentation-only controller: the
/// authoritative input lock lives in the deterministic simulation, which freezes
/// the player for the whole <c>BossIntro</c> state and unlocks exactly when the
/// FIGHT release fires. This node choreographs the five presentation phases on
/// top of that gate.
///
/// <para>Phase timeline (states mirror the design spec):</para>
/// <list type="number">
///   <item><see cref="IntroPhase.IntroCinematic"/> — HUD hidden, camera pushed in
///     on the pair (camera handled by <see cref="PrototypeStageView"/>).</item>
///   <item><see cref="IntroPhase.UiReveal"/> — HUD springs back into place and the
///     camera returns to the gameplay bounds.</item>
///   <item><see cref="IntroPhase.PhaseReady"/> — the READY call-to-action holds.</item>
///   <item><see cref="IntroPhase.PhaseFight"/> — swaps to FIGHT (20% larger), shake
///     + particle burst.</item>
///   <item><see cref="IntroPhase.CombatActive"/> — input is live; the FIGHT text
///     scales up and fades out so it never obstructs the action.</item>
/// </list>
///
/// The three simulation gates (<see cref="CombatPresentationEventType.BossIntroStarted"/>,
/// <see cref="CombatPresentationEventType.BossIntroReady"/>,
/// <see cref="CombatPresentationEventType.BossIntroFight"/>) advance the phases.
/// </summary>
public partial class BossIntroSequencer : CanvasLayer
{
    public enum IntroPhase
    {
        Inactive,
        Cutscene,
        IntroCinematic,
        UiReveal,
        PhaseReady,
        PhaseFight,
        CombatActive,
    }

    private const int ReadyFontSize = 132;
    private const float FightScale = 1.2f; // FIGHT reads 20% larger than READY.
    private const float ReadyPopSeconds = 0.28f;
    private const float FightPopSeconds = 0.09f;
    private const float FightHoldSeconds = 0.12f;
    private const float FightFadeSeconds = 0.3f;
    private const float HudRevealSeconds = 0.42f;

    private static readonly Color ReadyColor = new(1.0f, 0.86f, 0.32f);
    private static readonly Color FightColor = new(1.0f, 0.34f, 0.26f);

    private readonly HashSet<string> _seenEventKeys = new();

    private GameSimulation? _simulation;
    private MvpHud? _hud;
    private Control _messageRoot = null!;
    private Label _messageLabel = null!;
    private SpeedLineOverlay _speedLines = null!;
    private CpuParticles2D _burst = null!;
    private Control _cutsceneRoot = null!;
    private ColorRect _cutsceneWash = null!;
    private TextureRect _cutscenePlayerPortrait = null!;
    private TextureRect _cutsceneBossPortrait = null!;
    private Label _cutsceneVsLabel = null!;
    private Label _cutsceneBossName = null!;
    private Label _cutsceneSubtitle = null!;
    private Label _cutsceneTaunt = null!;
    private readonly List<Control> _splitCutsceneCards = new();

    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private float _uiRevealTimer;
    private bool _renderTestSuppressed;

    /// <summary>Fired when the cinematic build-up begins (Phase 1). Presentation
    /// systems such as a music manager or boss-specific intro animation driver can
    /// subscribe to react to the intro starting.</summary>
    public event Action? IntroStarted;

    /// <summary>Fired at the FIGHT release (Phase 4), the moment gameplay resumes.
    /// Boss AI reactions, music stingers, and other systems can hook this.</summary>
    public event Action? FightStarted;

    /// <summary>The current phase of the intro state machine.</summary>
    public IntroPhase Phase { get; private set; } = IntroPhase.Inactive;
    public bool IsMessageVisible => _messageLabel?.Visible == true;
    public string MessageText => _messageLabel?.Text ?? "";
    public bool IsCutsceneVisible => _cutsceneRoot?.Visible == true;

    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";
    [Export] public NodePath HudPath { get; set; } = "../MvpHud";

    public override void _Ready()
    {
        // Render above the HUD (default CanvasLayer layer is 1).
        Layer = 20;
        _renderTestSuppressed =
            OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_RENDER_TEST") == "1";
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        _hud = GetNodeOrNull<MvpHud>(HudPath);
        BuildInterface();
    }

    public override void _Process(double delta)
    {
        if (_simulation is null)
        {
            return;
        }

        CaptureEvents();

        // Presentation-only sub-phase: the UI reveal blends into the READY hold.
        if (Phase == IntroPhase.UiReveal)
        {
            _uiRevealTimer -= (float)delta;
            if (_uiRevealTimer <= 0.0f)
            {
                Phase = IntroPhase.PhaseReady;
            }
        }
    }

    private void BuildInterface()
    {
        _messageRoot = new Control
        {
            Name = "BossIntroMessageRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _messageRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_messageRoot);

        BuildCutsceneOverlay();

        _speedLines = new SpeedLineOverlay
        {
            Name = "SpeedLines",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        _speedLines.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _messageRoot.AddChild(_speedLines);

        _burst = new CpuParticles2D
        {
            Name = "FightBurst",
            Emitting = false,
            OneShot = true,
            Explosiveness = 1.0f,
            Amount = 56,
            Lifetime = 0.62,
            Direction = Vector2.Right,
            Spread = 180.0f,
            InitialVelocityMin = 340.0f,
            InitialVelocityMax = 860.0f,
            Gravity = Vector2.Zero,
            ScaleAmountMin = 3.0f,
            ScaleAmountMax = 8.0f,
            DampingMin = 180.0f,
            DampingMax = 260.0f,
            Color = new Color(1.0f, 0.74f, 0.28f),
        };
        _messageRoot.AddChild(_burst);

        _messageLabel = new Label
        {
            Name = "IntroMessage",
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        _messageLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _messageLabel.OffsetLeft = -640;
        _messageLabel.OffsetTop = -200;
        _messageLabel.OffsetRight = 640;
        _messageLabel.OffsetBottom = 200;
        // Scale around the box centre so pops/blowups stay anchored to screen centre.
        _messageLabel.PivotOffset = new Vector2(640, 200);
        _messageLabel.AddThemeFontSizeOverride("font_size", ReadyFontSize);
        _messageLabel.AddThemeColorOverride("font_color", Colors.White);
        _messageLabel.AddThemeColorOverride("font_outline_color", new Color(0.03f, 0.03f, 0.05f));
        _messageLabel.AddThemeConstantOverride("outline_size", 20);
        _messageLabel.AddThemeColorOverride("font_shadow_color", new Color(0.0f, 0.0f, 0.0f, 0.55f));
        _messageLabel.AddThemeConstantOverride("shadow_offset_x", 7);
        _messageLabel.AddThemeConstantOverride("shadow_offset_y", 7);
        _messageRoot.AddChild(_messageLabel);
    }

    private void CaptureEvents()
    {
        _presentationEventBuffer.Clear();
        _presentationEventCursor = _simulation!.CopyPresentationEventsSince(
            _presentationEventCursor,
            _presentationEventBuffer);
        foreach (var presentationEvent in _presentationEventBuffer)
        {
            var key = $"{presentationEvent.Tick}:{presentationEvent.Type}:{presentationEvent.SourceActorId}:{presentationEvent.Payload}";
            if (!_seenEventKeys.Add(key))
            {
                continue;
            }

            if (_seenEventKeys.Count > 128)
            {
                _seenEventKeys.Clear();
            }

            HandleEvent(presentationEvent);
        }
    }

    private void HandleEvent(CombatPresentationEvent presentationEvent)
    {
        switch (presentationEvent.Type)
        {
            case CombatPresentationEventType.BossIntroCutscene:
                EnterCutscene(presentationEvent);
                break;
            case CombatPresentationEventType.BossIntroStarted:
                EnterIntroCinematic();
                break;
            case CombatPresentationEventType.BossIntroReady:
                EnterUiRevealAndReady(presentationEvent.Payload);
                break;
            case CombatPresentationEventType.BossIntroFight:
                EnterPhaseFight();
                break;
        }
    }

    // ----- Phase 1: cinematic build-up ------------------------------------

    private void EnterIntroCinematic()
    {
        Phase = IntroPhase.IntroCinematic;
        DismissCutscene();

        // Notify listeners (music manager, boss intro-animation driver, ...).
        IntroStarted?.Invoke();

        if (_renderTestSuppressed)
        {
            return;
        }

        // Hide all standard gameplay HUD elements for the cinematic. The camera
        // push-in on the pair is handled by PrototypeStageView on the same event.
        _hud?.HideHudForIntro();
        ResetMessage();
    }

    // ----- Phases 2 & 3: UI reveal + READY call-to-action -----------------

    private void EnterUiRevealAndReady(string bossDisplayName)
    {
        if (_renderTestSuppressed)
        {
            Phase = IntroPhase.PhaseReady;
            return;
        }

        Phase = IntroPhase.UiReveal;
        _uiRevealTimer = HudRevealSeconds;

        // Phase 2: spring the HUD back into its anchor positions.
        _hud?.RevealHudForIntro(HudRevealSeconds);

        // Phase 3: the READY call-to-action pops in over the revealed HUD.
        _messageLabel.Text = ResolveReadyText(bossDisplayName);
        _messageLabel.Modulate = ReadyColor;
        _messageLabel.Scale = Vector2.One * 0.35f;
        _messageLabel.Visible = true;

        var pop = CreateTween();
        pop.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        pop.TweenProperty(_messageLabel, "scale", Vector2.One, ReadyPopSeconds);

        _speedLines.Visible = true;
        _speedLines.QueueRedraw();
        _speedLines.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        var lines = CreateTween();
        lines.TweenProperty(_speedLines, "modulate:a", 0.85f, 0.22f);
    }

    // ----- Phase 4 & 5: FIGHT release, then fade for combat ---------------

    private void EnterPhaseFight()
    {
        Phase = IntroPhase.PhaseFight;

        // Notify listeners the moment gameplay resumes (input is already unlocked
        // by the simulation entering EncounterActive on this same event).
        FightStarted?.Invoke();

        if (_renderTestSuppressed)
        {
            Phase = IntroPhase.CombatActive;
            return;
        }

        _messageLabel.Text = "FIGHT!";
        _messageLabel.Modulate = FightColor;
        _messageLabel.Scale = Vector2.One * (FightScale * 0.85f);
        _messageLabel.Visible = true;

        FireBurst();

        var tween = CreateTween();
        // Phase 4: sharp pop to the FIGHT size (20% larger than READY).
        tween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_messageLabel, "scale", Vector2.One * FightScale, FightPopSeconds);
        tween.TweenInterval(FightHoldSeconds);

        // Phase 5: gameplay is live — blow the FIGHT text up and fade it out so it
        // stops obstructing the action.
        tween.TweenCallback(Callable.From(() => Phase = IntroPhase.CombatActive));
        tween.TweenProperty(_messageLabel, "scale", Vector2.One * FightScale * 1.5f, FightFadeSeconds)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        tween.Parallel()
            .TweenProperty(_messageLabel, "modulate:a", 0.0f, FightFadeSeconds)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        tween.Parallel()
            .TweenProperty(_speedLines, "modulate:a", 0.0f, FightFadeSeconds);
        tween.TweenCallback(Callable.From(FinishIntro));
    }

    private void FinishIntro()
    {
        ResetMessage();
    }

    private void FireBurst()
    {
        _burst.Position = GetViewport().GetVisibleRect().Size * 0.5f;
        _burst.Restart();
        _burst.Emitting = true;
    }

    private void ResetMessage()
    {
        _messageLabel.Visible = false;
        _messageLabel.Scale = Vector2.One;
        _messageLabel.Modulate = Colors.White;
        _speedLines.Visible = false;
        _speedLines.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    }

    // ----- Phase 0: matchup cutscene (SF4/DBFZ rival "VS" intro) -----------

    private void BuildCutsceneOverlay()
    {
        _cutsceneRoot = new Control
        {
            Name = "MatchupCutscene",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        _cutsceneRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _messageRoot.AddChild(_cutsceneRoot);

        _cutsceneWash = new ColorRect
        {
            Name = "Wash",
            Color = new Color(0.02f, 0.02f, 0.05f, 0.0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _cutsceneWash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _cutsceneRoot.AddChild(_cutsceneWash);

        _cutscenePlayerPortrait = MakeCutscenePortrait("PlayerPortrait", flip: false);
        _cutsceneRoot.AddChild(_cutscenePlayerPortrait);

        _cutsceneBossPortrait = MakeCutscenePortrait("BossPortrait", flip: true);
        _cutsceneRoot.AddChild(_cutsceneBossPortrait);

        _cutsceneBossName = MakeCutsceneLabel(60, Colors.White);
        _cutsceneBossName.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _cutsceneBossName.OffsetLeft = -680;
        _cutsceneBossName.OffsetTop = 64;
        _cutsceneBossName.OffsetRight = 680;
        _cutsceneBossName.OffsetBottom = 156;
        _cutsceneRoot.AddChild(_cutsceneBossName);

        _cutsceneSubtitle = MakeCutsceneLabel(26, new Color(0.86f, 0.9f, 0.96f));
        _cutsceneSubtitle.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _cutsceneSubtitle.OffsetLeft = -680;
        _cutsceneSubtitle.OffsetTop = 152;
        _cutsceneSubtitle.OffsetRight = 680;
        _cutsceneSubtitle.OffsetBottom = 196;
        _cutsceneRoot.AddChild(_cutsceneSubtitle);

        _cutsceneVsLabel = MakeCutsceneLabel(150, Colors.White);
        _cutsceneVsLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _cutsceneVsLabel.OffsetLeft = -220;
        _cutsceneVsLabel.OffsetTop = -140;
        _cutsceneVsLabel.OffsetRight = 220;
        _cutsceneVsLabel.OffsetBottom = 140;
        _cutsceneVsLabel.PivotOffset = new Vector2(220, 140);
        _cutsceneVsLabel.Text = "VS";
        _cutsceneRoot.AddChild(_cutsceneVsLabel);

        _cutsceneTaunt = MakeCutsceneLabel(32, new Color(0.96f, 0.94f, 0.86f));
        _cutsceneTaunt.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        _cutsceneTaunt.OffsetLeft = -760;
        _cutsceneTaunt.OffsetTop = -156;
        _cutsceneTaunt.OffsetRight = 760;
        _cutsceneTaunt.OffsetBottom = -66;
        _cutsceneTaunt.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cutsceneRoot.AddChild(_cutsceneTaunt);
    }

    private void EnterCutscene(CombatPresentationEvent presentationEvent)
    {
        if (_renderTestSuppressed)
        {
            return;
        }

        Phase = IntroPhase.Cutscene;
        ClearSplitCutsceneCards();

        if (IsSplitScreenBossIntro())
        {
            EnterSplitCutscene();
            return;
        }

        var parts = presentationEvent.Payload.Split('|');
        var playerFormId = parts.Length > 0 ? parts[0] : "";
        var bossFormId = parts.Length > 1 ? parts[1] : "";
        var data = MatchupCutsceneCatalog.TryGet(playerFormId, bossFormId);
        var (playerForm, bossForm) = ResolveCutsceneForms(presentationEvent.SourceActorId);
        var accent = data?.AccentColor ?? FightColor;

        // The cutscene owns the screen; hide the gameplay HUD like the cinematic.
        _hud?.HideHudForIntro();

        _cutscenePlayerPortrait.Texture = FormSelectPortraitResolver.Resolve(playerForm);
        _cutsceneBossPortrait.Texture = FormSelectPortraitResolver.Resolve(bossForm);
        SetSingleCutsceneControlsVisible(true);
        _cutsceneBossName.Text = (bossForm?.DisplayName ?? "CHALLENGER").ToUpperInvariant();
        _cutsceneBossName.Modulate = accent;
        _cutsceneVsLabel.Modulate = accent;
        _cutsceneSubtitle.Text = data?.Subtitle ?? string.Empty;
        _cutsceneTaunt.Text = data?.Taunt ?? string.Empty;

        // Absolute portrait seats + off-screen start (so the slide-in does not
        // fight anchor layout), computed from the live viewport size.
        var viewport = GetViewport().GetVisibleRect().Size;
        var portraitSize = new Vector2(viewport.X * 0.44f, viewport.Y * 0.88f);
        var seatY = viewport.Y * 0.06f;
        var playerSeatX = viewport.X * 0.02f;
        var bossSeatX = viewport.X * 0.54f;
        _cutscenePlayerPortrait.Size = portraitSize;
        _cutsceneBossPortrait.Size = portraitSize;
        _cutscenePlayerPortrait.Position = new Vector2(-portraitSize.X, seatY);
        _cutsceneBossPortrait.Position = new Vector2(viewport.X, seatY);

        _cutsceneRoot.Visible = true;
        _cutsceneRoot.Modulate = Colors.White;
        _cutsceneWash.Color = new Color(0.02f, 0.02f, 0.05f, 0.0f);
        _cutsceneVsLabel.Scale = Vector2.One * 0.2f;
        SetControlAlpha(_cutsceneBossName, 0.0f);
        SetControlAlpha(_cutsceneSubtitle, 0.0f);
        SetControlAlpha(_cutsceneTaunt, 0.0f);
        SetControlAlpha(_cutsceneVsLabel, 0.0f);

        var tween = CreateTween();
        // Step 1: dark wash + fighters charge in from opposite sides.
        tween.TweenProperty(_cutsceneWash, "color:a", 0.86f, 0.35f);
        tween.Parallel()
            .TweenProperty(_cutscenePlayerPortrait, "position:x", playerSeatX, 0.5f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween.Parallel()
            .TweenProperty(_cutsceneBossPortrait, "position:x", bossSeatX, 0.5f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(_cutsceneBossName, "modulate:a", 1.0f, 0.4f);
        tween.Parallel().TweenProperty(_cutsceneSubtitle, "modulate:a", 1.0f, 0.4f);
        // Step 2: the "VS" clashes in, then the taunt line fades up.
        tween.TweenProperty(_cutsceneVsLabel, "scale", Vector2.One, 0.34f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(_cutsceneVsLabel, "modulate:a", 1.0f, 0.2f);
        tween.Parallel().TweenProperty(_cutsceneTaunt, "modulate:a", 1.0f, 0.3f);
    }

    private void EnterSplitCutscene()
    {
        if (_simulation is null)
        {
            return;
        }

        // Split-screen isolated duels have one boss locked to each player. Build
        // a compact rival card inside each split band instead of one global card
        // that would incorrectly describe only the first duel.
        var pairs = _simulation.Actors
            .Where(actor => actor.IsPlayerControlled && !actor.IsDead)
            .OrderBy(actor => actor.PlayerId)
            .Select(player => new
            {
                Player = player,
                Boss = _simulation.Actors.FirstOrDefault(actor =>
                    actor.IsBoss
                    && !actor.IsDead
                    && actor.LockedTargetPlayerId == player.PlayerId),
            })
            .Where(pair => pair.Boss is not null)
            .ToArray();
        if (pairs.Length == 0)
        {
            return;
        }

        _hud?.HideHudForIntro();
        SetSingleCutsceneControlsVisible(false);
        _cutsceneRoot.Visible = true;
        _cutsceneRoot.Modulate = Colors.White;
        _cutsceneWash.Color = new Color(0.02f, 0.02f, 0.05f, 0.0f);

        var viewport = GetViewport().GetVisibleRect().Size;
        var cardWidth = viewport.X / pairs.Length;
        for (var index = 0; index < pairs.Length; index++)
        {
            var pair = pairs[index];
            var boss = pair.Boss!;
            var data = MatchupCutsceneCatalog.TryGet(pair.Player.CurrentForm.Id, boss.CurrentForm.Id);
            var accent = data?.AccentColor ?? FightColor;
            var card = BuildSplitCutsceneCard(
                index,
                cardWidth,
                viewport.Y,
                pair.Player.CurrentForm,
                boss.CurrentForm,
                data,
                accent);
            _cutsceneRoot.AddChild(card.Root);
            _splitCutsceneCards.Add(card.Root);

            card.PlayerPortrait.Position = new Vector2(-card.PlayerPortrait.Size.X, card.PlayerPortrait.Position.Y);
            card.BossPortrait.Position = new Vector2(cardWidth, card.BossPortrait.Position.Y);
            card.VsLabel.Scale = Vector2.One * 0.2f;
            SetControlAlpha(card.BossName, 0.0f);
            SetControlAlpha(card.Subtitle, 0.0f);
            SetControlAlpha(card.Taunt, 0.0f);
            SetControlAlpha(card.VsLabel, 0.0f);

            var tween = CreateTween();
            tween.TweenProperty(card.PlayerPortrait, "position:x", cardWidth * 0.02f, 0.5f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            tween.Parallel()
                .TweenProperty(card.BossPortrait, "position:x", cardWidth * 0.58f, 0.5f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            tween.Parallel().TweenProperty(card.BossName, "modulate:a", 1.0f, 0.35f);
            tween.Parallel().TweenProperty(card.Subtitle, "modulate:a", 1.0f, 0.35f);
            tween.TweenProperty(card.VsLabel, "scale", Vector2.One, 0.34f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            tween.Parallel().TweenProperty(card.VsLabel, "modulate:a", 1.0f, 0.2f);
            tween.Parallel().TweenProperty(card.Taunt, "modulate:a", 1.0f, 0.3f);
        }

        var washTween = CreateTween();
        washTween.TweenProperty(_cutsceneWash, "color:a", 0.86f, 0.35f);
    }

    private void DismissCutscene()
    {
        if (_cutsceneRoot is null || !_cutsceneRoot.Visible)
        {
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(_cutsceneRoot, "modulate:a", 0.0f, 0.25f);
        tween.TweenCallback(Callable.From(() =>
        {
            _cutsceneRoot.Visible = false;
            _cutsceneRoot.Modulate = Colors.White;
            ClearSplitCutsceneCards();
            SetSingleCutsceneControlsVisible(true);
        }));
    }

    private bool IsSplitScreenBossIntro()
    {
        var director = _simulation?.EncounterDirector;
        var playerCount = _simulation?.Actors.Count(actor => actor.IsPlayerControlled && !actor.IsDead) ?? 0;
        return director is not null
            && playerCount >= 2
            && director.IsBossDuelActive
            && director.CurrentEncounter.BossType == BossEncounterType.IsolatedDuel;
    }

    private void ClearSplitCutsceneCards()
    {
        foreach (var card in _splitCutsceneCards)
        {
            card.QueueFree();
        }

        _splitCutsceneCards.Clear();
    }

    private void SetSingleCutsceneControlsVisible(bool visible)
    {
        _cutscenePlayerPortrait.Visible = visible;
        _cutsceneBossPortrait.Visible = visible;
        _cutsceneBossName.Visible = visible;
        _cutsceneSubtitle.Visible = visible;
        _cutsceneVsLabel.Visible = visible;
        _cutsceneTaunt.Visible = visible;
    }

    private SplitCutsceneCard BuildSplitCutsceneCard(
        int index,
        float width,
        float height,
        CharacterData playerForm,
        CharacterData bossForm,
        MatchupCutsceneData? data,
        Color accent)
    {
        var root = new Control
        {
            Name = $"SplitCutscene{index + 1}",
            Position = new Vector2(width * index, 0.0f),
            Size = new Vector2(width, height),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var playerPortrait = MakeCutscenePortrait("PlayerPortrait", flip: false);
        playerPortrait.Texture = FormSelectPortraitResolver.Resolve(playerForm);
        playerPortrait.Size = new Vector2(width * 0.4f, height * 0.78f);
        playerPortrait.Position = new Vector2(width * 0.02f, height * 0.13f);
        root.AddChild(playerPortrait);

        var bossPortrait = MakeCutscenePortrait("BossPortrait", flip: true);
        bossPortrait.Texture = FormSelectPortraitResolver.Resolve(bossForm);
        bossPortrait.Size = new Vector2(width * 0.4f, height * 0.78f);
        bossPortrait.Position = new Vector2(width * 0.58f, height * 0.13f);
        root.AddChild(bossPortrait);

        var bossName = MakeCutsceneLabel(34, accent);
        bossName.Text = bossForm.DisplayName.ToUpperInvariant();
        bossName.Position = new Vector2(width * 0.04f, height * 0.06f);
        bossName.Size = new Vector2(width * 0.92f, 54.0f);
        root.AddChild(bossName);

        var subtitle = MakeCutsceneLabel(17, new Color(0.86f, 0.9f, 0.96f));
        subtitle.Text = data?.Subtitle ?? string.Empty;
        subtitle.Position = new Vector2(width * 0.04f, height * 0.135f);
        subtitle.Size = new Vector2(width * 0.92f, 36.0f);
        root.AddChild(subtitle);

        var vs = MakeCutsceneLabel(86, accent);
        vs.Text = "VS";
        vs.Position = new Vector2(width * 0.36f, height * 0.36f);
        vs.Size = new Vector2(width * 0.28f, 118.0f);
        vs.PivotOffset = vs.Size * 0.5f;
        root.AddChild(vs);

        var taunt = MakeCutsceneLabel(19, new Color(0.96f, 0.94f, 0.86f));
        taunt.Text = data?.Taunt ?? string.Empty;
        taunt.Position = new Vector2(width * 0.05f, height * 0.78f);
        taunt.Size = new Vector2(width * 0.9f, 78.0f);
        taunt.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        root.AddChild(taunt);

        return new SplitCutsceneCard(root, playerPortrait, bossPortrait, vs, bossName, subtitle, taunt);
    }

    private (CharacterData? Player, CharacterData? Boss) ResolveCutsceneForms(string bossActorId)
    {
        if (_simulation is null)
        {
            return (null, null);
        }

        var player = _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled && !actor.IsDead);
        var boss = _simulation.Actors.FirstOrDefault(actor => actor.ActorId == bossActorId)
            ?? _simulation.Actors.FirstOrDefault(actor => actor.IsBoss);
        return (player?.CurrentForm, boss?.CurrentForm);
    }

    private static void SetControlAlpha(Control control, float alpha)
    {
        var color = control.Modulate;
        control.Modulate = new Color(color.R, color.G, color.B, alpha);
    }

    private static TextureRect MakeCutscenePortrait(string name, bool flip)
    {
        return new TextureRect
        {
            Name = name,
            FlipH = flip,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    private static Label MakeCutsceneLabel(int fontSize, Color color)
    {
        var label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = color,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.04f));
        label.AddThemeConstantOverride("outline_size", 12);
        return label;
    }

    private readonly record struct SplitCutsceneCard(
        Control Root,
        TextureRect PlayerPortrait,
        TextureRect BossPortrait,
        Label VsLabel,
        Label BossName,
        Label Subtitle,
        Label Taunt);

    private static string ResolveReadyText(string bossDisplayName)
    {
        // The matchup cutscene already identifies the major boss. Phase 3 is the
        // universal call-to-action from the original sequence: READY -> FIGHT.
        // Named elites own their separate WARNING banner in StageIntroSequencer.
        return "READY?";
    }

    /// <summary>
    /// Procedurally drawn anime "speed lines" (radial ink streaks converging on
    /// screen centre) used as the READY/FIGHT background motion graphic. Authored
    /// in code so it needs no external texture; deterministic (fixed RNG seed).
    /// </summary>
    private sealed partial class SpeedLineOverlay : Control
    {
        private const int LineCount = 72;

        public override void _Draw()
        {
            var size = Size;
            if (size.X <= 0.0f || size.Y <= 0.0f)
            {
                return;
            }

            var center = size * 0.5f;
            var outerRadius = size.Length(); // guaranteed past the corners.
            var innerRadius = Mathf.Min(size.X, size.Y) * 0.30f;
            var random = new Random(1337);
            var color = new Color(0.02f, 0.02f, 0.04f, 0.9f);

            for (var i = 0; i < LineCount; i++)
            {
                var angle = Mathf.Tau * i / LineCount
                    + (float)(random.NextDouble() - 0.5) * 0.05f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var width = 5.0f + (float)random.NextDouble() * 24.0f;
                var start = innerRadius * (0.85f + (float)random.NextDouble() * 0.5f);
                DrawLine(
                    center + direction * start,
                    center + direction * outerRadius,
                    color,
                    width);
            }
        }
    }
}
