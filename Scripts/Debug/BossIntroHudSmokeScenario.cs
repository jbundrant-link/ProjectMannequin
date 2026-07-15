using System.Collections.Generic;
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
        var passed = _sawCutscene
            && _sawStarted
            && _sawReady
            && _sawFight
            && _hudHiddenDuringIntro
            && _readyMessageVisible
            && _fightMessageVisible
            && _barsVisibleAfterReady;
        GD.Print(
            $"[BossIntroHudSmoke] SUMMARY passed={passed} "
            + $"cutscene={_sawCutscene} started={_sawStarted} ready={_sawReady} fight={_sawFight} "
            + $"hidden={_hudHiddenDuringIntro} readyMsg={_readyMessageVisible} "
            + $"fightMsg={_fightMessageVisible} bars={_barsVisibleAfterReady} "
            + $"opacity={_hud!.HudOpacity:0.00} phase={_sequencer!.Phase}");
        if (!passed)
        {
            GD.PushError("[BossIntroHudSmoke] Full final-boss intro/HUD lifecycle regressed.");
        }

        GetTree().Quit();
    }
}
