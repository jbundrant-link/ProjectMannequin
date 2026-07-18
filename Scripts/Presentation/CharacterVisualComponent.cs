using System.Collections.Generic;
using System.IO;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Presentation;

public partial class CharacterVisualComponent : Node3D
{
    private readonly Dictionary<string, int[]> _moveFrameSequences = new()
    {
        ["mannequin_light"] = new[] { 40, 41, 41, 40 },
        ["mannequin_medium"] = new[] { 40, 42, 42, 40 },
        ["mannequin_heavy"] = new[] { 40, 43, 43, 40 },
        ["mannequin_light_kick"] = new[] { 40, 45, 45, 40 },
        ["mannequin_medium_kick"] = new[] { 40, 46, 46, 40 },
        ["mannequin_heavy_kick"] = new[] { 40, 47, 47, 40 },
        ["mannequin_launcher"] = new[] { 60, 63, 63, 60 },
        ["mannequin_crouch_light"] = new[] { 60, 61, 61, 60 },
        ["mannequin_crouch_medium"] = new[] { 60, 62, 62, 60 },
        ["mannequin_crouch_light_kick"] = new[] { 64, 65, 65, 64 },
        ["mannequin_crouch_medium_kick"] = new[] { 64, 66, 66, 64 },
        ["mannequin_crouch_heavy_kick"] = new[] { 64, 66, 67, 64 },
        ["mannequin_air_light"] = new[] { 70, 71, 71, 70 },
        ["mannequin_air_medium"] = new[] { 70, 72, 72, 70 },
        ["mannequin_air_heavy"] = new[] { 70, 72, 75, 70 },
        ["mannequin_air_light_kick"] = new[] { 70, 73, 73, 70 },
        ["mannequin_air_medium_kick"] = new[] { 70, 74, 74, 70 },
        ["mannequin_air_heavy_kick"] = new[] { 70, 75, 75, 70 },
        ["archive_pulse"] = new[] { 40, 49, 49, 40 },
        ["archive_burst"] = new[] { 40, 43, 44, 49, 49, 40 },
        ["mannequin_uppercut"] = new[] { 80, 81, 82, 83, 84, 85 },
        ["ryu_jab"] = new[] { 64, 65, 64 },
        ["ryu_close_jab"] = new[] { 64, 65, 64 },
        ["ryu_strong"] = new[] { 66, 67, 68, 66 },
        ["ryu_close_strong"] = new[] { 66, 67, 68, 66 },
        ["ryu_fierce"] = new[] { 69, 70, 71, 72, 73 },
        ["ryu_close_fierce"] = new[] { 69, 70, 71, 72, 73 },
        ["ryu_light_kick"] = new[] { 80, 81, 82 },
        ["ryu_medium_kick"] = new[] { 83, 84, 85, 86 },
        ["ryu_close_medium_kick"] = new[] { 83, 84, 85, 86 },
        ["ryu_heavy_kick"] = new[] { 83, 84, 85, 86, 87 },
        ["ryu_crouch_jab"] = new[] { 96, 97, 98, 96 },
        ["ryu_crouch_strong"] = new[] { 99, 100, 101, 99 },
        ["ryu_crouch_fierce"] = new[] { 102, 103, 104, 102 },
        ["ryu_crouch_short"] = new[] { 112, 113, 114, 112 },
        ["ryu_crouch_medium_kick"] = new[] { 115, 116, 117, 118, 115 },
        ["ryu_sweep"] = new[] { 119, 120, 121, 122, 123, 124 },
        ["ryu_air_jab"] = new[] { 128, 129, 130 },
        ["ryu_air_strong"] = new[] { 131, 132, 133 },
        ["ryu_air_fierce"] = new[] { 134, 135, 136, 137 },
        ["ryu_air_short"] = new[] { 144, 145, 146 },
        ["ryu_air_forward"] = new[] { 147, 148, 149 },
        ["ryu_air_roundhouse"] = new[] { 150, 151, 152, 153 },
        ["ryu_leap_attack"] = new[] { 147, 148, 149, 148 },
        ["ryu_hadouken"] = new[] { 176, 177, 178 },
        ["ryu_shoryuken"] = new[] { 192, 193, 194, 195, 196, 197 },
        ["ryu_tatsumaki"] = new[] { 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219 },
        ["ryu_shinku_hadouken"] = new[] { 224, 225, 226 },
        ["enemy_swing"] = new[] { 40, 43, 43, 40 },
        ["boss_cleave"] = new[] { 40, 43, 44, 43, 40 },
        ["boss_vault_lunge"] = new[] { 40, 42, 47, 47, 40 },
        ["boss_archive_sweep"] = new[] { 40, 49, 43, 49, 40 },
        ["boss_final_breaker"] = new[] { 40, 44, 43, 49, 49, 40 },
        ["boss_archive_cataclysm"] = new[] { 50, 53, 44, 49, 49, 53, 50 },
        ["knight_light"] = new[] { 40, 41, 41, 40 },
        ["knight_medium"] = new[] { 40, 42, 42, 40 },
        ["knight_heavy"] = new[] { 40, 43, 43, 40 },
        ["knight_light_kick"] = new[] { 40, 45, 45, 40 },
        ["knight_medium_kick"] = new[] { 40, 46, 46, 40 },
        ["knight_heavy_kick"] = new[] { 40, 47, 47, 40 },
        ["knight_air_light"] = new[] { 40, 41, 48, 40 },
        ["knight_air_heavy"] = new[] { 40, 48, 48, 40 },
        ["knight_cleave"] = new[] { 40, 43, 44, 43, 40 },
        ["knight_archive_burst"] = new[] { 40, 43, 44, 49, 49, 40 },
    };

    private CombatActor? _actor;
    private AnimationPlayer? _animationPlayer;
    private Sprite3D _sprite = null!;
    private Sprite3D _auraSprite = null!;
    private Label3D _label = null!;
    private MeshInstance3D _healthBack = null!;
    private MeshInstance3D _healthFill = null!;
    private StandardMaterial3D _healthFillMaterial = null!;
    private Color _teamAccent = Colors.White;
    private bool _hasTeamAccent;
    private int _sheetColumns = ProceduralMannequinSpriteSheetFactory.Columns;
    private int _sheetRows = ProceduralMannequinSpriteSheetFactory.Rows;
    private string _lastVisualStyleKey = "";
    private string _activeAnimationProfileId = "mannequin";
    private float _activePresentationScale = 1.0f;
    private Vector3 _baseSpritePosition;
    private bool _usingAnimationOverrideAtlas;
    private SpriteAnimationClipData? _activeStateClip;
    private ulong _stateClipStartedMsec;
    private CombatActorState _lastActorState;
    private SpriteAnimationClipData? _introClip;
    private bool _introClipActive;
    private ulong _introClipStartedMsec;
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private float _impactFlashSeconds;
    private Color _impactFlashColor = Colors.White;
    private int _enemyIdleCaptureFrame = -1;
    private int _enemyIdleCaptureStableRenderFrames;
    private int _enemyAttackCaptureStableRenderFrames;
    private bool _enemyIdleCaptureSaved;
    private bool _enemyAttackCaptureSaved;
    private int _introCaptureStableRenderFrames;
    private bool _introCaptureSaved;

    private readonly AnimationDriver _driver = new();
    private GameSimulation? _simulation;
    private int _animationTick;
    private int _animationElapsed;

    [Export] public PackedScene? VisualScene { get; set; }
    [Export] public Texture2D? SpriteSheet { get; set; }
    [Export] public string SpriteSheetPath { get; set; } = "res://Assets/Sprites/Mannequin/mannequin_sheet_higgsfield_v1.png";
    [Export] public int SheetColumns { get; set; } = 10;
    [Export] public int SheetRows { get; set; } = 9;
    [Export] public float SpritePixelSize { get; set; } = 0.018f;
    [Export] public float SpriteGroundOffsetPixels { get; set; } = 120.0f;
    [Export] public bool TintLoadedSpriteSheets { get; set; }

    public bool IsUsingFallbackSpriteSheet { get; private set; }
    public string LoadedSpriteSheetPath { get; private set; } = "";

    public override void _Ready()
    {
        _actor = GetParentOrNull<CombatActor>();
        _lastActorState = _actor?.State ?? CombatActorState.Idle;
        _stateClipStartedMsec = Time.GetTicksMsec();

        var visualRoot = InstantiateVisualRoot();
        _animationPlayer = visualRoot.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _sprite = visualRoot.GetNodeOrNull<Sprite3D>("Sprite3D") ?? CreateSprite(visualRoot);
        ConfigureSprite();
        CreateAuraSprite();
        BuildHudElements();
    }

    public override void _Process(double delta)
    {
        if (_actor is null)
        {
            return;
        }

        PollBossIntroEvents();

        var visualStyleKey = $"{_actor.CurrentForm.Id}:{_actor.CurrentVisualVariantId}";
        if (_lastVisualStyleKey != visualStyleKey)
        {
            _lastVisualStyleKey = visualStyleKey;
            _stateClipStartedMsec = Time.GetTicksMsec();
            ApplyFormStyle(_actor.CurrentForm);
        }

        if (_lastActorState != _actor.State)
        {
            _lastActorState = _actor.State;
            _stateClipStartedMsec = Time.GetTicksMsec();
        }

        UpdateAnimationDriver();

        ApplyActiveAnimationAtlas();
        _sprite.FlipH = !_actor.FacingRight;
        _sprite.Frame = Mathf.Clamp(
            ResolveSpriteFrame(),
            0,
            Mathf.Max(0, _sheetColumns * _sheetRows - 1));
        CaptureIntroPeakIfRequested();
        UpdateSpritePosition();
        UpdateAuraSprite();
        UpdateHitFlash();
        _impactFlashSeconds = Mathf.Max(0.0f, _impactFlashSeconds - (float)delta);
        UpdateScale();
        UpdateHud();
        CaptureEnemyFrameIfRequested();
    }

    private void CaptureEnemyFrameIfRequested()
    {
        var targetFormId = CapturedEnemyFormId();
        if (_actor is null
            || _actor.CurrentForm.Id != targetFormId
            || _actor.ActorId != OS.GetEnvironment(
                EnemyActorIdVariable(targetFormId)))
        {
            return;
        }

        var idlePath = OS.GetEnvironment(
            EnemyIdleCaptureVariable(targetFormId));
        if (!_enemyIdleCaptureSaved
            && !string.IsNullOrWhiteSpace(idlePath)
            && _actor.State == CombatActorState.Idle)
        {
            if (_enemyIdleCaptureFrame != _sprite.Frame)
            {
                _enemyIdleCaptureFrame = _sprite.Frame;
                _enemyIdleCaptureStableRenderFrames = 1;
            }
            else
            {
                _enemyIdleCaptureStableRenderFrames++;
            }

            if (_enemyIdleCaptureStableRenderFrames >= 3)
            {
                _enemyIdleCaptureSaved = SaveEnemyCapture(
                    idlePath,
                    _actor.CurrentForm.DisplayName,
                    "idle");
            }
        }

        var attackPath = OS.GetEnvironment(
            EnemyAttackCaptureVariable(targetFormId));
        var targetMoveId = CapturedEnemyMoveId();
        var targetMoveFrame = CapturedEnemyMoveFrame();
        var isStableActivePose = _actor.State == CombatActorState.Attacking
            && _actor.CurrentMove?.Id == targetMoveId
            && _actor.CurrentMoveFrame >= targetMoveFrame
            && _actor.CurrentMoveFrame <= targetMoveFrame + 2;
        _enemyAttackCaptureStableRenderFrames = isStableActivePose
            ? _enemyAttackCaptureStableRenderFrames + 1
            : 0;
        if (!_enemyAttackCaptureSaved
            && !string.IsNullOrWhiteSpace(attackPath)
            && _enemyAttackCaptureStableRenderFrames >= 3)
        {
            _enemyAttackCaptureSaved = SaveEnemyCapture(
                attackPath,
                _actor.CurrentForm.DisplayName,
                $"{_actor.CurrentMove?.DisplayName ?? targetMoveId} frame {targetMoveFrame}");
        }
    }

    private bool SaveEnemyCapture(
        string path,
        string displayName,
        string poseLabel)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok)
        {
            GD.PushError(
                $"[CharacterVisual] Could not save {displayName} {poseLabel} capture '{path}' ({error}).");
            return false;
        }

        GD.Print($"[CharacterVisual] {displayName} {poseLabel} capture saved: {path}");
        return true;
    }

    private void CaptureIntroPeakIfRequested()
    {
        var path = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_BOSS_INTRO_POSE_CAPTURE");
        var targetFormId = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_BOSS_INTRO_FORM_ID");
        if (string.IsNullOrWhiteSpace(targetFormId))
        {
            targetFormId = "archive_knight_boss";
        }

        if (_introCaptureSaved
            || string.IsNullOrWhiteSpace(path)
            || _actor is null
            || !_actor.IsBoss
            || _actor.CurrentForm.Id != targetFormId
            || !_introClipActive
            || _introClip is null
            || _introClip.Frames.Count == 0)
        {
            return;
        }

        var peakIndex = Mathf.Clamp(
            _introClip.PeakFrameIndex,
            0,
            _introClip.Frames.Count - 1);
        var peakFrame = _introClip.Frames[peakIndex];
        _introCaptureStableRenderFrames = _sprite.Frame == peakFrame
            ? _introCaptureStableRenderFrames + 1
            : 0;
        if (_introCaptureStableRenderFrames < 3)
        {
            return;
        }

        _introCaptureSaved = SaveEnemyCapture(
            path,
            _actor.CurrentForm.DisplayName,
            $"intro peak frame {peakFrame}");
    }

    private static string CapturedEnemyFormId()
    {
        var value = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_ENEMY_FORM_ID");
        return string.IsNullOrWhiteSpace(value) ? "archive_raider" : value;
    }

    private static string CapturedEnemyMoveId()
    {
        var value = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_ENEMY_MOVE_ID");
        return string.IsNullOrWhiteSpace(value) ? "archive_raider_attack" : value;
    }

    private static int CapturedEnemyMoveFrame()
    {
        var value = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_ENEMY_MOVE_FRAME");
        return int.TryParse(value, out var frame) ? Mathf.Max(0, frame) : 17;
    }

    private static string EnemyIdleCaptureVariable(string formId)
    {
        return formId switch
        {
            "index_warden_veyra" => "PROJECT_MANNEQUIN_LADDER_VEYRA_IDLE_CAPTURE",
            "archive_scout" => "PROJECT_MANNEQUIN_LADDER_SCOUT_IDLE_CAPTURE",
            "cipher_captain_rhune" => "PROJECT_MANNEQUIN_LADDER_RHUNE_IDLE_CAPTURE",
            "overseer_basalt" => "PROJECT_MANNEQUIN_LADDER_BASALT_IDLE_CAPTURE",
            "archive_bruiser" => "PROJECT_MANNEQUIN_LADDER_BRUISER_IDLE_CAPTURE",
            "archive_knight_boss" => "PROJECT_MANNEQUIN_LADDER_KNIGHT_IDLE_CAPTURE",
            _ => "PROJECT_MANNEQUIN_LADDER_RAIDER_IDLE_CAPTURE",
        };
    }

    private static string EnemyAttackCaptureVariable(string formId)
    {
        return formId switch
        {
            "index_warden_veyra" => "PROJECT_MANNEQUIN_LADDER_VEYRA_ATTACK_CAPTURE",
            "archive_scout" => "PROJECT_MANNEQUIN_LADDER_SCOUT_ATTACK_CAPTURE",
            "cipher_captain_rhune" => "PROJECT_MANNEQUIN_LADDER_RHUNE_ATTACK_CAPTURE",
            "overseer_basalt" => "PROJECT_MANNEQUIN_LADDER_BASALT_ATTACK_CAPTURE",
            "archive_bruiser" => "PROJECT_MANNEQUIN_LADDER_BRUISER_ATTACK_CAPTURE",
            "archive_knight_boss" => "PROJECT_MANNEQUIN_LADDER_KNIGHT_ATTACK_CAPTURE",
            _ => "PROJECT_MANNEQUIN_LADDER_RAIDER_ATTACK_CAPTURE",
        };
    }

    private static string EnemyActorIdVariable(string formId)
    {
        return formId switch
        {
            "index_warden_veyra" => "PROJECT_MANNEQUIN_LADDER_VEYRA_ACTOR_ID",
            "archive_scout" => "PROJECT_MANNEQUIN_LADDER_SCOUT_ACTOR_ID",
            "cipher_captain_rhune" => "PROJECT_MANNEQUIN_LADDER_RHUNE_ACTOR_ID",
            "overseer_basalt" => "PROJECT_MANNEQUIN_LADDER_BASALT_ACTOR_ID",
            "archive_bruiser" => "PROJECT_MANNEQUIN_LADDER_BRUISER_ACTOR_ID",
            "archive_knight_boss" => "PROJECT_MANNEQUIN_LADDER_KNIGHT_ACTOR_ID",
            _ => "PROJECT_MANNEQUIN_LADDER_RAIDER_ACTOR_ID",
        };
    }

    private Node3D InstantiateVisualRoot()
    {
        var packedScene = VisualScene;
        if (packedScene is null && ResourceLoader.Exists("res://Scenes/Characters/MannequinSpriteVisuals.tscn"))
        {
            packedScene = GD.Load<PackedScene>("res://Scenes/Characters/MannequinSpriteVisuals.tscn");
        }

        var visualRoot = packedScene?.Instantiate<Node3D>() ?? new Node3D { Name = "GeneratedSpriteVisuals" };
        AddChild(visualRoot);
        return visualRoot;
    }

    private static Sprite3D CreateSprite(Node parent)
    {
        var sprite = new Sprite3D
        {
            Name = "Sprite3D",
        };
        parent.AddChild(sprite);
        return sprite;
    }

    private void ConfigureSprite()
    {
        _sprite.Texture = ResolveSpriteTexture(_actor?.CurrentForm);
        _sprite.Hframes = _sheetColumns;
        _sprite.Vframes = _sheetRows;
        _sprite.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps;
        _sprite.Position = _baseSpritePosition;
        _sprite.Shaded = true;
        _sprite.AlphaCut = SpriteBase3D.AlphaCutMode.Discard;
        _sprite.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
        _sprite.DoubleSided = true;
    }

    private void CreateAuraSprite()
    {
        _auraSprite = new Sprite3D
        {
            Name = "PhaseAuraSprite",
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
            Shaded = false,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            DoubleSided = true,
            Visible = false,
        };
        (_sprite.GetParent() ?? this).AddChild(_auraSprite);
    }

    private void BuildHudElements()
    {
        _healthFillMaterial = MakeMaterial(new Color(0.2f, 0.95f, 0.35f));

        _healthBack = MakeBox("HealthBack", MakeMaterial(new Color(0.08f, 0.08f, 0.08f)));
        _healthFill = MakeBox("HealthFill", _healthFillMaterial);

        _label = new Label3D
        {
            Name = "ActorLabel",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.006f,
            Modulate = Colors.White,
            OutlineSize = 3,
            Text = "",
        };
        AddChild(_label);
    }

    private int ResolveSpriteFrame()
    {
        if (_actor is null)
        {
            return 0;
        }

        if (_introClipActive && _introClip is not null)
        {
            return ResolveIntroClipFrame(_introClip);
        }

        if (_activeStateClip is not null)
        {
            return ResolveStateClipFrame(_activeStateClip);
        }

        return _actor.State switch
        {
            CombatActorState.Idle => UsesAnimationProfile("ryu_v4")
                ? AnimatedFrame(row: 0, frameCount: 8, ticksPerFrame: 5)
                : UsesAnimationProfile("goku")
                    ? AnimatedFrame(row: 0, frameCount: 6, ticksPerFrame: 10)
                : AnimatedFrame(row: 0, frameCount: 4, ticksPerFrame: 12),
            CombatActorState.Crouching => UsesAnimationProfile("ryu_v4")
                ? 96
                : UsesAnimationProfile("goku") ? 8 : 60,
            CombatActorState.Walking => UsesAnimationProfile("ryu_v4")
                ? AnimatedFrame(row: 1, frameCount: 11, ticksPerFrame: 3)
                : UsesAnimationProfile("goku")
                    ? AnimatedSequence(startFrame: 9, frameCount: 10, ticksPerFrame: 5)
                : AnimatedFrame(row: 1, frameCount: 8, ticksPerFrame: 4),
            CombatActorState.Dashing => UsesAnimationProfile("ryu_v4")
                ? AnimatedFrame(row: 2, frameCount: 9, ticksPerFrame: 3)
                : UsesAnimationProfile("goku")
                    ? AnimatedSequence(startFrame: 9, frameCount: 10, ticksPerFrame: 2)
                : AnimatedFrame(row: 2, frameCount: 6, ticksPerFrame: 3),
            CombatActorState.HomingDash => UsesAnimationProfile("ryu_v4")
                ? AnimatedFrame(row: 2, frameCount: 9, ticksPerFrame: 2)
                : UsesAnimationProfile("goku")
                    ? AnimatedSequence(startFrame: 50, frameCount: 8, ticksPerFrame: 2)
                : AnimatedFrame(row: 2, frameCount: 6, ticksPerFrame: 2),
            CombatActorState.JumpStartup => UsesAnimationProfile("ryu_v4")
                ? 48
                : UsesAnimationProfile("goku") ? 19 : 30,
            CombatActorState.Jumping => ResolveJumpFrame(),
            CombatActorState.Flying => UsesAnimationProfile("goku")
                ? AnimatedSequence(startFrame: 20, frameCount: 6, ticksPerFrame: 6)
                : ResolveJumpFrame(),
            CombatActorState.InstinctEvade => UsesAnimationProfile("goku")
                ? AnimatedSequence(startFrame: 50, frameCount: 8, ticksPerFrame: 2)
                : 51,
            CombatActorState.Landing => UsesAnimationProfile("ryu_v4")
                ? 59
                : UsesAnimationProfile("goku") ? 26 : 33,
            CombatActorState.Blocking => UsesAnimationProfile("ryu_v4")
                ? 163
                : UsesAnimationProfile("goku") ? 28 : 50,
            CombatActorState.Blockstun => UsesAnimationProfile("ryu_v4")
                ? 163
                : UsesAnimationProfile("goku") ? 29 : 50,
            CombatActorState.Parrying => UsesAnimationProfile("ryu_v4")
                ? 164
                : UsesAnimationProfile("goku") ? 28 : 51,
            CombatActorState.Hitstun => UsesAnimationProfile("ryu_v4")
                ? AnimatedFrame(row: 10, startColumn: 1, frameCount: 3, ticksPerFrame: 5)
                : UsesAnimationProfile("goku")
                    ? AnimatedSequence(startFrame: 44, frameCount: 3, ticksPerFrame: 5)
                : AnimatedFrame(row: 5, frameCount: 3, ticksPerFrame: 5),
            CombatActorState.GuardBreak => UsesAnimationProfile("ryu_v4")
                ? 164
                : UsesAnimationProfile("goku") ? 8 : 52,
            CombatActorState.Knockdown => UsesAnimationProfile("ryu_v4")
                ? 166
                : UsesAnimationProfile("goku") ? 86 : 56,
            CombatActorState.Dead => UsesAnimationProfile("ryu_v4")
                ? 167
                : UsesAnimationProfile("goku") ? 90 : 57,
            CombatActorState.FormSwapping => UsesAnimationProfile("ryu_v4")
                ? AnimatedFrame(row: 10, startColumn: 7, frameCount: 3, ticksPerFrame: 3)
                : UsesAnimationProfile("goku")
                    ? AnimatedSequence(startFrame: 0, frameCount: 6, ticksPerFrame: 3)
                : AnimatedFrame(row: 5, startColumn: 3, frameCount: 3, ticksPerFrame: 3),
            CombatActorState.CinematicLocked => UsesAnimationProfile("ryu_v4")
                ? AnimatedFrame(row: 10, startColumn: 7, frameCount: 3, ticksPerFrame: 3)
                : UsesAnimationProfile("goku")
                    ? AnimatedSequence(startFrame: 0, frameCount: 6, ticksPerFrame: 3)
                : AnimatedFrame(row: 5, startColumn: 3, frameCount: 3, ticksPerFrame: 3),
            CombatActorState.Attacking => ResolveAttackFrame(),
            _ => 0,
        };
    }

    private int ResolveJumpFrame()
    {
        if (_actor is null)
        {
            return 31;
        }

        if (UsesAnimationProfile("ryu_v4"))
        {
            var jumpVelocity = Mathf.Max(1.0f, _actor.CurrentForm.JumpVelocity);
            var progress = Mathf.Clamp(
                (jumpVelocity - _actor.Velocity.Y) / (jumpVelocity * 2.0f),
                0.0f,
                0.999f);
            var column = Mathf.Clamp(Mathf.FloorToInt(progress * 12.0f), 0, 11);
            return 3 * _sheetColumns + column;
        }

        if (UsesAnimationProfile("goku"))
        {
            var jumpVelocity = Mathf.Max(1.0f, _actor.CurrentForm.JumpVelocity);
            var progress = Mathf.Clamp(
                (jumpVelocity - _actor.Velocity.Y) / (jumpVelocity * 2.0f),
                0.0f,
                0.999f);
            var frame = Mathf.Clamp(Mathf.FloorToInt(progress * 6.0f), 0, 5);
            return 20 + frame;
        }

        return _actor.Velocity.Y switch
        {
            > 2.5f => 31,
            > -2.5f => 32,
            _ => 32,
        };
    }

    private bool UsesAnimationProfile(string profileId)
    {
        return _activeAnimationProfileId == profileId;
    }

    private int ResolveAttackFrame()
    {
        if (_actor?.CurrentMove is null)
        {
            return 0;
        }

        var move = _actor.CurrentMove;
        IReadOnlyList<int> sequence;
        if (move.AnimationFrameSequence.Count > 0)
        {
            sequence = move.AnimationFrameSequence;
        }
        else if (_moveFrameSequences.TryGetValue(move.Id, out var mappedSequence))
        {
            sequence = mappedSequence;
        }
        else
        {
            sequence = _moveFrameSequences["mannequin_light"];
        }

        if (sequence[0] >= _sheetColumns * _sheetRows)
        {
            sequence = _moveFrameSequences["mannequin_light"];
        }

        var animationIndex = ResolveAnimationIndex(move, sequence.Count);
        return sequence[Mathf.Clamp(animationIndex, 0, sequence.Count - 1)];
    }

    private int ResolveAnimationIndex(MoveData move, int sequenceCount)
    {
        var moveFrameCount = Mathf.Max(1, move.TotalFrames);
        var currentMoveFrame = Mathf.Clamp(_actor?.CurrentMoveFrame ?? 0, 0, moveFrameCount - 1);
        if (move.AnimationFrameDurations.Count == sequenceCount)
        {
            var totalDuration = 0;
            foreach (var duration in move.AnimationFrameDurations)
            {
                totalDuration += Mathf.Max(1, duration);
            }

            var weightedFrame = Mathf.FloorToInt(
                currentMoveFrame / (float)moveFrameCount * totalDuration);
            var accumulated = 0;
            for (var index = 0; index < sequenceCount; index++)
            {
                accumulated += Mathf.Max(1, move.AnimationFrameDurations[index]);
                if (weightedFrame < accumulated)
                {
                    return index;
                }
            }

            return sequenceCount - 1;
        }

        return Mathf.FloorToInt(
            currentMoveFrame / (float)moveFrameCount * sequenceCount);
    }

    private void UpdateAnimationDriver()
    {
        if (_actor is null)
        {
            return;
        }

        _simulation ??= _actor.Simulation;
        var move = _actor.CurrentMove;

        // Prefer the simulation's frozen-aware presentation clock so idle/walk
        // loops freeze during hitstop and super freeze; fall back to wall-clock
        // ticks when running without a simulation (e.g. isolated previews).
        var presentationClock = _simulation?.PresentationClock
            ?? (int)(Time.GetTicksMsec() * 60 / 1000);

        var snapshot = new AnimationDriverSnapshot(
            _actor.State,
            move?.Id ?? "",
            _actor.CurrentMoveFrame,
            move?.TotalFrames ?? 0,
            _actor.FacingRight,
            presentationClock,
            _simulation?.HitStopFramesRemaining ?? 0,
            _simulation?.SuperPauseFramesRemaining ?? 0,
            _actor.CurrentBossPhaseIndex);

        var driverState = _driver.Resolve(snapshot);
        _animationTick = presentationClock;
        _animationElapsed = driverState.StateClipElapsed;

        // Freeze any AnimationPlayer so it resumes from the authoritative combat
        // frame after the simulation-owned freeze ends.
        if (_animationPlayer is not null)
        {
            _animationPlayer.SpeedScale = driverState.IsFrozen ? 0.0f : 1.0f;
        }
    }

    private int ResolveStateClipFrame(SpriteAnimationClipData clip)
    {
        if (clip.Frames.Count == 0)
        {
            return 0;
        }

        var elapsedTicks = Mathf.Max(0, _animationElapsed);
        if (clip.Durations.Count != clip.Frames.Count)
        {
            return clip.Frames[Mathf.Min(elapsedTicks / 2, clip.Frames.Count - 1)];
        }

        var accumulated = 0;
        for (var index = 0; index < clip.Frames.Count; index++)
        {
            accumulated += Mathf.Max(1, clip.Durations[index]);
            if (elapsedTicks < accumulated)
            {
                return clip.Frames[index];
            }
        }

        return clip.Frames[^1];
    }

    // ----- Boss-fight intro clip (presentation-only, wall-clock timed) --------

    private void PollBossIntroEvents()
    {
        _simulation ??= _actor?.Simulation;
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
                case CombatPresentationEventType.HitConnected:
                case CombatPresentationEventType.LauncherHit:
                case CombatPresentationEventType.CounterHit:
                case CombatPresentationEventType.PunishCounter:
                    if (presentationEvent.TargetActorId == _actor?.ActorId)
                    {
                        TriggerImpactFlash(
                            presentationEvent.Type == CombatPresentationEventType.PunishCounter
                                ? new Color(1.0f, 0.72f, 0.48f)
                                : Colors.White,
                            presentationEvent.Type == CombatPresentationEventType.HitConnected ? 0.065f : 0.10f);
                    }
                    break;
                case CombatPresentationEventType.HazardZoneDamage:
                    if (presentationEvent.TargetActorId == _actor?.ActorId)
                    {
                        TriggerImpactFlash(new Color(1.0f, 0.34f, 0.24f), 0.11f);
                    }
                    break;
                case CombatPresentationEventType.ArmorAbsorbed:
                    if (presentationEvent.SourceActorId == _actor?.ActorId)
                    {
                        TriggerImpactFlash(new Color(0.76f, 0.58f, 1.0f), 0.09f);
                    }
                    break;
                case CombatPresentationEventType.BossIntroStarted:
                    BeginIntroClip();
                    break;
                case CombatPresentationEventType.BossIntroFight:
                    EndIntroClip();
                    break;
            }
        }
    }

    private void TriggerImpactFlash(Color color, float duration)
    {
        _impactFlashColor = color;
        _impactFlashSeconds = Mathf.Max(_impactFlashSeconds, duration);
    }

    private void BeginIntroClip()
    {
        // Only the fighters actually on stage for the duel strike an intro pose.
        if (_actor is null || !(_actor.IsBoss || _actor.IsPlayerControlled))
        {
            return;
        }

        var clip = _actor.CurrentForm.IntroAnimation;
        if (clip is null || clip.Frames.Count == 0)
        {
            return;
        }

        // A dedicated atlas is optional; if one is named it must be imported or we
        // hold the idle stance rather than sampling frames from the wrong sheet.
        if (!string.IsNullOrWhiteSpace(clip.AtlasPath) && !ResourceLoader.Exists(clip.AtlasPath))
        {
            return;
        }

        _introClip = clip;
        _introClipActive = true;
        _introClipStartedMsec = Time.GetTicksMsec();
    }

    private void EndIntroClip()
    {
        if (!_introClipActive)
        {
            return;
        }

        _introClipActive = false;
        _introClip = null;

        // Restore the normal form sheet so state-driven animation resumes cleanly
        // once the simulation unfreezes at the FIGHT release.
        if (_actor is not null)
        {
            ApplyFormStyle(_actor.CurrentForm);
        }
    }

    private int ResolveIntroClipFrame(SpriteAnimationClipData clip)
    {
        if (clip.Frames.Count == 0)
        {
            return 0;
        }

        // Wall-clock elapsed in 60 Hz "ticks": the simulation's PresentationClock is
        // frozen during the intro, so the clip must advance on real time instead.
        var elapsedTicks = (int)((Time.GetTicksMsec() - _introClipStartedMsec) * 60UL / 1000UL);
        var loopStart = Mathf.Clamp(clip.LoopStartFrameIndex, 0, clip.Frames.Count - 1);

        if (clip.Durations.Count != clip.Frames.Count)
        {
            // No authored durations: ~6 ticks/frame. Play through once, then loop
            // only the trailing stance frames (fighting-game "settle into stance").
            var frame = elapsedTicks / 6;
            if (frame >= clip.Frames.Count)
            {
                var tailCount = Mathf.Max(1, clip.Frames.Count - loopStart);
                frame = loopStart + (frame - clip.Frames.Count) % tailCount;
            }

            return clip.Frames[frame];
        }

        var totalDuration = 0;
        foreach (var duration in clip.Durations)
        {
            totalDuration += Mathf.Max(1, duration);
        }

        // First pass: the full flourish plays exactly once.
        if (elapsedTicks < totalDuration)
        {
            var accumulated = 0;
            for (var index = 0; index < clip.Frames.Count; index++)
            {
                accumulated += Mathf.Max(1, clip.Durations[index]);
                if (elapsedTicks < accumulated)
                {
                    return clip.Frames[index];
                }
            }

            return clip.Frames[^1];
        }

        // Settle: loop only the trailing frames from LoopStartFrameIndex so the
        // fighter holds/idle-bounces the combat stance until the FIGHT release.
        var tailDuration = 0;
        for (var index = loopStart; index < clip.Frames.Count; index++)
        {
            tailDuration += Mathf.Max(1, clip.Durations[index]);
        }

        var tailTicks = (elapsedTicks - totalDuration) % Mathf.Max(1, tailDuration);
        var tailAccumulated = 0;
        for (var index = loopStart; index < clip.Frames.Count; index++)
        {
            tailAccumulated += Mathf.Max(1, clip.Durations[index]);
            if (tailTicks < tailAccumulated)
            {
                return clip.Frames[index];
            }
        }

        return clip.Frames[^1];
    }

    private int AnimatedFrame(int row, int frameCount, int ticksPerFrame, int startColumn = 0)
    {
        if (row >= _sheetRows)
        {
            return 0;
        }

        var tickDivisor = (ulong)Mathf.Max(1, ticksPerFrame);
        var tick = (ulong)Mathf.Max(0, _animationTick) / tickDivisor;
        var safeFrameCount = Mathf.Min(Mathf.Max(1, frameCount), Mathf.Max(1, _sheetColumns - startColumn));
        var column = startColumn + (int)(tick % (ulong)safeFrameCount);
        return row * _sheetColumns + column;
    }

    private int AnimatedSequence(int startFrame, int frameCount, int ticksPerFrame)
    {
        var totalFrames = _sheetColumns * _sheetRows;
        if (startFrame < 0 || startFrame >= totalFrames)
        {
            return 0;
        }

        var safeFrameCount = Mathf.Min(
            Mathf.Max(1, frameCount),
            totalFrames - startFrame);
        var tickDivisor = (ulong)Mathf.Max(1, ticksPerFrame);
        var tick = (ulong)Mathf.Max(0, _animationTick) / tickDivisor;
        return startFrame + (int)(tick % (ulong)safeFrameCount);
    }

    private void UpdateSpritePosition()
    {
        if (_actor is null)
        {
            return;
        }

        var transitionOffset = _actor.State is CombatActorState.JumpStartup or CombatActorState.Landing
            ? -0.08f
            : 0.0f;
        _sprite.Position = new Vector3(
            _baseSpritePosition.X,
            _baseSpritePosition.Y + transitionOffset,
            _baseSpritePosition.Z);
    }

    private void UpdateAuraSprite()
    {
        // Boss-intro ki-aura takes priority: flare behind the fighter at the
        // power-up peak (wall-clock, since the sim is frozen during the intro).
        if (_introClipActive && _introClip is { } introClip && introClip.IntroAuraColor.A > 0.001f)
        {
            UpdateIntroAura(introClip);
            return;
        }

        if (_actor?.CurrentVisualVariant is not { } variant
            || variant.AuraColor.A <= 0.001f
            || _actor.IsFlightActive
            || _actor.SimPosition.Y > 0.1f)
        {
            _auraSprite.Visible = false;
            return;
        }

        var pulse = 0.22f + 0.06f * Mathf.Sin(Time.GetTicksMsec() * 0.008f);
        _auraSprite.Visible = true;
        _auraSprite.Texture = _sprite.Texture;
        _auraSprite.Hframes = _sprite.Hframes;
        _auraSprite.Vframes = _sprite.Vframes;
        _auraSprite.Frame = _sprite.Frame;
        _auraSprite.FlipH = _sprite.FlipH;
        _auraSprite.PixelSize = _sprite.PixelSize;
        _auraSprite.Position = _sprite.Position + new Vector3(0.0f, 0.0f, -0.025f);
        _auraSprite.Scale = Vector3.One * 1.055f;
        _auraSprite.Modulate = new Color(
            variant.AuraColor.R,
            variant.AuraColor.G,
            variant.AuraColor.B,
            pulse);
    }

    private void UpdateIntroAura(SpriteAnimationClipData clip)
    {
        var intensity = ResolveIntroAuraIntensity(clip);
        if (intensity <= 0.001f)
        {
            _auraSprite.Visible = false;
            return;
        }

        // Flash white-hot at the very peak for an "impact frame" pop.
        var hot = Mathf.Pow(intensity, 3.0f);
        var color = clip.IntroAuraColor.Lerp(Colors.White, hot * 0.6f);
        var flicker = 0.82f + 0.18f * Mathf.Sin(Time.GetTicksMsec() * 0.03f);

        _auraSprite.Visible = true;
        _auraSprite.Texture = _sprite.Texture;
        _auraSprite.Hframes = _sprite.Hframes;
        _auraSprite.Vframes = _sprite.Vframes;
        _auraSprite.Frame = _sprite.Frame;
        _auraSprite.FlipH = _sprite.FlipH;
        _auraSprite.PixelSize = _sprite.PixelSize;
        _auraSprite.Position = _sprite.Position + new Vector3(0.0f, 0.0f, -0.03f);
        _auraSprite.Scale = Vector3.One * (1.06f + intensity * 0.32f);
        _auraSprite.Modulate = new Color(color.R, color.G, color.B, intensity * 0.6f * flicker);
    }

    private float ResolveIntroAuraIntensity(SpriteAnimationClipData clip)
    {
        if (clip.Frames.Count == 0 || clip.Durations.Count != clip.Frames.Count)
        {
            return 0.0f;
        }

        var elapsedTicks = (int)((Time.GetTicksMsec() - _introClipStartedMsec) * 60UL / 1000UL);
        var peakIndex = Mathf.Clamp(clip.PeakFrameIndex, 0, clip.Frames.Count - 1);
        var total = 0;
        var peakStart = 0;
        for (var index = 0; index < clip.Frames.Count; index++)
        {
            if (index == peakIndex)
            {
                peakStart = total;
            }

            total += Mathf.Max(1, clip.Durations[index]);
        }

        // Once the first-pass flourish is over (settled into stance), no aura.
        if (elapsedTicks >= total)
        {
            return 0.0f;
        }

        var peakMid = peakStart + Mathf.Max(1, clip.Durations[peakIndex]) * 0.5f;
        return elapsedTicks <= peakMid
            ? Mathf.Clamp(elapsedTicks / Mathf.Max(1.0f, peakMid), 0.0f, 1.0f)
            : Mathf.Clamp(1.0f - (elapsedTicks - peakMid) / Mathf.Max(1.0f, total - peakMid), 0.0f, 1.0f);
    }

    private void UpdateScale()
    {
        if (_actor is null)
        {
            return;
        }

        var bossScale = _actor.IsBoss ? _actor.CurrentForm.BossPresentationScale : 1.0f;
        var formScale = _actor.CurrentForm.Id.Contains("knight") && _actor.TeamId == 1 ? 1.08f : 1.0f;
        _sprite.Scale = Vector3.One;
        Scale = Vector3.One * bossScale * formScale * _activePresentationScale;
    }

    private void UpdateHitFlash()
    {
        if (_actor is null)
        {
            return;
        }

        if (_impactFlashSeconds > 0.0f)
        {
            _sprite.Modulate = _impactFlashColor;
            return;
        }

        if (_actor.State is CombatActorState.FormSwapping or CombatActorState.CinematicLocked)
        {
            var pulse = Time.GetTicksMsec() / 80 % 2 == 0 ? 1.0f : 0.72f;
            _sprite.Modulate = new Color(pulse, pulse, pulse, 1.0f);
            return;
        }

        if (_actor.State == CombatActorState.Parrying)
        {
            var pulse = Time.GetTicksMsec() / 65 % 2 == 0 ? 1.0f : 0.68f;
            _sprite.Modulate = new Color(0.34f * pulse, 0.94f * pulse, 1.0f);
            return;
        }

        if (_actor.State == CombatActorState.GuardBreak)
        {
            var pulse = Time.GetTicksMsec() / 85 % 2 == 0 ? 1.0f : 0.62f;
            _sprite.Modulate = new Color(1.0f, 0.34f * pulse, 0.16f * pulse);
            return;
        }

        if (_actor.State == CombatActorState.Attacking
            && _actor.CurrentMove is { IsSuper: true }
            && _actor.CurrentMoveFrame < _actor.CurrentMove.StartupFrames)
        {
            var pulse = Time.GetTicksMsec() / 55 % 2 == 0 ? 1.0f : 0.58f;
            _sprite.Modulate = new Color(1.0f, 0.74f * pulse, 0.30f * pulse);
            return;
        }

        if (_actor.State == CombatActorState.Hitstun && Time.GetTicksMsec() / 70 % 2 == 0)
        {
            _sprite.Modulate = new Color(1.0f, 0.44f, 0.44f);
            return;
        }

        if (_actor.State == CombatActorState.Blockstun)
        {
            _sprite.Modulate = new Color(0.58f, 0.84f, 1.0f);
            return;
        }

        _sprite.Modulate = ShouldTintCurrentSprite() ? _teamAccent : Colors.White;
    }

    private void UpdateHud()
    {
        if (_actor is null)
        {
            return;
        }

        var showWorldHud = _actor.TeamId != 1 && !_actor.IsBoss;
        _healthBack.Visible = showWorldHud;
        _healthFill.Visible = showWorldHud;
        _label.Visible = showWorldHud;
        if (!showWorldHud)
        {
            return;
        }

        var maxHealth = Mathf.Max(1, _actor.CurrentForm.MaxHealth);
        var healthRatio = Mathf.Clamp((float)_actor.Health / maxHealth, 0.0f, 1.0f);
        var barY = _actor.IsBoss ? 4.72f : 4.48f;

        _healthBack.Scale = new Vector3(_actor.IsBoss ? 1.55f : 1.25f, 0.08f, 0.04f);
        _healthBack.Position = new Vector3(0.0f, barY, 0.0f);
        _healthFill.Scale = new Vector3(_healthBack.Scale.X * healthRatio, _healthBack.Scale.Y * 0.72f, _healthBack.Scale.Z * 1.2f);
        _healthFill.Position = _healthBack.Position + new Vector3((_healthFill.Scale.X - _healthBack.Scale.X) * 0.5f, 0.012f, -0.03f);
        _healthFillMaterial.AlbedoColor = healthRatio switch
        {
            > 0.55f => new Color(0.2f, 0.95f, 0.35f),
            > 0.25f => new Color(1.0f, 0.82f, 0.2f),
            _ => new Color(1.0f, 0.2f, 0.16f),
        };

        _label.Position = new Vector3(0.0f, barY + 0.22f, 0.0f);
        _label.Text = _actor.IsBoss
            ? "BOSS"
            : _actor.TeamId == 1
                ? $"P{_actor.PlayerId}"
                : "ENEMY";
    }

    private void ApplyFormStyle(CharacterData form)
    {
        var variant = _actor?.CurrentVisualVariant;
        _usingAnimationOverrideAtlas = false;
        _activeStateClip = null;
        _activeAnimationProfileId = !string.IsNullOrWhiteSpace(variant?.AnimationProfileId)
            ? variant.AnimationProfileId
            : form.AnimationProfileId;
        _activePresentationScale = variant?.PresentationScale ?? 1.0f;
        _sprite.Texture = ResolveSpriteTexture(form, variant);
        _sprite.Hframes = _sheetColumns;
        _sprite.Vframes = _sheetRows;
    }

    private void ApplyActiveAnimationAtlas()
    {
        if (_actor is null)
        {
            return;
        }

        var move = _actor.State == CombatActorState.Attacking
            ? _actor.CurrentMove
            : null;
        var moveAtlasPath = ResolveMoveAtlasPath(move);
        var hasMoveAtlas = move is not null
            && !string.IsNullOrWhiteSpace(moveAtlasPath)
            && move.AnimationAtlasColumns > 0
            && move.AnimationAtlasRows > 0
            && ResourceLoader.Exists(moveAtlasPath);
        var stateClip = ResolveStateClip();
        var hasStateClip = stateClip is not null
            && !string.IsNullOrWhiteSpace(stateClip.AtlasPath)
            && stateClip.AtlasColumns > 0
            && stateClip.AtlasRows > 0
            && ResourceLoader.Exists(stateClip.AtlasPath);

        if (!hasMoveAtlas && !hasStateClip)
        {
            if (_usingAnimationOverrideAtlas)
            {
                ApplyFormStyle(_actor.CurrentForm);
            }
            return;
        }

        var atlasPath = hasMoveAtlas ? moveAtlasPath : stateClip!.AtlasPath;
        if (_usingAnimationOverrideAtlas
            && LoadedSpriteSheetPath == atlasPath
            && _activeStateClip == stateClip)
        {
            return;
        }

        _usingAnimationOverrideAtlas = true;
        _activeStateClip = hasStateClip && !hasMoveAtlas ? stateClip : null;
        IsUsingFallbackSpriteSheet = false;
        LoadedSpriteSheetPath = atlasPath;
        _sheetColumns = hasMoveAtlas ? move!.AnimationAtlasColumns : stateClip!.AtlasColumns;
        _sheetRows = hasMoveAtlas ? move!.AnimationAtlasRows : stateClip!.AtlasRows;
        _sprite.Texture = GD.Load<Texture2D>(atlasPath);
        _sprite.Hframes = _sheetColumns;
        _sprite.Vframes = _sheetRows;
        var pixelSize = hasMoveAtlas
            ? move!.AnimationPixelSize
            : stateClip!.PixelSize;
        var groundOffset = hasMoveAtlas
            ? move!.AnimationGroundOffsetPixels
            : stateClip!.GroundOffsetPixels;
        if (pixelSize <= 0.0f)
        {
            pixelSize = _actor.CurrentForm.SpritePixelSize;
        }
        if (groundOffset <= 0.0f)
        {
            groundOffset = _actor.CurrentForm.SpriteGroundOffsetPixels;
        }
        ApplySpriteMetrics(pixelSize, groundOffset);
        _sprite.Modulate = Colors.White;
    }

    private string ResolveMoveAtlasPath(MoveData? move)
    {
        if (move is null)
        {
            return "";
        }

        var variantId = _actor?.CurrentVisualVariantId ?? "";
        return !string.IsNullOrWhiteSpace(variantId)
            && move.AnimationVariantAtlasPaths.TryGetValue(variantId, out var variantPath)
            && !string.IsNullOrWhiteSpace(variantPath)
                ? variantPath
                : move.AnimationAtlasPath;
    }

    private SpriteAnimationClipData? ResolveStateClip()
    {
        if (_actor is null)
        {
            return null;
        }

        if (_introClipActive && _introClip is not null)
        {
            return _introClip;
        }

        return _actor.State switch
        {
            CombatActorState.CinematicLocked =>
                _actor.CurrentBossPhase?.TransitionAnimation,
            CombatActorState.InstinctEvade =>
                _actor.CurrentVisualVariant?.InstinctEvadeAnimation,
            _ => null,
        };
    }

    public void SetColor(Color teamColor)
    {
        _teamAccent = teamColor;
        _hasTeamAccent = true;

        if (_sprite is not null && _actor is not null)
        {
            ApplyFormStyle(_actor.CurrentForm);
        }
    }

    private MannequinSpritePalette CreatePalette(CharacterData? form = null)
    {
        var actor = _actor;
        var id = form?.Id ?? actor?.CurrentForm.Id ?? "";
        var accent = _hasTeamAccent
            ? _teamAccent
            : actor is { TeamId: 1 }
            ? GameConstants.StandardPlayerColors[Mathf.Clamp(actor.PlayerId - 1, 0, GameConstants.StandardPlayerColors.Length - 1)]
            : new Color(1.0f, 0.36f, 0.20f);

        if (id.Contains("knight"))
        {
            return new MannequinSpritePalette(
                new Color(0.04f, 0.04f, 0.07f),
                actor is { TeamId: 1 } ? new Color(0.68f, 0.74f, 0.82f) : new Color(0.54f, 0.44f, 0.70f),
                actor is { TeamId: 1 } ? new Color(0.92f, 0.96f, 1.0f) : new Color(0.82f, 0.68f, 1.0f),
                actor is { TeamId: 1 } ? new Color(0.28f, 0.34f, 0.45f) : new Color(0.25f, 0.17f, 0.34f),
                new Color(0.08f, 0.07f, 0.11f),
                accent);
        }

        return new MannequinSpritePalette(
            new Color(0.055f, 0.035f, 0.055f),
            new Color(0.86f, 0.61f, 0.43f),
            new Color(1.0f, 0.82f, 0.60f),
            new Color(0.48f, 0.27f, 0.25f),
            new Color(0.11f, 0.07f, 0.10f),
            accent);
    }

    private Texture2D ResolveSpriteTexture(
        CharacterData? form = null,
        CharacterVisualVariantData? variant = null)
    {
        if (variant is not null
            && !string.IsNullOrWhiteSpace(variant.SpriteSheetPath)
            && ResourceLoader.Exists(variant.SpriteSheetPath))
        {
            IsUsingFallbackSpriteSheet = false;
            LoadedSpriteSheetPath = variant.SpriteSheetPath;
            _sheetColumns = Mathf.Max(1, variant.SpriteSheetColumns);
            _sheetRows = Mathf.Max(1, variant.SpriteSheetRows);
            ApplySpriteMetrics(variant.SpritePixelSize, variant.SpriteGroundOffsetPixels);
            _sprite.Modulate = variant.TintSpriteSheet ? _teamAccent : Colors.White;
            return GD.Load<Texture2D>(variant.SpriteSheetPath);
        }

        if (form is not null
            && !string.IsNullOrWhiteSpace(form.SpriteSheetPath)
            && ResourceLoader.Exists(form.SpriteSheetPath))
        {
            IsUsingFallbackSpriteSheet = false;
            LoadedSpriteSheetPath = form.SpriteSheetPath;
            _sheetColumns = Mathf.Max(1, form.SpriteSheetColumns);
            _sheetRows = Mathf.Max(1, form.SpriteSheetRows);
            ApplySpriteMetrics(form.SpritePixelSize, form.SpriteGroundOffsetPixels);
            _sprite.Modulate = form.TintSpriteSheet ? _teamAccent : Colors.White;
            return GD.Load<Texture2D>(form.SpriteSheetPath);
        }

        if (SpriteSheet is not null)
        {
            IsUsingFallbackSpriteSheet = false;
            LoadedSpriteSheetPath = "<exported SpriteSheet resource>";
            _sheetColumns = Mathf.Max(1, SheetColumns);
            _sheetRows = Mathf.Max(1, SheetRows);
            ApplySpriteMetrics(SpritePixelSize, SpriteGroundOffsetPixels);
            _sprite.Modulate = ShouldTintCurrentSprite() ? _teamAccent : Colors.White;
            return SpriteSheet;
        }

        if (!string.IsNullOrWhiteSpace(SpriteSheetPath) && ResourceLoader.Exists(SpriteSheetPath))
        {
            IsUsingFallbackSpriteSheet = false;
            LoadedSpriteSheetPath = SpriteSheetPath;
            _sheetColumns = Mathf.Max(1, SheetColumns);
            _sheetRows = Mathf.Max(1, SheetRows);
            ApplySpriteMetrics(SpritePixelSize, SpriteGroundOffsetPixels);
            _sprite.Modulate = ShouldTintCurrentSprite() ? _teamAccent : Colors.White;
            return GD.Load<Texture2D>(SpriteSheetPath);
        }

        IsUsingFallbackSpriteSheet = true;
        LoadedSpriteSheetPath = string.IsNullOrWhiteSpace(SpriteSheetPath)
            ? "<no SpriteSheetPath configured>"
            : $"missing {SpriteSheetPath}";
        _sheetColumns = ProceduralMannequinSpriteSheetFactory.Columns;
        _sheetRows = ProceduralMannequinSpriteSheetFactory.Rows;
        ApplySpriteMetrics(SpritePixelSize, SpriteGroundOffsetPixels);
        _sprite.Modulate = Colors.White;
        return ProceduralMannequinSpriteSheetFactory.Create(CreatePalette(form));
    }

    private void ApplySpriteMetrics(float pixelSize, float groundOffsetPixels)
    {
        _sprite.PixelSize = pixelSize;
        _baseSpritePosition = new Vector3(0.0f, pixelSize * groundOffsetPixels, -0.08f);
    }

    private bool ShouldTintCurrentSprite()
    {
        if (_actor?.CurrentVisualVariant is { } variant
            && !string.IsNullOrWhiteSpace(variant.SpriteSheetPath))
        {
            return variant.TintSpriteSheet;
        }

        return _actor?.CurrentForm is { } form
            && !string.IsNullOrWhiteSpace(form.SpriteSheetPath)
            ? form.TintSpriteSheet
            : TintLoadedSpriteSheets;
    }

    private MeshInstance3D MakeBox(string nodeName, Material material)
    {
        var meshInstance = new MeshInstance3D
        {
            Name = nodeName,
            Mesh = new BoxMesh { Size = Vector3.One },
            MaterialOverride = material,
        };
        AddChild(meshInstance);
        return meshInstance;
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = 0.88f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
        };
    }
}
