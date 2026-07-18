using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Presentation;
using ProjectMannequin.Stage;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Presentation regression for the full final-boss intro restored from the
/// prior session: cutscene/cinematic hide HUD, READY reveals lifebars, FIGHT is
/// visible, and both bars remain seated once combat begins.
/// </summary>
public partial class BossIntroHudSmokeScenario : Node
{
    private GameSimulation? _simulation;
    private MvpHud? _hud;
    private BossIntroSequencer? _sequencer;
    private bool _sawCutscene;
    private bool _sawStarted;
    private bool _sawReady;
    private bool _sawFight;
    private bool _hudHiddenDuringIntro;
    private bool _readyMessageVisible;
    private bool _fightMessageVisible;
    private bool _barsVisibleAfterReady;
    private int _framesAfterFight;
    private int _processFrames;
    private int _cutsceneCaptureStableFrames;
    private bool _cutsceneCaptureSaved;
    private bool _hudCaptureSaved;
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();

    public void Initialize(
        GameSimulation simulation,
        MvpHud hud,
        BossIntroSequencer sequencer)
    {
        _simulation = simulation;
        _hud = hud;
        _sequencer = sequencer;
        ProcessMode = ProcessModeEnum.Always;
        PrepareCaptureOutput("PROJECT_MANNEQUIN_BOSS_CUTSCENE_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_BOSS_INTRO_POSE_CAPTURE");
        PrepareCaptureOutput("PROJECT_MANNEQUIN_HUD_FRAME_CAPTURE");
        GD.Print("[BossIntroHudSmoke] Driver active.");
    }

    public override void _Process(double delta)
    {
        if (_simulation?.EncounterDirector is null || _hud is null || _sequencer is null)
        {
            return;
        }

        _processFrames++;
        CapturePreviousStep();
        CaptureCutsceneIfRequested();

        var director = _simulation.EncounterDirector;
        var player = _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
        if (!_sawFight && player is not null)
        {
            if (director.State == ArcadeStageState.Traveling)
            {
                player.SimPosition = new Vector3(
                    director.CurrentEncounter.TriggerX + 0.1f,
                    0.0f,
                    0.0f);
                player.Position = player.SimPosition;
            }

            _simulation.StepSimulation();
            return;
        }

        if (_sawFight)
        {
            _framesAfterFight++;
            _barsVisibleAfterReady |= _hud.HudOpacity >= 0.95f
                && _hud.PlayerLifeBarVisible
                && _hud.BossLifeBarVisible
                && _hud.PlayerHealthBarValue > 0.0
                && _hud.BossHealthBarValue > 0.0;
            CaptureHudIfRequested();
            if (_framesAfterFight >= 45)
            {
                Finish();
            }
        }

        if (_processFrames >= 1400)
        {
            GD.PushError($"[BossIntroHudSmoke] Timed out at state={director.State} phase={_sequencer.Phase}.");
            GetTree().Quit();
        }
    }

    private void CapturePreviousStep()
    {
        _presentationEventBuffer.Clear();
        _presentationEventCursor = _simulation!.CopyPresentationEventsSince(
            _presentationEventCursor,
            _presentationEventBuffer);
        foreach (var presentationEvent in _presentationEventBuffer)
        {
            switch (presentationEvent.Type)
            {
                case CombatPresentationEventType.BossIntroCutscene:
                    _sawCutscene = true;
                    _hudHiddenDuringIntro |= _hud!.HudOpacity <= 0.05f;
                    break;
                case CombatPresentationEventType.BossIntroStarted:
                    _sawStarted = true;
                    _hudHiddenDuringIntro |= _hud!.HudOpacity <= 0.05f;
                    break;
                case CombatPresentationEventType.BossIntroReady:
                    _sawReady = true;
                    _readyMessageVisible |= _sequencer!.IsMessageVisible
                        && _sequencer.MessageText.Contains("READY");
                    break;
                case CombatPresentationEventType.BossIntroFight:
                    _sawFight = true;
                    _fightMessageVisible |= _sequencer!.IsMessageVisible
                        && _sequencer.MessageText.Contains("FIGHT");
                    break;
            }
        }
    }

    private void Finish()
    {
        var cutsceneCapturePath = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_BOSS_CUTSCENE_CAPTURE");
        var introCapturePath = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_BOSS_INTRO_POSE_CAPTURE");
        var hudCapturePath = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_HUD_FRAME_CAPTURE");
        var cutsceneCapturePassed = string.IsNullOrWhiteSpace(cutsceneCapturePath)
            || File.Exists(cutsceneCapturePath);
        var introCapturePassed = string.IsNullOrWhiteSpace(introCapturePath)
            || File.Exists(introCapturePath);
        var hudCapturePassed = string.IsNullOrWhiteSpace(hudCapturePath)
            || File.Exists(hudCapturePath);
        var styledHudFrame = _hud!.HudFramePath.EndsWith(
                "project_mannequin_hud_frame_style_v2.png")
            && ResourceLoader.Exists(_hud.HudFramePath);
        var activePlayer = _simulation!.Actors.FirstOrDefault(actor =>
            actor.IsPlayerControlled);
        var activeBoss = _simulation.Actors.FirstOrDefault(actor =>
            (actor.IsBoss || actor.IsElite) && !actor.IsDead);
        var currentPortraits = activePlayer is not null
            && activeBoss is not null
            && _hud.PlayerPortraitFormId == activePlayer.CurrentForm.Id
            && _hud.BossPortraitFormId == activeBoss.CurrentForm.Id;
        var passed = _sawCutscene
            && _sawStarted
            && _sawReady
            && _sawFight
            && _hudHiddenDuringIntro
            && _readyMessageVisible
            && _fightMessageVisible
            && _barsVisibleAfterReady
            && cutsceneCapturePassed
            && introCapturePassed
            && hudCapturePassed
            && styledHudFrame
            && currentPortraits;
        GD.Print(
            $"[BossIntroHudSmoke] SUMMARY passed={passed} "
            + $"cutscene={_sawCutscene} started={_sawStarted} ready={_sawReady} fight={_sawFight} "
            + $"hidden={_hudHiddenDuringIntro} readyMsg={_readyMessageVisible} "
            + $"fightMsg={_fightMessageVisible} bars={_barsVisibleAfterReady} "
            + $"cutsceneCapture={cutsceneCapturePassed} "
            + $"introCapture={introCapturePassed} "
            + $"hudCapture={hudCapturePassed} styledFrame={styledHudFrame} "
            + $"currentPortraits={currentPortraits} "
            + $"opacity={_hud!.HudOpacity:0.00} phase={_sequencer!.Phase}");
        if (!passed)
        {
            GD.PushError("[BossIntroHudSmoke] Full final-boss intro/HUD lifecycle regressed.");
        }

        GetTree().Quit();
    }

    private void CaptureCutsceneIfRequested()
    {
        var path = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_BOSS_CUTSCENE_CAPTURE");
        if (_cutsceneCaptureSaved
            || string.IsNullOrWhiteSpace(path)
            || _sequencer?.IsCutsceneVisible != true)
        {
            return;
        }

        _cutsceneCaptureStableFrames++;
        if (_cutsceneCaptureStableFrames < 90)
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
            GD.PushError(
                $"[BossIntroHudSmoke] Could not save matchup cutscene capture '{path}' ({error}).");
            return;
        }

        _cutsceneCaptureSaved = true;
        GD.Print($"[BossIntroHudSmoke] Matchup cutscene capture saved: {path}");
    }

    private void CaptureHudIfRequested()
    {
        var path = OS.GetEnvironment("PROJECT_MANNEQUIN_HUD_FRAME_CAPTURE");
        if (_hudCaptureSaved
            || string.IsNullOrWhiteSpace(path)
            || _framesAfterFight < 30
            || !_barsVisibleAfterReady)
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
            GD.PushError(
                $"[BossIntroHudSmoke] Could not save HUD frame capture '{path}' ({error}).");
            return;
        }

        _hudCaptureSaved = true;
        GD.Print($"[BossIntroHudSmoke] HUD frame capture saved: {path}");
    }

    private static void PrepareCaptureOutput(string environmentVariable)
    {
        var path = OS.GetEnvironment(environmentVariable);
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
