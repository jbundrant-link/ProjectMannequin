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
    private readonly List<FullFramePlateInstance> _fullFramePlates = new();
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

    // Clearance BEHIND the deepest standable lane position before haze starts.
    // Anchored to the lane, not to a world constant, so a stage with different
    // framing cannot accidentally start hazing the fighters.
    public const float FarHazeLaneClearance = 1.5f;

    // Depth over which haze ramps from none to full. Reaches full strength past
    // the backdrop plane, so the backdrop takes the whole effect and the
    // midground takes a partial amount - the ordering aerial perspective wants.
    public const float FarHazeSpan = 7.0f;

    // Well short of 1.0: the backdrop has to recede, not disappear.
    private const float FarHazeDensity = 0.55f;
    private const float FarHazeEnergy = 1.0f;
    private const float FarHazeCurve = 1.0f;
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
    private int _boundedArenaStableRenderFrames;
    private string _stagePlateCapturePath = "";
    private int _stagePlateStableFrames;
    private bool _stagePlateCaptureSaved;
    private int _forcedFullFramePlateIndex = -1;
    private bool _fullFrameCoverageValid;

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
        _stagePlateCapturePath =
            OS.GetEnvironment("PROJECT_MANNEQUIN_STAGE_PLATE_CAPTURE_PATH");
        if (int.TryParse(
                OS.GetEnvironment("PROJECT_MANNEQUIN_FULL_FRAME_PLATE_INDEX"),
                out var forcedPlateIndex))
        {
            _forcedFullFramePlateIndex = forcedPlateIndex;
        }
        _startingCameraCenterX =
            _mission.StageMinX + _mission.CameraViewportWidth * 0.5f;
        CreateLayerRoots();
        CreateCamera();
        CreateStageBackground();
        CreateStageSurface();
        CreateLighting();
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
        var isCompositeTraversal =
            _mission.PresentationMode == StagePresentationMode.CompositeTraversal;
        var isBoundedArena =
            _mission.PresentationMode == StagePresentationMode.BoundedArena
            || (_mission.PresentationMode == StagePresentationMode.FullFramePlates
                && _mission.ArenaPresentation is not null);
        var isFullFrameTraversal =
            _mission.PresentationMode == StagePresentationMode.FullFramePlates
            && _mission.ArenaPresentation is null;
        var usesTraversalCamera = isCompositeTraversal || isFullFrameTraversal;
        // A full-frame plate is a screen-filling billboard, so the painting can
        // never follow a fighter into the lane. When the stage declares where
        // its floor is painted, the resting look height is solved so world
        // y = 0 lands on that floor instead of hovering above it.
        var restingCamera =
            ProjectMannequin.Stage.StageGroundProjection
                .ResolveRestingCameraProfile(_mission);
        var baseCameraHeight = restingCamera.Height;
        var baseCameraDepth = restingCamera.Depth;
        var baseLookHeight = restingCamera.LookHeight;
        var targetPosition = new Vector3(
            centerX,
            baseCameraHeight
                + (isBoundedArena
                    ? 0.0f
                    : spreadZ * (usesTraversalCamera ? 0.05f : 0.08f)
                        + airFraming * 0.28f
                        + verticalFollow * 0.9f),
            baseCameraDepth
                + (isBoundedArena
                    ? 0.0f
                    : Mathf.Max(0.0f, targetSize - _mission.CameraBaseSize)
                        * 0.28f));
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
            // Applied at the point of use rather than at the point the shake
            // is requested, so an intensity of zero is exactly zero regardless
            // of which system asked for the shake or how strong it asked for.
            var shakeIntensity =
                ProjectMannequin.Settings.SettingsStore.Current.ShakeIntensity;
            targetPosition += new Vector3(
                Mathf.Sin(phase) * _shakeStrength * shakeScale * shakeIntensity,
                Mathf.Cos(phase * 1.37f) * _shakeStrength * 0.55f * shakeScale
                    * shakeIntensity,
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
        _camera.LookAt(new Vector3(
            centerX,
            baseLookHeight
                + (isBoundedArena
                    ? 0.0f
                    : airFraming * 1.08f + verticalFollow),
            isBoundedArena ? 0.0f : centerZ), Vector3.Up);
        UpdateFullFramePlates(gameplayCenterX);
        CaptureCameraSmoke(activePlayers.Length, targetSize);
        CaptureStageVisualSmoke(director);
        CaptureStagePlateIfRequested(director);
    }

    private void CaptureStagePlateIfRequested(
        ProjectMannequin.Stage.ArcadeEncounterDirector? director)
    {
        if (string.IsNullOrWhiteSpace(_stagePlateCapturePath)
            || _stagePlateCaptureSaved)
        {
            return;
        }

        HideNonStagePresentation();
        var ready = _mission.PresentationMode != StagePresentationMode.BoundedArena
            || director?.State
                == ProjectMannequin.Stage.ArcadeStageState.EncounterActive;
        _stagePlateStableFrames = ready ? _stagePlateStableFrames + 1 : 0;
        if (_stagePlateStableFrames < 3)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_stagePlateCapturePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var error = GetViewport()
            .GetTexture()
            .GetImage()
            .SavePng(_stagePlateCapturePath);
        _stagePlateCaptureSaved = error == Error.Ok;
        GD.Print(
            $"[StagePlate] CAPTURE path={_stagePlateCapturePath} "
            + $"result={error}");
        if (!_stagePlateCaptureSaved)
        {
            GD.PushError("[StagePlate] Could not save clean stage plate.");
        }
    }

    private void HideNonStagePresentation()
    {
        var root = GetParent();
        foreach (var nodeName in new[]
        {
            "Actors",
            "StageHazardPresentation",
            "CombatBoxViewer",
            "StageDebugDrawer",
            "MvpHud",
            "CombatVfxManager",
            "StageIntroSequencer",
            "BossIntroSequencer",
            "DebugOverlay",
            "PauseMenu",
            "FormSelectOverlay",
        })
        {
            var node = root.GetNodeOrNull(nodeName);
            switch (node)
            {
                case Node3D node3D:
                    node3D.Visible = false;
                    break;
                case CanvasLayer canvasLayer:
                    canvasLayer.Visible = false;
                    break;
                case CanvasItem canvasItem:
                    canvasItem.Visible = false;
                    break;
            }
        }
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
        if (_mission.PresentationMode == StagePresentationMode.FullFramePlates)
        {
            return;
        }

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

    private void UpdateFullFramePlates(float cameraCenterX)
    {
        if (_mission.PresentationMode != StagePresentationMode.FullFramePlates
            || _fullFramePlates.Count == 0)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var viewportAspect = viewportSize.Y > 0.0f
            ? viewportSize.X / viewportSize.Y
            : 16.0f / 9.0f;
        var viewWorldHeight = _camera.Size;
        var viewWorldWidth = viewWorldHeight * viewportAspect;
        _fullFrameCoverageValid = true;

        for (var plateIndex = 0;
             plateIndex < _fullFramePlates.Count;
             plateIndex++)
        {
            var plate = _fullFramePlates[plateIndex];
            var sourceAspect = (float)plate.TextureWidth / plate.TextureHeight;
            var renderedWidth = viewWorldWidth;
            var renderedHeight = renderedWidth / sourceAspect;
            if (renderedHeight > viewWorldHeight)
            {
                renderedHeight = viewWorldHeight;
                renderedWidth = renderedHeight * sourceAspect;
            }

            var uniformScale = renderedWidth / Mathf.Max(
                0.1f,
                plate.TextureWidth * _mission.StageTexturePixelSize);
            var cameraForward = -_camera.GlobalTransform.Basis.Z.Normalized();
            var planeDistance = (
                plate.Data.PositionZ - _camera.GlobalPosition.Z)
                / cameraForward.Z;
            var frameCenter = _camera.GlobalPosition
                + cameraForward * planeDistance;
            plate.Sprite.Position = new Vector3(
                frameCenter.X,
                frameCenter.Y,
                plate.Data.PositionZ + plateIndex * 0.002f);
            plate.Sprite.Scale = new Vector3(
                uniformScale,
                uniformScale,
                1.0f);
            var weight = ResolveFullFramePlateWeight(
                plateIndex,
                cameraCenterX);
            plate.Sprite.Modulate = new Color(1.0f, 1.0f, 1.0f, weight);
            plate.Sprite.Visible = weight > 0.001f;
            _fullFrameCoverageValid &= Mathf.Abs(sourceAspect - viewportAspect)
                <= 0.001f;
        }
    }

    private float ResolveFullFramePlateWeight(int plateIndex, float cameraCenterX)
    {
        if (_forcedFullFramePlateIndex >= 0)
        {
            return plateIndex == Mathf.Clamp(
                _forcedFullFramePlateIndex,
                0,
                _fullFramePlates.Count - 1)
                ? 1.0f
                : 0.0f;
        }

        if (_fullFramePlates.Count == 1)
        {
            return 1.0f;
        }

        var transitionWidth = Mathf.Max(
            0.1f,
            _mission.FullFramePlateTransitionWidth);
        var halfTransition = transitionWidth * 0.5f;
        for (var leftIndex = 0;
             leftIndex < _fullFramePlates.Count - 1;
             leftIndex++)
        {
            var boundary = (
                _fullFramePlates[leftIndex].Data.CenterX
                + _fullFramePlates[leftIndex + 1].Data.CenterX) * 0.5f;
            if (cameraCenterX < boundary - halfTransition
                || cameraCenterX > boundary + halfTransition)
            {
                continue;
            }

            var transition = Mathf.InverseLerp(
                boundary - halfTransition,
                boundary + halfTransition,
                cameraCenterX);
            if (plateIndex == leftIndex)
            {
                return 1.0f - transition;
            }

            return plateIndex == leftIndex + 1 ? transition : 0.0f;
        }

        var nearestIndex = 0;
        var nearestDistance = float.MaxValue;
        for (var candidateIndex = 0;
             candidateIndex < _fullFramePlates.Count;
             candidateIndex++)
        {
            var distance = Mathf.Abs(
                cameraCenterX
                - _fullFramePlates[candidateIndex].Data.CenterX);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestIndex = candidateIndex;
        }

        return plateIndex == nearestIndex ? 1.0f : 0.0f;
    }

    private void UpdateStageSurfaceBounds()
    {
        if (_mission.PresentationMode is StagePresentationMode.BoundedArena
            or StagePresentationMode.FullFramePlates)
        {
            return;
        }

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
        if (_mission.ArenaPresentation is not null
            && _mission.PresentationMode is StagePresentationMode.BoundedArena
                or StagePresentationMode.FullFramePlates
            && director?.State
                == ProjectMannequin.Stage.ArcadeStageState.EncounterActive)
        {
            _boundedArenaStableRenderFrames++;
        }
        else
        {
            _boundedArenaStableRenderFrames = 0;
        }
        var boundedArenaReady =
            _mission.ArenaPresentation is null
            || _mission.PresentationMode is not (
                StagePresentationMode.BoundedArena
                or StagePresentationMode.FullFramePlates)
            || _boundedArenaStableRenderFrames >= 45;
        if (!_stageVisualSmokeEnabled
            || _stageVisualSmokeSummaryPrinted
            || _simulation is null
            || director is null
            || !boundedArenaReady
            || _simulation.CurrentTick < 30
            || (_mission.ArenaPresentation is null
                && director.StageProgress < 0.55f))
        {
            return;
        }

        _stageVisualSmokeSummaryPrinted = true;
        var activeLayerKinds = _layerRoots.Keys.Count;
        var legacySampling = _stageLayers.All(layer =>
            layer.Sprite.TextureFilter
            is not BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps
                and not BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps);
        var compositeBackdropSampling = _stageLayers.All(layer =>
            layer.Sprite.TextureFilter
            is BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps
                or BaseMaterial3D.TextureFilterEnum
                    .LinearWithMipmapsAnisotropic);
        var compositeFloorSampling =
            _gameplayFloor?.Mesh is PlaneMesh compositeFloor
            && compositeFloor.Material is StandardMaterial3D floorMaterial
            && floorMaterial.TextureFilter
                == BaseMaterial3D.TextureFilterEnum
                    .LinearWithMipmapsAnisotropic;
        var qualitySampling =
            _mission.PresentationMode is StagePresentationMode.CompositeTraversal
                or StagePresentationMode.BoundedArena
                or StagePresentationMode.FullFramePlates
                ? compositeBackdropSampling
                    && (_mission.PresentationMode == StagePresentationMode.FullFramePlates
                        || compositeFloorSampling)
                : legacySampling;
        var aspectPreserved = _stageLayers.All(layer =>
            Mathf.IsEqualApprox(layer.Sprite.Scale.X, layer.Sprite.Scale.Y));
        var authoredPanelCount = _mission.PresentationMode switch
        {
            StagePresentationMode.CompositeTraversal =>
                _mission.CompositeSegments.Sum(segment =>
                    string.IsNullOrWhiteSpace(segment.BackdropTexturePath)
                        ? 2
                        : 1),
            StagePresentationMode.BoundedArena => 1,
            StagePresentationMode.FullFramePlates =>
                _mission.FullFramePlates.Count,
            _ => _mission.BackgroundPanels.Count,
        };
        var fullFrameFidelity =
            _mission.PresentationMode != StagePresentationMode.FullFramePlates
            || (_fullFrameCoverageValid
                && _fullFramePlates.Count == _mission.FullFramePlates.Count
                && _gameplayFloor?.Visible == false);
        var requiredPanelCount = Mathf.Max(
            1,
            Mathf.Min(3, authoredPanelCount));
        var passed = activeLayerKinds == 4
            && _stageLayers.Count >= requiredPanelCount
            && _gameplayFloor is not null
            && (_parallaxObserved
                || _mission.PresentationMode is StagePresentationMode.BoundedArena
                    or StagePresentationMode.FullFramePlates)
            && _lightingTransitionObserved
            && qualitySampling
            && aspectPreserved
            && fullFrameFidelity;
        GD.Print(
            $"[StageVisualSmoke] SUMMARY passed={passed} "
            + $"roots={activeLayerKinds} panels={_stageLayers.Count}/"
            + $"{requiredPanelCount} "
            + $"floor={_gameplayFloor is not null} parallax={_parallaxObserved} "
            + $"lighting={_lightingTransitionObserved} sampling={qualitySampling} "
            + $"aspect={aspectPreserved} fidelity={fullFrameFidelity}");
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
        if (_mission.ArenaPresentation is not null
            && _mission.PresentationMode is StagePresentationMode.BoundedArena
                or StagePresentationMode.FullFramePlates
            && _mission.ArenaPresentation is not null)
        {
            return _mission.ArenaPresentation.CameraSize;
        }

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

    /// <summary>
    /// Atmospheric haze on the layers behind the fight.
    /// </summary>
    /// <remarks>
    /// The depth tint from the layer ramp is a MULTIPLY, so it scales a distant
    /// layer toward black and can never reduce its contrast. Aerial perspective
    /// is a BLEND instead: distance mixes everything toward the atmosphere, so
    /// far detail loses contrast in BOTH directions and converges on the sky.
    /// That contrast collapse is the cue the eye reads as depth, and it is why
    /// a backdrop can be correctly tinted and still look stuck to the fighters.
    /// Depth fog is exactly that blend.
    ///
    /// Note the direction is stage-dependent, not always a brightening: the
    /// haze colour is the stage's own atmosphere, so an outdoor dusk stage
    /// settles DOWNWARD toward its dark sky while a bright stage lifts. What is
    /// constant is the loss of contrast, which is the part that reads as depth.
    ///
    /// Deliberately NOT applied to full-frame plate stages. A plate is one
    /// complete painting on a single billboard, so every pixel of it sits at
    /// the same depth - fog would wash the whole image by a constant amount
    /// rather than grading it, flattening the aerial perspective the artist
    /// already painted in and breaking the plate fidelity comparison against
    /// the source PNG.
    /// </remarks>
    private void ApplyFarLayerHaze(Color hazeColor)
    {
        if (_mission.PresentationMode == StagePresentationMode.FullFramePlates)
        {
            return;
        }

        var haze = ProjectMannequin.Stage.StageGroundProjection
            .ResolveFarHazeRange(_mission, FarHazeLaneClearance, FarHazeSpan);

        _environment.FogEnabled = true;
        _environment.FogMode = Godot.Environment.FogModeEnum.Depth;
        _environment.FogLightColor = hazeColor;
        _environment.FogLightEnergy = FarHazeEnergy;
        _environment.FogDensity = FarHazeDensity;
        _environment.FogDepthBegin = haze.Begin;
        _environment.FogDepthEnd = haze.End;
        _environment.FogDepthCurve = FarHazeCurve;

        // The background is a flat clear colour standing in for open sky, and
        // it is the colour the haze already converges on. Letting fog tint it
        // as well would double the shift and, more importantly, would move the
        // clear colour that the junction audit relies on being exact.
        _environment.FogSkyAffect = 0.0f;

        // Sun scatter brightens fog toward the key light. The backdrops are
        // unshaded painted art that already carries its own light direction, so
        // adding a second runtime highlight would fight the painting.
        _environment.FogSunScatter = 0.0f;
    }

    private void CreateLighting()
    {
        var farColor = new Color(
            _mission.FarColorR,
            _mission.FarColorG,
            _mission.FarColorB);

        // Held before the capture override below so the haze colour is always
        // the stage's authored atmosphere. If the forced clear colour leaked
        // into the fog, the junction audit's two-render differential would see
        // fogged geometry change between renders and report false gaps.
        var hazeColor = farColor;

        // Capture-only override. The junction audit renders each stage twice
        // with different clear colours; only pixels that actually show the
        // background change between the two, which separates a real geometry
        // gap from a dark line painted into the art. Gated on persistence being
        // disabled, so it can only ever apply in a capture or smoke run.
        if (ProjectMannequin.Progression.MvpProgressStore.IsPersistenceDisabled)
        {
            var forced = OS.GetEnvironment("PROJECT_MANNEQUIN_FORCE_CLEAR_COLOR");
            if (!string.IsNullOrWhiteSpace(forced) && forced.StartsWith("#"))
            {
                farColor = new Color(forced);
            }
        }
        _environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = farColor,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Colors.White,
            AmbientLightEnergy = 0.62f,
        };
        ApplyFarLayerHaze(hazeColor);
        var worldEnvironment = new WorldEnvironment
        {
            Name = "StageFarLayer",
            Environment = _environment,
        };
        _layerRoots[StageVisualLayerKind.Far].AddChild(worldEnvironment);

        // Declared per stage so painted lighting and runtime lighting agree,
        // and published on StageKeyLight for presentation code that has no
        // reference to the mission.
        ProjectMannequin.Stage.StageKeyLight.Set(
            _mission.KeyLightPitchDegrees,
            _mission.KeyLightYawDegrees);
        _sun = new DirectionalLight3D
        {
            Name = "PrototypeSun",
            RotationDegrees = new Vector3(
                _mission.KeyLightPitchDegrees,
                _mission.KeyLightYawDegrees,
                0.0f),
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
        if (_mission.PresentationMode == StagePresentationMode.FullFramePlates)
        {
            CreateInvisibleGameplaySurface();
            return;
        }

        if (_mission.PresentationMode == StagePresentationMode.BoundedArena)
        {
            CreateBoundedArenaSurface();
            return;
        }

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
            // Deliberately unshaded. Promoting this to PerPixel was measured on
            // 2026-07-26: it does produce real directional cast shadows from
            // the fighters, but only 9,952 pixels of them, while brightening
            // 360,766 pixels of floor by +35 luma. That trade is bad three
            // times over. It would require re-approving every stage's floor
            // brightness, it buys nothing on the five full-frame plate stages
            // where the gameplay floor is invisible, and it spends the Phase 7
            // frame budget on shadow-mapping a full-screen plane for a cue the
            // per-actor contact shadow already delivers everywhere.
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
                using var source = GD.Load<Texture2D>(floorTexturePath);
                using var sourceImage = source.GetImage();
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
                    using var roadImage = sourceImage.GetRegion(
                        new Rect2I(0, roadTop, roadWidth, roadHeight));

                    // Mirror both axes so repeated floor art keeps matching edges
                    // while retaining the source's world-space proportions. The
                    // mirror puts a symmetry axis every second tile; it moves
                    // with the camera because the floor UV is world-anchored, so
                    // it is far less intrusive than the contrast an offset blend
                    // costs (measured 27 percent of floor detail).
                    using var mirroredRoadX = (Image)roadImage.Duplicate();
                    mirroredRoadX.FlipX();
                    using var mirroredRoadY = (Image)roadImage.Duplicate();
                    mirroredRoadY.FlipY();
                    using var mirroredRoadXY = (Image)mirroredRoadX.Duplicate();
                    mirroredRoadXY.FlipY();
                    using var seamlessTile = Image.CreateEmpty(
                        roadWidth * 2, roadHeight * 2, false, Image.Format.Rgba8);
                    seamlessTile.BlitRect(
                        roadImage,
                        new Rect2I(0, 0, roadWidth, roadHeight),
                        new Vector2I(0, 0));
                    seamlessTile.BlitRect(
                        mirroredRoadX,
                        new Rect2I(0, 0, roadWidth, roadHeight),
                        new Vector2I(roadWidth, 0));
                    seamlessTile.BlitRect(
                        mirroredRoadY,
                        new Rect2I(0, 0, roadWidth, roadHeight),
                        new Vector2I(0, roadHeight));
                    seamlessTile.BlitRect(
                        mirroredRoadXY,
                        new Rect2I(0, 0, roadWidth, roadHeight),
                        new Vector2I(roadWidth, roadHeight));
                    seamlessTile.GenerateMipmaps();

                    using var generatedFloorTexture =
                        ImageTexture.CreateFromImage(seamlessTile);
                    floorMaterial.AlbedoTexture = generatedFloorTexture;
                    floorMaterial.AlbedoColor = Colors.White;
                    floorMaterial.TextureFilter =
                        BaseMaterial3D.TextureFilterEnum
                            .LinearWithMipmapsAnisotropic;
                    var tileWidth = Mathf.Max(1.0f, _mission.FloorTextureTileWidth);
                    var tileDepth = tileWidth * roadHeight / roadWidth;
                    floorMaterial.Uv1Scale = new Vector3(
                        Mathf.Max(1.0f, floorWidth / (tileWidth * 2.0f)),
                        floorDepth / Mathf.Max(1.0f, tileDepth * 2.0f),
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
        CreateBackdropContactShadow(floorWidth);
    }

    /// <summary>
    /// Lays a soft occlusion band on the ground where the far backdrop meets it.
    /// </summary>
    /// <remarks>
    /// Most layered backdrops are elevations cropped flat at their bottom edge,
    /// with no plinths or contact shading. Their columns therefore terminate on
    /// a hard line even though the painting meets the floor exactly, and the
    /// junction reads as a cut. A darkening that is strongest against the
    /// backdrop and fades forward is what sells that contact in a real set.
    /// </remarks>
    private void CreateBackdropContactShadow(float floorWidth)
    {
        var farPanel = _mission.BackgroundPanels.FirstOrDefault(panel =>
            panel.Layer == StageVisualLayerKind.Far);
        if (farPanel is null)
        {
            return;
        }

        const int gradientHeight = 256;
        const float contactOpacity = 0.55f;
        using var gradient = Image.CreateEmpty(
            4,
            gradientHeight,
            false,
            Image.Format.Rgba8);
        for (var row = 0; row < gradientHeight; row++)
        {
            // PlaneMesh maps v = 0 to its minimum Z, which is the edge against
            // the backdrop, so row 0 carries the darkest contact.
            var distance = row / (float)(gradientHeight - 1);
            var falloff = (1.0f - distance) * (1.0f - distance);
            var alpha = contactOpacity * falloff;
            for (var column = 0; column < 4; column++)
            {
                gradient.SetPixel(column, row, new Color(0.0f, 0.0f, 0.0f, alpha));
            }
        }

        using var gradientTexture = ImageTexture.CreateFromImage(gradient);
        var shadowDepth = 3.2f;
        var contactShadow = new MeshInstance3D
        {
            Name = "BackdropContactShadow",
            Mesh = new PlaneMesh
            {
                Size = new Vector2(floorWidth, shadowDepth),
                Material = new StandardMaterial3D
                {
                    AlbedoTexture = gradientTexture,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    // The band lies flat on the floor, so writing depth would
                    // let it fight the floor plane it is meant to sit on.
                    DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
                },
            },
            Position = new Vector3(
                (_mission.StageMinX + _mission.StageMaxX) * 0.5f,
                -0.03f,
                farPanel.PositionZ + shadowDepth * 0.5f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _layerRoots[StageVisualLayerKind.Gameplay].AddChild(contactShadow);
    }

    private void CreateStageBackground()
    {
        if (_mission.PresentationMode == StagePresentationMode.FullFramePlates)
        {
            CreateFullFramePlateBackgrounds();
            return;
        }

        if (_mission.PresentationMode == StagePresentationMode.BoundedArena)
        {
            CreateBoundedArenaBackground();
            return;
        }

        if (_mission.PresentationMode == StagePresentationMode.CompositeTraversal)
        {
            CreateCompositeStageBackground();
            return;
        }

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

    private void CreateCompositeStageBackground()
    {
        for (var segmentIndex = 0;
             segmentIndex < _mission.CompositeSegments.Count;
             segmentIndex++)
        {
            var segment = _mission.CompositeSegments[segmentIndex];
            var backdropTexturePath = string.IsNullOrWhiteSpace(
                segment.BackdropTexturePath)
                ? segment.TexturePath
                : segment.BackdropTexturePath;
            if (!ResourceLoader.Exists(backdropTexturePath))
            {
                GD.PushWarning(
                    $"Composite stage texture not found: {backdropTexturePath}");
                continue;
            }

            try
            {
                var sourceTexture = GD.Load<Texture2D>(backdropTexturePath);
                var usesProductionBackdrop = !string.IsNullOrWhiteSpace(
                    segment.BackdropTexturePath);
                Texture2D backdropTexture = sourceTexture;
                var floorEndPixel = sourceTexture.GetHeight();
                if (!usesProductionBackdrop)
                {
                    floorEndPixel = Mathf.Clamp(
                        Mathf.RoundToInt(
                            sourceTexture.GetHeight()
                            * segment.FloorEndFraction),
                        2,
                        sourceTexture.GetHeight() - 1);
                    backdropTexture = new AtlasTexture
                    {
                        Atlas = sourceTexture,
                        Region = new Rect2(
                            0,
                            0,
                            sourceTexture.GetWidth(),
                            floorEndPixel),
                    };
                }
                var worldWidth = Mathf.Max(0.1f, segment.MaxX - segment.MinX);
                var sourceWorldWidth =
                    backdropTexture.GetWidth() * _mission.StageTexturePixelSize;
                var uniformScale =
                    worldWidth / Mathf.Max(0.1f, sourceWorldWidth);
                var renderedHeight =
                    backdropTexture.GetHeight()
                    * _mission.StageTexturePixelSize
                    * uniformScale;
                var centerY = renderedHeight * 0.5f;
                if (usesProductionBackdrop)
                {
                    var cameraForward = -_camera.GlobalTransform.Basis.Z
                        .Normalized();
                    var planeDistance = (
                        segment.BackdropPositionZ
                        - _camera.GlobalPosition.Z)
                        / cameraForward.Z;
                    var centerRayY = _camera.GlobalPosition.Y
                        + cameraForward.Y * planeDistance;
                    var protectedCenterFraction = (
                        segment.ProtectedFrameTopFraction
                        + segment.ProtectedFrameBottomFraction) * 0.5f;
                    centerY = centerRayY
                        + (protectedCenterFraction - 0.5f) * renderedHeight;
                }
                var basePosition = new Vector3(
                    (segment.MinX + segment.MaxX) * 0.5f,
                    centerY,
                    segment.BackdropPositionZ);
                var sprite = new Sprite3D
                {
                    Name = $"CompositeBackdrop{segmentIndex + 1:00}",
                    Texture = backdropTexture,
                    Position = basePosition,
                    PixelSize = _mission.StageTexturePixelSize,
                    Scale = new Vector3(uniformScale, uniformScale, 1.0f),
                    FlipH = segment.FlipH,
                    TextureFilter = BaseMaterial3D.TextureFilterEnum
                        .LinearWithMipmaps,
                    Shaded = false,
                    DoubleSided = true,
                    AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
                    Billboard = usesProductionBackdrop
                        ? BaseMaterial3D.BillboardModeEnum.Enabled
                        : BaseMaterial3D.BillboardModeEnum.Disabled,
                };
                var layerData = new StageBackgroundPanelData
                {
                    TexturePath = backdropTexturePath,
                    Layer = StageVisualLayerKind.Midground,
                    MinX = segment.MinX,
                    MaxX = segment.MaxX,
                    ParallaxFactorX = segment.ParallaxFactorX,
                };
                _layerRoots[StageVisualLayerKind.Midground].AddChild(sprite);
                _stageLayers.Add(new StageLayerInstance(
                    sprite,
                    basePosition,
                    layerData));
                _background ??= sprite;

                if (usesProductionBackdrop)
                {
                    continue;
                }

                var foregroundTexture = new AtlasTexture
                {
                    Atlas = sourceTexture,
                    Region = new Rect2(
                        0,
                        floorEndPixel,
                        sourceTexture.GetWidth(),
                        sourceTexture.GetHeight() - floorEndPixel),
                };
                var foregroundScale =
                    worldWidth / Mathf.Max(
                        0.1f,
                        foregroundTexture.GetWidth()
                            * _mission.StageTexturePixelSize);
                var foregroundHeight =
                    foregroundTexture.GetHeight()
                    * _mission.StageTexturePixelSize
                    * foregroundScale;
                var foregroundPosition = new Vector3(
                    (segment.MinX + segment.MaxX) * 0.5f,
                    -foregroundHeight * 0.5f,
                    segment.ForegroundPositionZ);
                var foregroundSprite = new Sprite3D
                {
                    Name = $"CompositeForeground{segmentIndex + 1:00}",
                    Texture = foregroundTexture,
                    Position = foregroundPosition,
                    PixelSize = _mission.StageTexturePixelSize,
                    Scale = new Vector3(
                        foregroundScale,
                        foregroundScale,
                        1.0f),
                    FlipH = segment.FlipH,
                    TextureFilter = BaseMaterial3D.TextureFilterEnum
                        .LinearWithMipmaps,
                    Shaded = false,
                    DoubleSided = true,
                    AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
                };
                var foregroundData = new StageBackgroundPanelData
                {
                    TexturePath = segment.TexturePath,
                    Layer = StageVisualLayerKind.Foreground,
                    MinX = segment.MinX,
                    MaxX = segment.MaxX,
                    ParallaxFactorX = segment.ForegroundParallaxFactorX,
                };
                _layerRoots[StageVisualLayerKind.Foreground]
                    .AddChild(foregroundSprite);
                _stageLayers.Add(new StageLayerInstance(
                    foregroundSprite,
                    foregroundPosition,
                    foregroundData));
            }
            catch (Exception ex)
            {
                GD.PushWarning(
                    $"Failed to build composite backdrop '{backdropTexturePath}': "
                    + ex.Message);
            }
        }
    }

    private void CreateFullFramePlateBackgrounds()
    {
        for (var plateIndex = 0;
             plateIndex < _mission.FullFramePlates.Count;
             plateIndex++)
        {
            var plate = _mission.FullFramePlates[plateIndex];
            if (!ResourceLoader.Exists(plate.TexturePath))
            {
                GD.PushWarning($"Full-frame stage plate not found: {plate.TexturePath}");
                continue;
            }

            try
            {
                using var sourceTexture = GD.Load<Texture2D>(plate.TexturePath);
                using var sourceImage = sourceTexture.GetImage();
                if (sourceImage.IsCompressed())
                {
                    sourceImage.Decompress();
                }
                sourceImage.Convert(Image.Format.Rgba8);
                sourceImage.GenerateMipmaps();
                using var plateTexture = ImageTexture.CreateFromImage(sourceImage);
                var sprite = new Sprite3D
                {
                    Name = $"FullFramePlate{plateIndex + 1:00}",
                    Texture = plateTexture,
                    PixelSize = _mission.StageTexturePixelSize,
                    FlipH = plate.FlipH,
                    TextureFilter = BaseMaterial3D.TextureFilterEnum
                        .LinearWithMipmaps,
                    Shaded = false,
                    DoubleSided = true,
                    AlphaCut = SpriteBase3D.AlphaCutMode.Disabled,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    // A screen-filling alpha-blended backdrop must sort before
                    // every other transparent object. Without this it draws
                    // last and erases anything transparent in the gameplay
                    // layer, such as per-actor contact shadows.
                    RenderPriority = -8,
                };
                _layerRoots[StageVisualLayerKind.Midground].AddChild(sprite);
                _fullFramePlates.Add(new FullFramePlateInstance(
                    sprite,
                    plate,
                    sourceImage.GetWidth(),
                    sourceImage.GetHeight()));
                _stageLayers.Add(new StageLayerInstance(
                    sprite,
                    Vector3.Zero,
                    new StageBackgroundPanelData
                    {
                        TexturePath = plate.TexturePath,
                        Layer = StageVisualLayerKind.Midground,
                        MinX = plate.CenterX,
                        MaxX = plate.CenterX,
                        ParallaxFactorX = 1.0f,
                    }));
                _background ??= sprite;
            }
            catch (Exception ex)
            {
                GD.PushWarning(
                    $"Failed to build full-frame plate '{plate.TexturePath}': "
                    + ex.Message);
            }
        }

        UpdateFullFramePlates(_startingCameraCenterX);
    }

    private void CreateInvisibleGameplaySurface()
    {
        var stageLength = Mathf.Max(1.0f, _mission.StageMaxX - _mission.StageMinX);
        _gameplayFloor = new MeshInstance3D
        {
            Name = "GameplayFloor",
            Mesh = new PlaneMesh
            {
                Size = new Vector2(stageLength + 30.0f, 24.0f),
            },
            Position = new Vector3(
                (_mission.StageMinX + _mission.StageMaxX) * 0.5f,
                -0.035f,
                0.0f),
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _layerRoots[StageVisualLayerKind.Gameplay].AddChild(_gameplayFloor);
    }

    private void CreateBoundedArenaBackground()
    {
        var arena = _mission.ArenaPresentation;
        if (arena is null || !ResourceLoader.Exists(arena.BackdropTexturePath))
        {
            GD.PushWarning("Bounded arena backdrop is missing.");
            return;
        }

        try
        {
            using var sourceTexture = GD.Load<Texture2D>(arena.BackdropTexturePath);
            using var sourceImage = sourceTexture.GetImage();
            if (sourceImage.IsCompressed())
            {
                sourceImage.Decompress();
            }
            sourceImage.Convert(Image.Format.Rgba8);
            sourceImage.GenerateMipmaps();
            using var backdropTexture = ImageTexture.CreateFromImage(sourceImage);
            var worldWidth = Mathf.Max(1.0f, arena.BackdropWorldWidth);
            var uniformScale = worldWidth / Mathf.Max(
                0.1f,
                backdropTexture.GetWidth() * _mission.StageTexturePixelSize);
            var renderedHeight = backdropTexture.GetHeight()
                * _mission.StageTexturePixelSize
                * uniformScale;
            var cameraForward = -_camera.GlobalTransform.Basis.Z.Normalized();
            var planeDistance = (arena.BackdropPositionZ - _camera.GlobalPosition.Z)
                / cameraForward.Z;
            var centerRayY = _camera.GlobalPosition.Y
                + cameraForward.Y * planeDistance;
            var basePosition = new Vector3(
                arena.CenterX,
                centerRayY + arena.BackdropBottomY,
                arena.BackdropPositionZ);
            var backdrop = new Sprite3D
            {
                Name = "BoundedArenaBackdrop",
                Texture = backdropTexture,
                Position = basePosition,
                PixelSize = _mission.StageTexturePixelSize,
                Scale = new Vector3(uniformScale, uniformScale, 1.0f),
                TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
                Shaded = false,
                DoubleSided = true,
                AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            };
            _layerRoots[StageVisualLayerKind.Midground].AddChild(backdrop);
            _stageLayers.Add(new StageLayerInstance(
                backdrop,
                basePosition,
                new StageBackgroundPanelData
                {
                    TexturePath = arena.BackdropTexturePath,
                    Layer = StageVisualLayerKind.Midground,
                    MinX = arena.CenterX - worldWidth * 0.5f,
                    MaxX = arena.CenterX + worldWidth * 0.5f,
                    ParallaxFactorX = 1.0f,
                }));
            _background = backdrop;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Failed to build bounded arena backdrop: {ex.Message}");
        }
    }

    private void CreateBoundedArenaSurface()
    {
        var arena = _mission.ArenaPresentation;
        if (arena is null)
        {
            return;
        }

        var floorMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(
                _mission.FloorColorR,
                _mission.FloorColorG,
                _mission.FloorColorB),
            Roughness = 0.95f,
            Metallic = 0.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            TextureFilter = BaseMaterial3D.TextureFilterEnum
                .LinearWithMipmapsAnisotropic,
            TextureRepeat = false,
        };
        if (ResourceLoader.Exists(arena.FloorTexturePath))
        {
            try
            {
                using var sourceTexture = GD.Load<Texture2D>(arena.FloorTexturePath);
                using var sourceImage = sourceTexture.GetImage();
                if (sourceImage.IsCompressed())
                {
                    sourceImage.Decompress();
                }
                sourceImage.Convert(Image.Format.Rgba8);
                sourceImage.GenerateMipmaps();
                using var floorTexture = ImageTexture.CreateFromImage(sourceImage);
                floorMaterial.AlbedoTexture = floorTexture;
                floorMaterial.AlbedoColor = Colors.White;
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Failed to build bounded arena floor: {ex.Message}");
            }
        }

        var floorSize = Mathf.Max(1.0f, arena.FloorWorldSize);
        _gameplayFloor = new MeshInstance3D
        {
            Name = "GameplayFloor",
            Mesh = new PlaneMesh
            {
                Size = new Vector2(floorSize, floorSize),
                Material = floorMaterial,
            },
            Position = new Vector3(
                arena.CenterX,
                -0.035f,
                arena.FloorNearZ - floorSize * 0.5f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _layerRoots[StageVisualLayerKind.Gameplay].AddChild(_gameplayFloor);
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
            var texture = CropPanelTexture(sourceTexture, panel);

            var worldWidth = Mathf.Max(0.1f, panel.MaxX - panel.MinX);
            var sourceWorldWidth =
                texture.GetWidth() * _mission.StageTexturePixelSize;
            var uniformScale =
                worldWidth / Mathf.Max(0.1f, sourceWorldWidth)
                * Mathf.Max(0.1f, panel.ScaleYMultiplier);
            var sourceWorldHeight =
                texture.GetHeight() * _mission.StageTexturePixelSize;
            // The far layer is the only thing behind the ground plane, so a
            // short one leaves the clear colour showing along the top of the
            // frame. Grow it uniformly until it reaches the top of the widest
            // camera envelope; authored scale still wins when it is taller.
            var groundLineFraction = Mathf.Clamp(panel.GroundLineFraction, 0.05f, 1.0f);
            if (panel.Layer == StageVisualLayerKind.Far
                && panel.AlignBottomToFloor)
            {
                // Only the part above the painted ground line is on screen, so
                // the panel has to be taller by that same proportion to keep
                // reaching the top of the frame.
                var requiredHeight =
                    ResolveFarLayerCoverHeight(panel.PositionZ) / groundLineFraction;
                uniformScale = Mathf.Max(
                    uniformScale,
                    requiredHeight / Mathf.Max(0.1f, sourceWorldHeight));
            }

            var renderedWidth = sourceWorldWidth * uniformScale;
            var renderedHeight = sourceWorldHeight * uniformScale;
            var centerY = panel.AlignBottomToFloor
                ? renderedHeight * (groundLineFraction - 0.5f)
                : panel.PositionY;
            var firstCenterX = panel.RepeatHorizontally
                ? panel.MinX - renderedWidth * 0.5f
                : (panel.MinX + panel.MaxX) * 0.5f;
            var lastCenterX = panel.RepeatHorizontally
                ? panel.MaxX + renderedWidth * 0.5f
                : firstCenterX;
            var tileIndex = 0;
            for (var centerX = firstCenterX;
                 centerX <= lastCenterX;
                 centerX += renderedWidth)
            {
                var basePosition = new Vector3(
                    centerX,
                    centerY,
                    panel.PositionZ);
                var backgroundPanel = new Sprite3D
                {
                    Name = $"{panel.Layer}{_mission.WorldId}Panel"
                        + $"{panelIndex + 1:00}Tile{tileIndex + 1:00}",
                    Texture = texture,
                    Position = basePosition,
                    PixelSize = _mission.StageTexturePixelSize,
                    Scale = new Vector3(
                        uniformScale,
                        uniformScale,
                        1.0f),
                    FlipH = panel.FlipH ^ (panel.RepeatHorizontally
                        && tileIndex % 2 == 1),
                    TextureFilter = panel.Sampling == StageTextureSampling.Nearest
                        ? BaseMaterial3D.TextureFilterEnum.Nearest
                        : BaseMaterial3D.TextureFilterEnum.Linear,
                    Modulate = ResolveLayerDebugTint(panel.Layer, panel.Opacity),
                    Shaded = false,
                    DoubleSided = true,
                    AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
                    // Layer order is explicit rather than left to depth
                    // sorting. These panels are alpha-blended billboards at
                    // similar depths, and without a declared priority the far
                    // backdrop was compositing over the midground props, so a
                    // torii gate and a lantern pavilion at z=-4.8 were hidden
                    // behind a deck wall at z=-6 with only their feet showing.
                    RenderPriority = ResolveLayerRenderPriority(panel.Layer),
                };
                _layerRoots[panel.Layer].AddChild(backgroundPanel);
                _stageLayers.Add(new StageLayerInstance(
                    backgroundPanel,
                    basePosition,
                    panel));
                _background ??= backgroundPanel;
                tileIndex++;
            }
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Failed to load stage panel texture '{panel.TexturePath}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// World height a floor-anchored far layer needs so its top edge reaches the
    /// top of the frame at the stage's widest camera size.
    /// </summary>
    /// <summary>
    /// Aerial-perspective tint per parallax layer.
    /// </summary>
    /// <remarks>
    /// Distance was previously carried only by scale and parallax, so every
    /// layer was drawn at full brightness and the frame read flat: a fighter
    /// and a building forty metres behind them had the same contrast and the
    /// same colour temperature. Real depth cues come from atmosphere, so each
    /// layer further from the camera is darkened and shifted cooler.
    ///
    /// This is a multiply on <c>Modulate</c>, so it cannot desaturate directly;
    /// the cool shift approximates that by pulling red down faster than blue.
    /// Only RGB is touched. Alpha is deliberately left alone, because an alpha
    /// below 1 moves the panel into a render path where the backdrop composites
    /// over it and the layer vanishes.
    /// </remarks>
    private static Color ResolveLayerDepthTint(StageVisualLayerKind layer) =>
        layer switch
        {
            StageVisualLayerKind.Far => new Color(0.74f, 0.80f, 0.93f),
            StageVisualLayerKind.Midground => new Color(0.88f, 0.91f, 0.98f),
            StageVisualLayerKind.Gameplay => Colors.White,
            // Foreground occluders sit between the light and the camera, so
            // they read as shadowed rather than brighter.
            StageVisualLayerKind.Foreground => new Color(0.82f, 0.85f, 0.92f),
            _ => Colors.White,
        };

    /// <summary>
    /// Declared draw order for the stage's parallax layers.
    /// </summary>
    /// <remarks>
    /// Spaced so individual panels can still be nudged between layers later
    /// without colliding. Full-frame stage plates sit at -8, further back than
    /// any of these.
    /// </remarks>
    private static int ResolveLayerRenderPriority(StageVisualLayerKind layer) =>
        layer switch
        {
            StageVisualLayerKind.Far => -6,
            StageVisualLayerKind.Midground => -3,
            StageVisualLayerKind.Gameplay => 0,
            StageVisualLayerKind.Foreground => 3,
            _ => 0,
        };

    /// <summary>
    /// Capture-only flat tint for one background layer, so an audit can prove
    /// which layer owns a given pixel instead of inferring it from colour.
    /// </summary>
    /// <remarks>
    /// Gated on persistence being disabled, so it can only apply in a capture
    /// or smoke run and never in a player session.
    /// </remarks>
    private static Color ResolveLayerDebugTint(
        StageVisualLayerKind layer,
        float opacity)
    {
        var depth = ResolveLayerDepthTint(layer);
        var normal = new Color(depth.R, depth.G, depth.B, opacity);
        if (!ProjectMannequin.Progression.MvpProgressStore.IsPersistenceDisabled)
        {
            return normal;
        }

        var hidden = OS.GetEnvironment("PROJECT_MANNEQUIN_HIDE_LAYER");
        if (!string.IsNullOrWhiteSpace(hidden)
            && System.Enum.TryParse<StageVisualLayerKind>(
                hidden, ignoreCase: true, out var hiddenLayer)
            && hiddenLayer == layer)
        {
            return new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }

        var requested = OS.GetEnvironment("PROJECT_MANNEQUIN_TINT_LAYER");
        if (string.IsNullOrWhiteSpace(requested)
            || !System.Enum.TryParse<StageVisualLayerKind>(
                requested, ignoreCase: true, out var target)
            || target != layer)
        {
            return normal;
        }

        return new Color(1.0f, 0.0f, 1.0f, 1.0f);
    }

    private float ResolveFarLayerCoverHeight(float panelZ)
    {
        var resting = ProjectMannequin.Stage.StageGroundProjection
            .ResolveRestingCameraProfile(_mission);
        var forwardY = resting.LookHeight - resting.Height;
        var forwardZ = -resting.Depth;
        var length = Mathf.Sqrt(forwardY * forwardY + forwardZ * forwardZ);
        if (length <= 0.0f)
        {
            return 0.0f;
        }

        var upY = resting.Depth / length;
        var upZ = forwardY / length;
        if (Mathf.IsZeroApprox(upY))
        {
            return 0.0f;
        }

        var halfView = Mathf.Max(resting.Size, _mission.CameraMaxSize) * 0.5f;
        var topWorldY = resting.Height
            + (halfView - (panelZ - resting.Depth) * upZ) / upY;
        return Mathf.Max(0.0f, topWorldY);
    }

    private static Texture2D CropPanelTexture(
        Texture2D sourceTexture,
        StageBackgroundPanelData panel)
    {
        var solidified = SolidifyPanelAlpha(sourceTexture, panel.AlphaSolidify);
        var cropTop = Mathf.RoundToInt(
            solidified.GetHeight() * panel.CropTopFraction);
        var cropBottom = Mathf.RoundToInt(
            solidified.GetHeight() * panel.CropBottomFraction);
        var cropHeight = solidified.GetHeight() - cropTop - cropBottom;
        if ((cropTop <= 0 && cropBottom <= 0) || cropHeight <= 0)
        {
            return solidified;
        }

        return new AtlasTexture
        {
            Atlas = solidified,
            Region = new Rect2(
                0,
                cropTop,
                solidified.GetWidth(),
                cropHeight),
        };
    }

    /// <summary>
    /// Remaps a panel texture's alpha so painted interiors read as solid.
    /// </summary>
    /// <remarks>
    /// Some props carry mid-range alpha across large interior areas, so the
    /// backdrop shows through stone plinths and timber that should be opaque.
    /// Raising the panel's <c>Opacity</c> cannot fix that, because the problem
    /// is in the texture rather than the modulate. A narrow soft band is kept
    /// below the threshold so silhouette edges stay anti-aliased instead of
    /// turning into stair steps.
    /// </remarks>
    private static Texture2D SolidifyPanelAlpha(Texture2D sourceTexture, float solidify)
    {
        if (solidify <= 0.0f)
        {
            return sourceTexture;
        }

        try
        {
            using var image = sourceTexture.GetImage();
            if (image is null || image.GetHeight() <= 0)
            {
                return sourceTexture;
            }

            if (image.IsCompressed())
            {
                image.Decompress();
            }

            image.Convert(Image.Format.Rgba8);

            // GetData returns every mip level concatenated, so walking it with
            // a 4-byte stride would corrupt the smaller levels. Drop them here
            // and regenerate once the alpha curve has been applied.
            if (image.HasMipmaps())
            {
                image.ClearMipmaps();
            }

            // Anything at or above solidFrom becomes fully opaque; the band
            // between fadeFloor and solidFrom stays soft for edge quality.
            const float fadeFloor = 0.08f;
            var solidFrom = Mathf.Lerp(1.0f, 0.30f, Mathf.Clamp(solidify, 0.0f, 1.0f));
            var span = Mathf.Max(0.01f, solidFrom - fadeFloor);

            // Raw byte access rather than GetPixel/SetPixel: these panels are
            // multi-megapixel, and per-pixel marshalling turned a load-time
            // step into a visible hitch.
            var pixels = image.GetData();
            var floorByte = (byte)Mathf.RoundToInt(fadeFloor * 255.0f);
            var lookup = new byte[256];
            for (var value = 0; value < 256; value++)
            {
                if (value <= floorByte || value >= 255)
                {
                    lookup[value] = (byte)value;
                    continue;
                }

                var normalized = ((value / 255.0f) - fadeFloor) / span;
                lookup[value] = (byte)Mathf.RoundToInt(
                    Mathf.Clamp(normalized, 0.0f, 1.0f) * 255.0f);
            }

            for (var index = 3; index < pixels.Length; index += 4)
            {
                pixels[index] = lookup[pixels[index]];
            }

            image.SetData(
                image.GetWidth(),
                image.GetHeight(),
                false,
                Image.Format.Rgba8,
                pixels);
            image.GenerateMipmaps();
            return ImageTexture.CreateFromImage(image);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Could not solidify panel alpha: {exception.Message}");
            return sourceTexture;
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
                    layer.Sprite.Texture = CropPanelTexture(
                        GD.Load<Texture2D>(texturePath),
                        layer.Data);
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
        var boundedArena = _mission.ArenaPresentation is not null
            && _mission.PresentationMode is (
                StagePresentationMode.BoundedArena
                or StagePresentationMode.FullFramePlates)
            ? _mission.ArenaPresentation
            : null;
        var startingCenterX = boundedArena?.CenterX
            ?? _mission.StageMinX + _mission.CameraViewportWidth * 0.5f;
        // Share the resting framing with _Process so the very first rendered
        // frame already grounds fighters on the painted floor. Capture tooling
        // reads early frames, so a different first-frame camera would bake an
        // uncalibrated pose into the evidence.
        var restingCamera =
            ProjectMannequin.Stage.StageGroundProjection
                .ResolveRestingCameraProfile(_mission);
        _camera = new Camera3D
        {
            Name = "PrototypeCamera",
            Current = true,
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = restingCamera.Size,
            Position = new Vector3(
                startingCenterX,
                restingCamera.Height,
                restingCamera.Depth),
            KeepAspect = Camera3D.KeepAspectEnum.Height,
        };
        AddChild(_camera);
        _camera.LookAt(new Vector3(
            startingCenterX,
            restingCamera.LookHeight,
            0.0f), Vector3.Up);
    }

    private sealed record StageLayerInstance(
        Sprite3D Sprite,
        Vector3 BasePosition,
        StageBackgroundPanelData Data);

    private sealed record FullFramePlateInstance(
        Sprite3D Sprite,
        StageFullFramePlateData Data,
        int TextureWidth,
        int TextureHeight);

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
