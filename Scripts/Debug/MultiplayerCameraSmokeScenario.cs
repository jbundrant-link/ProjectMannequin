using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

public sealed class MultiplayerCameraSmokeScenario
{
    private readonly GameSimulation _simulation;
    private bool _sawCatchUpStart;
    private bool _sawHardCorrection;
    private bool _sawCatchUpComplete;
    private bool _summaryPrinted;
    private bool _separationInjected;
    private float _maximumCorrectedSeparation;

    public MultiplayerCameraSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
    }

    public void UpdateBeforeSimulation(int tick, ArcadeEncounterDirector director)
    {
        if (_separationInjected || tick < 2)
        {
            return;
        }

        var players = _simulation.Actors
            .Where(actor => actor.IsPlayerControlled && !actor.IsDead)
            .OrderBy(actor => actor.PlayerId)
            .ToArray();
        if (players.Length < 2)
        {
            return;
        }

        _separationInjected = true;
        players[0].SimPosition = new Vector3(
            director.Mission.StageMinX + 11.5f,
            0.0f,
            0.6f);
        players[1].SimPosition = new Vector3(
            director.Mission.StageMinX - 5.0f,
            0.0f,
            -0.6f);
    }

    public void CaptureAfterSimulation(
        int tick,
        ArcadeEncounterDirector director,
        IEnumerable<CombatPresentationEvent> events)
    {
        foreach (var presentationEvent in events)
        {
            _sawCatchUpStart |=
                presentationEvent.Type == CombatPresentationEventType.PlayerCatchUpStarted;
            _sawHardCorrection |=
                presentationEvent.Type == CombatPresentationEventType.PlayerCatchUpWarped;
            _sawCatchUpComplete |=
                presentationEvent.Type == CombatPresentationEventType.PlayerCatchUpCompleted;
        }

        var players = _simulation.Actors
            .Where(actor => actor.IsPlayerControlled && !actor.IsDead)
            .ToArray();
        if (players.Length >= 2)
        {
            var separation = players.Max(actor => actor.SimPosition.X)
                - players.Min(actor => actor.SimPosition.X);
            _maximumCorrectedSeparation = Mathf.Max(
                _maximumCorrectedSeparation,
                separation);
        }

        if (_summaryPrinted || tick < 90)
        {
            return;
        }

        _summaryPrinted = true;
        var passed = players.Length == 2
            && _sawCatchUpStart
            && _sawHardCorrection
            && _sawCatchUpComplete
            && _maximumCorrectedSeparation <= director.Mission.PartyHardSeparationX + 0.01f;
        GD.Print(
            $"[CameraSmoke] TETHER passed={passed} players={players.Length} "
            + $"start={_sawCatchUpStart} hard={_sawHardCorrection} "
            + $"complete={_sawCatchUpComplete} "
            + $"maxSeparation={_maximumCorrectedSeparation:0.00}");
        if (!passed)
        {
            GD.PushError("[CameraSmoke] Multiplayer party tether assertions failed.");
        }
    }
}
