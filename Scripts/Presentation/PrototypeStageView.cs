using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Presentation;

public partial class PrototypeStageView : Node3D
{
    private Camera3D _camera = null!;
    private Sprite3D _background = null!;
    private DirectionalLight3D _sun = null!;
    private OmniLight3D _fill = null!;
    private Godot.Environment _environment = null!;
    private MeshInstance3D? _gameplayFloor;
    private readonly List<MeshInstance3D> _laneBoundaries = new();
    private readonly List<StageLayerInstance> _stageLayers = new();
    private readonly List<DestructionBurstInstance> _destructionBursts = new();
    private Sprite3D? _destructionOverlaySprite;
    private readonly Dictionary<StageVisualLayerKind, Node3D> _layerRoots = new();
    private GameSimulation? _simulation;
    private StageMissionData _mission = null!;
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private float _shakeTime;
    private float _shakeStrength;
    private float _cinematicFocusTime;
    private float _cinematicBlend;
    private string _cinematicActorId = "";
    private bool _bossIntroFocusActive;
    private float _bossIntroFocusElapsed;
    private bool _bossIntroSwitchedToPlayer;
    private string _bossIntroPlayerActorId = "";
    private const float BossIntroBossFocusSeconds = 1.15f;
    private Vector3 _lastCinematicFocusPosition;
    private bool _cameraInitialized;
    private bool _cameraSmokeEnabled;
    private bool _cameraSmokeSummaryPrinted;
    private int _cameraSmokeMaxPlayerCount;
    private float _cameraSmokeMaxSize;
    private float _startingCameraCenterX;
    private bool _stageVisualSmokeEnabled;
    private bool _stageVisualSmokeSummaryPrinted;
    private bool _parallaxObserved;
    private bool _lightingTransitionObserved;
    private int _destructionPhase;
    private int _destructionBurstCaptureFrames;
    private int _destructionSettledCaptureFrames;
    private bool _destructionBurstCaptureSaved;
    private bool _destructionSettledCaptureSaved;

    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";
    [Export] public string StageTexturePath { get; set; } = "";

    public override void _Ready()
    {
        ConfigureCaptureViewport();
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        _mission = MvpMissionSelection.CreateSelectedMission();
        _cameraSmokeEnabled =
            OS.GetEnvironment("PROJECT_MANNEQUIN_CAMERA_SMOKE_TEST") == "1";
        _stageVisualSmokeEnabled =
            OS.GetEnvironment("PROJECT_MANNEQUIN_STAGE_VISUAL_SMOKE_TEST") == "1";
        _startingCameraCenterX =
            _mission.StageMinX + _mission.CameraViewportWidth * 0.5f;
        CreateLayerRoots();
        CreateStageBackground();
        CreateStageSurface();
        CreateLighting();
        CreateCamera();
        CreateSplitScreenOverlay();
        PrepareDestructionCaptureOutputs();
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

    private void CreateSplitScreenOverlay()
    {
        if (_simulation is null)
        {
            return;
        }

        var overlay = new SplitScreenOverlay();
        overlay.Configure(_simulation);
        AddChild(overlay);
    }

    public override void _Process(double delta)
    {
        if (_simulation is null || _simulation.Actors.Count == 0)
        {
            return;
        }

        CapturePresentationEvents();
        UpdateDestructionBursts((float)delta);
        CaptureDestructionFrameIfRequested();

        var activePlayers = _simulation.Actors
            .Where(actor => actor.IsPlayerControlled && !actor.IsDead)
            .ToArray();
        if (activePlayers.Length == 0)
        {
            return;
        }

        var director = _simulation.EncounterDirector;
        var framingActors = activePlayers.AsEnumerable();
        if (director?.CurrentEncounter.Kind == StageEncounterKind.Boss
            && director.State is ProjectMannequin.Stage.ArcadeStageState.BossIntro
                or ProjectMannequin.Stage.ArcadeStageState.EncounterActive
                or ProjectMannequin.Stage.ArcadeStageState.AwaitingFormSwap)
        {
            framingActors = framingActors.Concat(_simulation.Actors.Where(actor =>
                actor.IsBoss && !actor.IsDead));
        }

        var framedActors = framingActors.ToArray();
        var deltaSeconds = (float)delta;
        var minX = framedActors.Min(actor => actor.SimPosition.X);
        var maxX = framedActors.Max(actor => actor.SimPosition.X);
        var minZ = framedActors.Min(actor => actor.SimPosition.Z);
        var maxZ = framedActors.Max(actor => actor.SimPosition.Z);
        var maxAirHeight = framedActors.Max(actor => actor.SimPosition.Y);
        var spreadX = maxX - minX;
        var spreadZ = maxZ - minZ;
        var gameplayCenterX = (minX + maxX) * 0.5f;

        if (director is not null)
        {
            gameplayCenterX = director.CameraCenterX;
        }

        UpdateStageLayers(gameplayCenterX);
        UpdateEncounterLighting(director, deltaSeconds);

        // Per-fighter close-up (SF4/DBFZ): after holding on the boss for its intro
        // animation, pan the cinematic focus to the player for theirs. The camera
        // follow-smoothing turns the target swap into a smooth pan between them.
        if (_bossIntroFocusActive)
        {
            _bossIntroFocusElapsed += deltaSeconds;
            if (!_bossIntroSwitchedToPlayer
                && _bossIntroFocusElapsed >= BossIntroBossFocusSeconds
                && !string.IsNullOrEmpty(_bossIntroPlayerActorId))
            {
                _cinematicActorId = _bossIntroPlayerActorId;
                _cinematicFocusTime = 30.0f;
                _bossIntroSwitchedToPlayer = true;
            }
        }

        var cinematicActor = _cinematicFocusTime > 0.0f
            ? _simulation.Actors.FirstOrDefault(actor => actor.ActorId == _cinematicActorId)
            : null;
        if (_cinematicFocusTime > 0.0f)
        {
            _cinematicFocusTime = Mathf.Max(0.0f, _cinematicFocusTime - deltaSeconds);
        }

        if (cinematicActor is not null)
        {
            _lastCinematicFocusPosition = cinematicActor.SimPosition;
            _cinematicBlend = Mathf.MoveToward(
                _cinematicBlend,
                1.0f,
                deltaSeconds / 0.10f);
        }
        else
        {
            _cinematicBlend = Mathf.MoveToward(
                _cinematicBlend,
                0.0f,
                deltaSeconds / _mission.CameraCinematicRecoverySeconds);
        }

        var centerX = Mathf.Lerp(
            gameplayCenterX,
            _lastCinematicFocusPosition.X,
            _cinematicBlend);
        // Keep the camera vertically stable as fighters change lane depth (W/S).
        // A belt-scroll camera never pans vertically with lane movement; frame a
        // fixed lane center and only shift Z for cinematic focus (super/boss).
        var laneCenterZ = (_mission.LaneMinZ + _mission.LaneMaxZ) * 0.5f;
        var centerZ = Mathf.Lerp(
            laneCenterZ,
            _lastCinematicFocusPosition.Z,
            _cinematicBlend);
        var focusAirHeight = Mathf.Lerp(
            maxAirHeight,
            _lastCinematicFocusPosition.Y,
            _cinematicBlend);
        var airFraming = Mathf.Clamp(focusAirHeight, 0.0f, 3.2f);
        var gameplaySize = ResolveGameplayCameraSize(
            spreadX,
            spreadZ,
            Mathf.Clamp(maxAirHeight, 0.0f, 3.2f),
            director);
        var targetSize = Mathf.Lerp(
            gameplaySize,
            _mission.CameraCinematicSize,
            _cinematicBlend);
        var verticalFollow = director?.CameraCenterY ?? 0.0f;
        var targetPosition = new Vector3(
            centerX,
            6.5f + spreadZ * 0.08f + airFraming * 0.28f + verticalFollow * 0.9f,
            11.5f + Mathf.Max(0.0f, targetSize - _mission.CameraBaseSize) * 0.28f);
        if (_shakeTime > 0.0f)
        {
            _shakeTime = Mathf.Max(0.0f, _shakeTime - deltaSeconds);
            var phase = Time.GetTicksMsec() * 0.08f;
            var shakeScale = activePlayers.Length switch
            {
                >= 4 => _mission.FourPlayerShakeScale,
                3 => _mission.ThreePlayerShakeScale,
                _ => 1.0f,
            };
            targetPosition += new Vector3(
                Mathf.Sin(phase) * _shakeStrength * shakeScale,
                Mathf.Cos(phase * 1.37f) * _shakeStrength * 0.55f * shakeScale,
                0.0f);
        }

        var snapCamera = !_cameraInitialized;
        _cameraInitialized = true;
        var followWeight = 1.0f
            - Mathf.Exp(-_mission.CameraFollowSharpness * deltaSeconds);
        _camera.GlobalPosition = snapCamera
            ? targetPosition
            : _camera.GlobalPosition.Lerp(targetPosition, followWeight);
        var zoomWeight = 1.0f
            - Mathf.Exp(-_mission.CameraZoomSharpness * deltaSeconds);
        _camera.Size = snapCamera
            ? targetSize
            : Mathf.Lerp(_camera.Size, targetSize, zoomWeight);
        _camera.LookAt(new Vector3(centerX, 1.15f + airFraming * 1.08f + verticalFollow, centerZ), Vector3.Up);
        CaptureCameraSmoke(activePlayers.Length, targetSize);
        CaptureStageVisualSmoke(director);
    }

    private void CreateLayerRoots()
    {
        foreach (var layerKind in System.Enum.GetValues<StageVisualLayerKind>())
        {
            var root = new Node3D
            {
                Name = $"{layerKind}Layer",
            };
            AddChild(root);
            _layerRoots[layerKind] = root;
        }
    }

    private void UpdateStageLayers(float cameraCenterX)
    {
        UpdateStageSurfaceBounds();
        var cameraTravel = cameraCenterX - _startingCameraCenterX;
        foreach (var layer in _stageLayers)
        {
            var targetX = layer.BasePosition.X
                + cameraTravel * (1.0f - layer.Data.ParallaxFactorX);
            layer.Sprite.Position = new Vector3(
                targetX,
                layer.BasePosition.Y,
                layer.BasePosition.Z);
            _parallaxObserved |=
                !Mathf.IsEqualApprox(layer.Data.ParallaxFactorX, 1.0f)
                && Mathf.Abs(targetX - layer.BasePosition.X) > 0.1f;
        }
    }

    private void UpdateStageSurfaceBounds()
    {
        var stageLength = Mathf.Max(
            1.0f,
            _mission.StageMaxX - _mission.StageMinX);
        var stageCenterX =
            (_mission.StageMinX + _mission.StageMaxX) * 0.5f;
        if (_gameplayFloor?.Mesh is PlaneMesh floorMesh)
        {
            floorMesh.Size = new Vector2(
                stageLength + 30.0f,
                floorMesh.Size.Y);
            _gameplayFloor.Position = new Vector3(
                stageCenterX,
                _gameplayFloor.Position.Y,
                _gameplayFloor.Position.Z);
        }

        foreach (var laneBoundary in _laneBoundaries)
        {
            if (laneBoundary.Mesh is BoxMesh boundaryMesh)
            {
                boundaryMesh.Size = new Vector3(
                    stageLength + 28.0f,
                    boundaryMesh.Size.Y,
                    boundaryMesh.Size.Z);
            }

            laneBoundary.Position = new Vector3(
                stageCenterX,
                laneBoundary.Position.Y,
                laneBoundary.Position.Z);
        }
    }

    private void UpdateEncounterLighting(
        ProjectMannequin.Stage.ArcadeEncounterDirector? director,
        float deltaSeconds)
    {
        var encounter = director?.CurrentEncounter;
        var targetColor = encounter is null
            ? Colors.White
            : new Color(
                encounter.LightColorR,
                encounter.LightColorG,
                encounter.LightColorB);
        var targetMultiplier = encounter?.LightEnergyMultiplier ?? 1.0f;
        var transitionSeconds = encounter?.LightTransitionSeconds ?? 0.45f;
        var weight = 1.0f
            - Mathf.Exp(-deltaSeconds / Mathf.Max(0.01f, transitionSeconds));
        _sun.LightColor = _sun.LightColor.Lerp(targetColor, weight);
        _fill.LightColor = _fill.LightColor.Lerp(targetColor, weight);
        _sun.LightEnergy = Mathf.Lerp(
            _sun.LightEnergy,
            2.0f * targetMultiplier,
            weight);
        _fill.LightEnergy = Mathf.Lerp(
            _fill.LightEnergy,
            1.1f * targetMultiplier,
            weight);
        _environment.AmbientLightColor =
            _environment.AmbientLightColor.Lerp(targetColor, weight * 0.45f);
        _lightingTransitionObserved |=
            !Mathf.IsEqualApprox(targetMultiplier, 1.0f)
            || Mathf.Abs(targetColor.R - 1.0f) > 0.01f
            || Mathf.Abs(targetColor.G - 1.0f) > 0.01f
            || Mathf.Abs(targetColor.B - 1.0f) > 0.01f;
    }

    private void CaptureStageVisualSmoke(
        ProjectMannequin.Stage.ArcadeEncounterDirector? director)
    {
        if (!_stageVisualSmokeEnabled
            || _stageVisualSmokeSummaryPrinted
            || _simulation is null
            || director is null
            || _simulation.CurrentTick < 30
            || director.StageProgress < 0.55f)
        {
            return;
        }

        _stageVisualSmokeSummaryPrinted = true;
        var activeLayerKinds = _layerRoots.Keys.Count;
        var sharpSampling = _stageLayers.All(layer =>
            layer.Sprite.TextureFilter
            is not BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps
                and not BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps);
        var passed = activeLayerKinds == 4
            && _stageLayers.Count >= 3
            && _gameplayFloor is not null
            && _parallaxObserved
            && _lightingTransitionObserved
            && sharpSampling;
        GD.Print(
            $"[StageVisualSmoke] SUMMARY passed={passed} "
            + $"roots={activeLayerKinds} panels={_stageLayers.Count} "
            + $"floor={_gameplayFloor is not null} parallax={_parallaxObserved} "
            + $"lighting={_lightingTransitionObserved} sharp={sharpSampling}");
        var capturePath =
            OS.GetEnvironment("PROJECT_MANNEQUIN_STAGE_CAPTURE_PATH");
        if (!string.IsNullOrWhiteSpace(capturePath)
            && DisplayServer.GetName() != "headless")
        {
            var captureError = GetViewport()
                .GetTexture()
                .GetImage()
                .SavePng(capturePath);
            GD.Print(
                $"[StageVisualSmoke] CAPTURE path={capturePath} "
                + $"result={captureError}");
        }

        if (!passed)
        {
            GD.PushError("[StageVisualSmoke] Layered stage presentation assertions failed.");
        }
    }

    private float ResolveGameplayCameraSize(
        float spreadX,
        float spreadZ,
        float airHeight,
        ProjectMannequin.Stage.ArcadeEncounterDirector? director)
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var aspectRatio = viewportSize.Y > 0.0f
            ? viewportSize.X / viewportSize.Y
            : 16.0f / 9.0f;
        var horizontalSize = (
            spreadX + _mission.CameraHorizontalPadding * 2.0f)
            / Mathf.Max(1.0f, aspectRatio);
        var depthSize = _mission.CameraBaseSize
            + Mathf.Max(0.0f, spreadZ - 2.0f) * 0.42f;
        var airSize = _mission.CameraBaseSize + airHeight * 0.65f;
        var encounterSize = director?.IsArenaLocked == true
            ? director.CurrentEncounter.CameraOrthographicSize
            : 0.0f;
        var requiredSize = Mathf.Max(
            Mathf.Max(_mission.CameraBaseSize, horizontalSize),
            Mathf.Max(depthSize, Mathf.Max(airSize, encounterSize)));
        return Mathf.Clamp(
            requiredSize,
            _mission.CameraBaseSize,
            _mission.CameraMaxSize);
    }

    private void CaptureCameraSmoke(int activePlayerCount, float targetSize)
    {
        if (!_cameraSmokeEnabled || _cameraSmokeSummaryPrinted || _simulation is null)
        {
            return;
        }

        _cameraSmokeMaxPlayerCount = Mathf.Max(
            _cameraSmokeMaxPlayerCount,
            activePlayerCount);
        _cameraSmokeMaxSize = Mathf.Max(_cameraSmokeMaxSize, targetSize);
        if (_simulation.CurrentTick < 90)
        {
            return;
        }

        _cameraSmokeSummaryPrinted = true;
        var passed = _cameraSmokeMaxPlayerCount == 2
            && _cameraSmokeMaxSize > _mission.CameraBaseSize + 0.1f
            && _camera.Size >= _mission.CameraBaseSize
            && _camera.Size <= _mission.CameraMaxSize;
        GD.Print(
            $"[CameraSmoke] PRESENTATION passed={passed} "
            + $"players={_cameraSmokeMaxPlayerCount} "
            + $"maxSize={_cameraSmokeMaxSize:0.00} currentSize={_camera.Size:0.00}");
        if (!passed)
        {
            GD.PushError("[CameraSmoke] Multiplayer camera composition assertions failed.");
        }
    }

    private void CapturePresentationEvents()
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
                case CombatPresentationEventType.HitConnected:
                case CombatPresentationEventType.LauncherHit:
                case CombatPresentationEventType.CounterHit:
                case CombatPresentationEventType.PunishCounter:
                    var damage = ParseDamage(presentationEvent.Payload);
                    _shakeTime = Mathf.Max(_shakeTime, 0.08f + damage * 0.0025f);
                    _shakeStrength = Mathf.Clamp(0.025f + damage * 0.002f, 0.04f, 0.18f);
                    break;
                case CombatPresentationEventType.Parried:
                    _shakeTime = 0.16f;
                    _shakeStrength = 0.08f;
                    break;
                case CombatPresentationEventType.SuperStarted:
                    BeginCinematicFocus(presentationEvent.SourceActorId, 0.72f, 0.24f);
                    break;
                case CombatPresentationEventType.BossIntroStarted:
                    // Phase 1: override the framing to push in on the boss. The
                    // BossIntro framing already folds in the player (see _Process)
                    // so both fighters stay in shot; the focus is held (shake-free)
                    // until the READY beat releases it.
                    BeginBossIntroFocus(presentationEvent.SourceActorId);
                    break;
                case CombatPresentationEventType.BossIntroReady:
                    // Phase 2: release the override so the camera interpolates back
                    // to the default gameplay bounds/zoom while the HUD reveals.
                    ReleaseBossIntroFocus();
                    break;
                case CombatPresentationEventType.BossIntroFight:
                    // Phase 4: sharp shake impulse punctuating the FIGHT release.
                    KickCameraShake(0.42f, 0.17f);
                    break;
                case CombatPresentationEventType.EliteIntroStarted:
                    BeginCinematicFocus(presentationEvent.SourceActorId, 0.72f, 0.0f);
                    break;
                case CombatPresentationEventType.EliteIntroFight:
                    KickCameraShake(0.28f, 0.10f);
                    break;
                case CombatPresentationEventType.BossPhaseChanged
                    when presentationEvent.Payload != "BossActivated":
                    _shakeTime = 0.42f;
                    _shakeStrength = 0.13f;
                    BeginCinematicFocus(presentationEvent.SourceActorId, 0.62f, 0.13f);
                    break;
                case CombatPresentationEventType.WallBreakStarted:
                    _shakeTime = 0.7f;
                    _shakeStrength = 0.25f;
                    _destructionPhase++;
                    ApplyDestroyedStageVariant(_destructionPhase);
                    break;
            }
        }
    }

    private void BeginCinematicFocus(string actorId, float duration, float shakeStrength)
    {
        _cinematicActorId = actorId;
        _cinematicFocusTime = duration;
        _shakeTime = Mathf.Max(_shakeTime, duration * 0.55f);
        _shakeStrength = Mathf.Max(_shakeStrength, shakeStrength);
    }

    /// <summary>
    /// Camera-controller hook for the boss-intro sequence (Phase 1). Runs a
    /// two-beat per-fighter close-up: it first pushes in on the boss for its intro
    /// animation, then pans to the player for theirs. The hold stays open until
    /// <see cref="ReleaseBossIntroFocus"/> is called (the READY beat).
    /// </summary>
    public void BeginBossIntroFocus(string bossActorId)
    {
        // First beat: push in on the boss during its intro animation.
        _cinematicActorId = bossActorId;
        _cinematicFocusTime = 30.0f;
        // Arm the second beat: pan to the player for their intro after a hold.
        _bossIntroFocusActive = true;
        _bossIntroFocusElapsed = 0.0f;
        _bossIntroSwitchedToPlayer = false;
        _bossIntroPlayerActorId = _simulation?.Actors
            .FirstOrDefault(actor => actor.IsPlayerControlled && !actor.IsDead)?.ActorId ?? "";
    }

    /// <summary>
    /// Camera-controller hook for the boss-intro sequence (Phase 2). Ends the
    /// cinematic override so the camera smoothly interpolates back to the default
    /// gameplay bounds/zoom through the normal recovery blend.
    /// </summary>
    public void ReleaseBossIntroFocus()
    {
        _cinematicFocusTime = 0.0f;
        _bossIntroFocusActive = false;
    }

    /// <summary>
    /// Camera-controller hook: fire a one-shot shake impulse (used for the Phase 4
    /// FIGHT release, but reusable by any presentation system).
    /// </summary>
    public void KickCameraShake(float duration, float strength)
    {
        _shakeTime = Mathf.Max(_shakeTime, duration);
        _shakeStrength = Mathf.Max(_shakeStrength, strength);
    }

    private static int ParseDamage(string payload)
    {
        var separatorIndex = payload.LastIndexOf('|');
        return separatorIndex >= 0
               && separatorIndex < payload.Length - 1
               && int.TryParse(payload[(separatorIndex + 1)..], out var damage)
            ? damage
            : 0;
    }

    private void CreateLighting()
    {
        var farColor = new Color(
            _mission.FarColorR,
            _mission.FarColorG,
            _mission.FarColorB);
        _environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = farColor,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Colors.White,
            AmbientLightEnergy = 0.62f,
        };
        var worldEnvironment = new WorldEnvironment
        {
            Name = "StageFarLayer",
            Environment = _environment,
        };
        _layerRoots[StageVisualLayerKind.Far].AddChild(worldEnvironment);

        _sun = new DirectionalLight3D
        {
            Name = "PrototypeSun",
            RotationDegrees = new Vector3(-48.0f, -30.0f, 0.0f),
            LightEnergy = 2.0f,
            ShadowEnabled = true,
        };
        AddChild(_sun);

        _fill = new OmniLight3D
        {
            Name = "PrototypeFill",
            Position = new Vector3(3.0f, 5.0f, 5.0f),
            LightEnergy = 1.1f,
            OmniRange = 18.0f,
        };
        AddChild(_fill);
    }

    private void CreateStageSurface()
    {
        var stageLength = Mathf.Max(1.0f, _mission.StageMaxX - _mission.StageMinX);
        // Extend the floor's far edge well behind the backdrop billboard (Z=-6)
        // so the buildings occlude the plane's hard far edge. The road then
        // reads as meeting the building bases with no gap at the horizon.
        var floorMinZ = _mission.LaneMinZ - 12.0f;
        var floorMaxZ = _mission.LaneMaxZ + 9.0f;
        var floorDepth = Mathf.Max(1.0f, floorMaxZ - floorMinZ);
        var floorWidth = stageLength + 30.0f;
        var floorMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(
                _mission.FloorColorR,
                _mission.FloorColorG,
                _mission.FloorColorB),
            Roughness = 0.95f,
            Metallic = 0.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };

        // Paint the road portion of the stage art onto the walkable ground so it
        // recedes with depth and characters stay planted at every lane position
        // (a flat backdrop billboard cannot follow the fighters into the lane).
        var floorTexturePath = string.IsNullOrWhiteSpace(_mission.FloorTexturePath)
            ? _mission.StageTexturePath
            : _mission.FloorTexturePath;
        if (ResourceLoader.Exists(floorTexturePath))
        {
            try
            {
                var source = GD.Load<Texture2D>(floorTexturePath);
                var sourceImage = source.GetImage();
                if (sourceImage is not null && sourceImage.GetHeight() > 0)
            {
                if (sourceImage.IsCompressed())
                {
                    sourceImage.Decompress();
                }
                sourceImage.Convert(Image.Format.Rgba8);
                var roadTop = Mathf.Clamp(
                    Mathf.RoundToInt(
                        sourceImage.GetHeight() * _mission.FloorTextureTopFraction),
                    0,
                    sourceImage.GetHeight() - 1);
                var roadHeight = sourceImage.GetHeight() - roadTop;
                if (roadHeight > 8)
                {
                    var roadWidth = sourceImage.GetWidth();
                    var roadImage = sourceImage.GetRegion(
                        new Rect2I(0, roadTop, roadWidth, roadHeight));

                    // Mirror the road into a double-wide tile so horizontal
                    // repeats share matching edges and scroll seamlessly beneath
                    // the fighters instead of showing a hard seam every tile.
                    var mirroredRoad = (Image)roadImage.Duplicate();
                    mirroredRoad.FlipX();
                    var seamlessTile = Image.CreateEmpty(
                        roadWidth * 2, roadHeight, false, Image.Format.Rgba8);
                    seamlessTile.BlitRect(
                        roadImage,
                        new Rect2I(0, 0, roadWidth, roadHeight),
                        new Vector2I(0, 0));
                    seamlessTile.BlitRect(
                        mirroredRoad,
                        new Rect2I(0, 0, roadWidth, roadHeight),
                        new Vector2I(roadWidth, 0));

                    floorMaterial.AlbedoTexture =
                        ImageTexture.CreateFromImage(seamlessTile);
                    floorMaterial.AlbedoColor = Colors.White;
                    floorMaterial.TextureFilter =
                        BaseMaterial3D.TextureFilterEnum.Linear;
                    var tileWidth = Mathf.Max(1.0f, _mission.FloorTextureTileWidth);
                    floorMaterial.Uv1Scale = new Vector3(
                        Mathf.Max(1.0f, floorWidth / (tileWidth * 2.0f)),
                        1.0f,
                        1.0f);
                }
            }
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Failed to load floor texture '{floorTexturePath}': {ex.Message}");
            }
        }

        _gameplayFloor = new MeshInstance3D
        {
            Name = "GameplayFloor",
            Mesh = new PlaneMesh
            {
                Size = new Vector2(floorWidth, floorDepth),
                Material = floorMaterial,
            },
            Position = new Vector3(
                (_mission.StageMinX + _mission.StageMaxX) * 0.5f,
                -0.035f,
                (floorMinZ + floorMaxZ) * 0.5f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _layerRoots[StageVisualLayerKind.Gameplay].AddChild(_gameplayFloor);
    }

    private void CreateStageBackground()
    {
        if (_mission.BackgroundPanels.Count > 0)
        {
            CreateSegmentedStageBackground();
            return;
        }

        var texturePath = string.IsNullOrWhiteSpace(StageTexturePath)
            ? _mission.StageTexturePath
            : StageTexturePath;
        if (!ResourceLoader.Exists(texturePath))
        {
            GD.PushWarning($"Stage texture not found: {texturePath}");
            return;
        }

        try
        {
            var sourceTexture = GD.Load<Texture2D>(texturePath);
        Texture2D stageTexture = sourceTexture;
        var croppedHeight = sourceTexture.GetHeight()
            - _mission.StageTextureCropTopPixels
            - _mission.StageTextureCropBottomPixels;
        if ((_mission.StageTextureCropTopPixels > 0
                || _mission.StageTextureCropBottomPixels > 0)
            && croppedHeight > 0)
        {
            stageTexture = new AtlasTexture
            {
                Atlas = sourceTexture,
                Region = new Rect2(
                    0,
                    _mission.StageTextureCropTopPixels,
                    sourceTexture.GetWidth(),
                    croppedHeight),
            };
        }

        _background = new Sprite3D
        {
            Name = $"{_mission.WorldId}Background",
            Texture = stageTexture,
            Position = new Vector3(
                (_mission.StageMinX + _mission.StageMaxX) * 0.5f,
                _mission.StageTexturePositionY,
                -6.0f),
            PixelSize = _mission.StageTexturePixelSize,
            Scale = new Vector3(
                _mission.StageTextureScaleX,
                _mission.StageTextureScaleY,
                1.0f),
            Shaded = false,
            DoubleSided = true,
            AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
        };
        _layerRoots[StageVisualLayerKind.Midground].AddChild(_background);
        _stageLayers.Add(new StageLayerInstance(
            _background,
            _background.Position,
            new StageBackgroundPanelData
            {
                TexturePath = texturePath,
                Layer = StageVisualLayerKind.Midground,
            }));
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Failed to load stage background texture '{texturePath}': {ex.Message}");
        }
    }

    private void CreateSegmentedStageBackground()
    {
        for (var panelIndex = 0;
             panelIndex < _mission.BackgroundPanels.Count;
             panelIndex++)
        {
            var panel = _mission.BackgroundPanels[panelIndex];
            if (!ResourceLoader.Exists(panel.TexturePath))
            {
                GD.PushWarning($"Stage panel texture not found: {panel.TexturePath}");
                continue;
            }

            try
            {
                var sourceTexture = GD.Load<Texture2D>(panel.TexturePath);
            Texture2D texture = sourceTexture;
            var cropTop = Mathf.RoundToInt(
                sourceTexture.GetHeight() * panel.CropTopFraction);
            var cropBottom = Mathf.RoundToInt(
                sourceTexture.GetHeight() * panel.CropBottomFraction);
            var cropHeight = sourceTexture.GetHeight() - cropTop - cropBottom;
            if ((cropTop > 0 || cropBottom > 0) && cropHeight > 0)
            {
                texture = new AtlasTexture
                {
                    Atlas = sourceTexture,
                    Region = new Rect2(
                        0,
                        cropTop,
                        sourceTexture.GetWidth(),
                        cropHeight),
                };
            }

            var worldWidth = Mathf.Max(0.1f, panel.MaxX - panel.MinX);
            var sourceWorldWidth =
                texture.GetWidth() * _mission.StageTexturePixelSize;
            var uniformScale =
                worldWidth / Mathf.Max(0.1f, sourceWorldWidth);
            var basePosition = new Vector3(
                (panel.MinX + panel.MaxX) * 0.5f,
                panel.PositionY,
                panel.PositionZ);
            var backgroundPanel = new Sprite3D
            {
                Name =
                    $"{panel.Layer}{_mission.WorldId}Panel{panelIndex + 1:00}",
                Texture = texture,
                Position = basePosition,
                PixelSize = _mission.StageTexturePixelSize,
                Scale = new Vector3(
                    uniformScale,
                    uniformScale * Mathf.Max(0.1f, panel.ScaleYMultiplier),
                    1.0f),
                FlipH = panel.FlipH,
                TextureFilter = panel.Sampling == StageTextureSampling.Nearest
                    ? BaseMaterial3D.TextureFilterEnum.Nearest
                    : BaseMaterial3D.TextureFilterEnum.Linear,
                Modulate = new Color(1.0f, 1.0f, 1.0f, panel.Opacity),
                Shaded = false,
                DoubleSided = true,
                AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
                Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            };
            _layerRoots[panel.Layer].AddChild(backgroundPanel);
            _stageLayers.Add(new StageLayerInstance(
                backgroundPanel,
                basePosition,
                panel));
            _background ??= backgroundPanel;
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Failed to load stage panel texture '{panel.TexturePath}': {ex.Message}");
            }
        }
    }

    private void ApplyDestroyedStageVariant(int destructionPhase)
    {
        StageBackgroundPanelData? burstPanel = null;
        foreach (var layer in _stageLayers)
        {
            var texturePath = layer.Data.DestructionTexturePaths.Count > 0
                ? layer.Data.DestructionTexturePaths[Mathf.Clamp(
                    destructionPhase - 1,
                    0,
                    layer.Data.DestructionTexturePaths.Count - 1)]
                : layer.Data.DestroyedTexturePath;
            if (!string.IsNullOrWhiteSpace(texturePath)
                && ResourceLoader.Exists(texturePath))
            {
                try
                {
                    layer.Sprite.Texture = GD.Load<Texture2D>(texturePath);
                }
                catch (Exception ex)
                {
                    GD.PushWarning(
                        $"Failed to load destroyed texture '{texturePath}': {ex.Message}");
                }

                if (layer.Data.DestructionOverlayPixelSize > 0.0f)
                {
                    ApplyDestructionOverlay(layer.Data, texturePath);
                }
            }
            else
            {
                layer.Sprite.Modulate = new Color(
                    layer.Data.DestroyedTintR,
                    layer.Data.DestroyedTintG,
                    layer.Data.DestroyedTintB,
                    layer.Data.Opacity);
            }

            if (burstPanel is null
                && layer.Data.DestructionBurstTexturePaths.Count > 0)
            {
                burstPanel = layer.Data;
            }
        }

        if (burstPanel is not null)
        {
            SpawnDestructionBurst(burstPanel, destructionPhase);
        }
    }

    private void SpawnDestructionBurst(
        StageBackgroundPanelData panel,
        int destructionPhase)
    {
        var index = Mathf.Clamp(
            destructionPhase - 1,
            0,
            panel.DestructionBurstTexturePaths.Count - 1);
        var texturePath = panel.DestructionBurstTexturePaths[index];
        if (!ResourceLoader.Exists(texturePath))
        {
            return;
        }

        var anchorX = panel.DestructionBurstAnchorXs.Count > index
            ? panel.DestructionBurstAnchorXs[index]
            : 0.5f;
        var centerX = ResolveDestructionCenterX();
        var sprite = new Sprite3D
        {
            Name = $"DestructionBurst{destructionPhase}",
            Texture = GD.Load<Texture2D>(texturePath),
            PixelSize = panel.DestructionBurstPixelSize,
            Position = new Vector3(
                centerX + Mathf.Lerp(-3.6f, 3.6f, anchorX),
                panel.DestructionBurstPositionY,
                panel.DestructionBurstPositionZ),
            Scale = Vector3.One * 0.72f,
            Shaded = false,
            DoubleSided = true,
            AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
        };
        _layerRoots[StageVisualLayerKind.Midground].AddChild(sprite);
        _destructionBursts.Add(new DestructionBurstInstance(sprite));
        _destructionBurstCaptureFrames = 0;
        _destructionSettledCaptureFrames = 0;
        _destructionBurstCaptureSaved = false;
        _destructionSettledCaptureSaved = false;
    }

    private void ApplyDestructionOverlay(
        StageBackgroundPanelData panel,
        string texturePath)
    {
        if (_destructionOverlaySprite is null)
        {
            _destructionOverlaySprite = new Sprite3D
            {
                Name = "ReliquaryDestructionOverlay",
                PixelSize = panel.DestructionOverlayPixelSize,
                Shaded = false,
                DoubleSided = true,
                AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
                Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            };
            _layerRoots[StageVisualLayerKind.Midground].AddChild(
                _destructionOverlaySprite);
        }

        _destructionOverlaySprite.Texture = GD.Load<Texture2D>(texturePath);
        _destructionOverlaySprite.PixelSize = panel.DestructionOverlayPixelSize;
        _destructionOverlaySprite.Position = new Vector3(
            ResolveDestructionCenterX(),
            panel.DestructionOverlayPositionY,
            panel.DestructionOverlayPositionZ);
    }

    private float ResolveDestructionCenterX()
    {
        return _simulation?.EncounterDirector?.CameraCenterX
            ?? (_mission.StageMinX + _mission.StageMaxX) * 0.5f;
    }

    private void UpdateDestructionBursts(float deltaSeconds)
    {
        for (var index = _destructionBursts.Count - 1; index >= 0; index--)
        {
            var burst = _destructionBursts[index];
            burst.ElapsedSeconds += deltaSeconds;
            var progress = Mathf.Clamp(burst.ElapsedSeconds / 0.72f, 0.0f, 1.0f);
            var eased = 1.0f - Mathf.Pow(1.0f - progress, 3.0f);
            burst.Sprite.Scale = Vector3.One * Mathf.Lerp(0.72f, 1.08f, eased);
            burst.Sprite.Modulate = new Color(
                1.0f,
                1.0f,
                1.0f,
                progress < 0.45f
                    ? 1.0f
                    : Mathf.Clamp((1.0f - progress) / 0.55f, 0.0f, 1.0f));
            burst.Sprite.Position += Vector3.Up * (deltaSeconds * 0.18f);
            if (progress < 1.0f)
            {
                continue;
            }

            burst.Sprite.QueueFree();
            _destructionBursts.RemoveAt(index);
        }
    }

    private void CaptureDestructionFrameIfRequested()
    {
        if (_destructionPhase is < 1 or > 2)
        {
            return;
        }

        var bossPhaseNumber = _destructionPhase + 1;
        var burstPath = OS.GetEnvironment(
            $"PROJECT_MANNEQUIN_RELIQUARY_PHASE{bossPhaseNumber}_BURST_CAPTURE");
        var settledPath = OS.GetEnvironment(
            $"PROJECT_MANNEQUIN_RELIQUARY_PHASE{bossPhaseNumber}_SETTLED_CAPTURE");
        if (_destructionBursts.Count > 0)
        {
            _destructionBurstCaptureFrames++;
            if (!_destructionBurstCaptureSaved
                && !string.IsNullOrWhiteSpace(burstPath)
                && _destructionBurstCaptureFrames >= 10)
            {
                _destructionBurstCaptureSaved = SaveDestructionCapture(
                    burstPath,
                    $"phase {bossPhaseNumber} burst");
            }
            return;
        }

        _destructionSettledCaptureFrames++;
        if (!_destructionSettledCaptureSaved
            && !string.IsNullOrWhiteSpace(settledPath)
            && _destructionSettledCaptureFrames >= 120)
        {
            _destructionSettledCaptureSaved = SaveDestructionCapture(
                settledPath,
                $"phase {bossPhaseNumber} settled");
        }
    }

    private bool SaveDestructionCapture(string path, string label)
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
                $"[ReliquaryDestruction] Could not save {label} capture '{path}' ({error}).");
            return false;
        }

        GD.Print($"[ReliquaryDestruction] Saved {label} capture: {path}");
        return true;
    }

    private static void PrepareDestructionCaptureOutputs()
    {
        foreach (var phase in new[] { 2, 3 })
        {
            foreach (var state in new[] { "BURST", "SETTLED" })
            {
                var path = OS.GetEnvironment(
                    $"PROJECT_MANNEQUIN_RELIQUARY_PHASE{phase}_{state}_CAPTURE");
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    private void CreateCamera()
    {
        var startingCenterX = _mission.StageMinX + _mission.CameraViewportWidth * 0.5f;
        _camera = new Camera3D
        {
            Name = "PrototypeCamera",
            Current = true,
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = _mission.CameraBaseSize,
            Position = new Vector3(startingCenterX, 6.5f, 13.5f),
        };
        AddChild(_camera);
        _camera.LookAt(new Vector3(startingCenterX, 1.0f, 0.0f), Vector3.Up);
    }

    private sealed record StageLayerInstance(
        Sprite3D Sprite,
        Vector3 BasePosition,
        StageBackgroundPanelData Data);

    private sealed class DestructionBurstInstance
    {
        public DestructionBurstInstance(Sprite3D sprite)
        {
            Sprite = sprite;
        }

        public Sprite3D Sprite { get; }
        public float ElapsedSeconds { get; set; }
    }
}
