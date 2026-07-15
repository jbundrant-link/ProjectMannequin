using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.Stage;

namespace ProjectMannequin.Progression;

public enum StageRank
{
    D,
    C,
    B,
    A,
    S,
}

public sealed record StageResultsData(
    string MissionId,
    int StageNumber,
    int CombatScore,
    int ClearBonus,
    int TimeBonus,
    int StageTotal,
    int RunTotal,
    int ActiveFrames,
    int MaxCombo,
    int EnemiesDefeated,
    int Parries,
    int CounterHits,
    int DamageTaken,
    int Deaths,
    StageRank Rank)
{
    public float ClearTimeSeconds => ActiveFrames / (float)GameConstants.TickRate;
}

/// <summary>
/// Pure deterministic scoring accumulator. It consumes authoritative combat
/// events once per simulation tick and never changes hit resolution or AI.
/// </summary>
public sealed class RunScoreManager
{
    private int _lastCapturedTick = -1;
    private bool _stageFinalized;

    public int RunScore { get; private set; }
    public int StageCombatScore { get; private set; }
    public int ActiveGameplayFrames { get; private set; }
    public int MaxCombo { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public int Parries { get; private set; }
    public int CounterHits { get; private set; }
    public int DamageTaken { get; private set; }
    public int Deaths { get; private set; }
    public StageResultsData? LastStageResults { get; private set; }

    public void ResetRun()
    {
        RunScore = 0;
        BeginStage();
    }

    public void RestoreRunScore(int score)
    {
        RunScore = System.Math.Max(0, score);
    }

    public void BeginStage()
    {
        _lastCapturedTick = -1;
        _stageFinalized = false;
        StageCombatScore = 0;
        ActiveGameplayFrames = 0;
        MaxCombo = 0;
        EnemiesDefeated = 0;
        Parries = 0;
        CounterHits = 0;
        DamageTaken = 0;
        Deaths = 0;
        LastStageResults = null;
    }

    public void AdvanceGameplayFrame(ArcadeStageState state)
    {
        if (state is ArcadeStageState.Traveling
            or ArcadeStageState.EncounterActive
            or ArcadeStageState.Intermission)
        {
            ActiveGameplayFrames++;
        }
    }

    public void AwardPickupScore(int points)
    {
        AddScore(points);
        RunSessionManager.Instance.RunScore = RunScore;
    }

    public void CaptureEvents(
        int tick,
        IReadOnlyCollection<CombatPresentationEvent> events,
        IReadOnlyCollection<CombatActor> actors,
        StageMissionData? mission,
        RunSessionManager session,
        ICollection<CombatPresentationEvent> outputEvents)
    {
        if (tick == _lastCapturedTick)
        {
            return;
        }

        _lastCapturedTick = tick;
        var scoreBefore = RunScore;
        foreach (var presentationEvent in events.ToArray())
        {
            switch (presentationEvent.Type)
            {
                case CombatPresentationEventType.HitConnected:
                case CombatPresentationEventType.LauncherHit:
                case CombatPresentationEventType.CounterHit:
                case CombatPresentationEventType.PunishCounter:
                    if (actors.FirstOrDefault(actor => actor.ActorId == presentationEvent.SourceActorId)
                        is { IsPlayerControlled: true } attacker)
                    {
                        var damage = ParseDamage(presentationEvent.Payload);
                        var comboMultiplier = 1.0f + System.Math.Min(attacker.ComboHitCount, 20) * 0.045f;
                        var styleBonus = presentationEvent.Type switch
                        {
                            CombatPresentationEventType.PunishCounter => 400,
                            CombatPresentationEventType.CounterHit => 220,
                            CombatPresentationEventType.LauncherHit => 140,
                            _ => 0,
                        };
                        AddScore(System.Math.Max(10, (int)(damage * 0.08f * comboMultiplier)) + styleBonus);
                        if (presentationEvent.Type is CombatPresentationEventType.CounterHit
                            or CombatPresentationEventType.PunishCounter)
                        {
                            CounterHits++;
                        }
                    }

                    if (actors.FirstOrDefault(actor => actor.ActorId == presentationEvent.TargetActorId)
                        is { IsPlayerControlled: true })
                    {
                        DamageTaken += ParseDamage(presentationEvent.Payload);
                    }
                    break;
                case CombatPresentationEventType.ComboUpdated:
                    if (int.TryParse(presentationEvent.Payload, out var combo))
                    {
                        MaxCombo = System.Math.Max(MaxCombo, combo);
                    }
                    break;
                case CombatPresentationEventType.Parried:
                    if (actors.FirstOrDefault(actor => actor.ActorId == presentationEvent.SourceActorId)
                        is { IsPlayerControlled: true })
                    {
                        Parries++;
                        AddScore(presentationEvent.Payload == "perfect" ? 600 : 300);
                    }
                    break;
                case CombatPresentationEventType.ActorDefeated:
                    var defeated = actors.FirstOrDefault(actor => actor.ActorId == presentationEvent.SourceActorId);
                    if (defeated is not null && !defeated.IsPlayerControlled)
                    {
                        EnemiesDefeated++;
                        AddScore(defeated.IsBoss ? 8000 : defeated.IsElite ? 3500 : 750);
                    }
                    break;
                case CombatPresentationEventType.PlayerLifeLost:
                    Deaths++;
                    break;
                case CombatPresentationEventType.StageCompleted:
                    if (mission is not null && !_stageFinalized)
                    {
                        FinalizeStage(mission);
                        outputEvents.Add(new CombatPresentationEvent(
                            CombatPresentationEventType.StageResultsReady,
                            tick,
                            presentationEvent.SourceActorId,
                            Payload: LastStageResults!.Rank.ToString()));
                    }
                    break;
            }
        }

        while (RunScore >= (session.ExtraLifeThresholdIndex + 1) * 25000)
        {
            session.ExtraLifeThresholdIndex++;
            session.RemainingLives++;
            outputEvents.Add(new CombatPresentationEvent(
                CombatPresentationEventType.ExtraLifeAwarded,
                tick,
                "score",
                Payload: session.RemainingLives.ToString(CultureInfo.InvariantCulture)));
        }

        session.RunScore = RunScore;
        if (RunScore != scoreBefore)
        {
            outputEvents.Add(new CombatPresentationEvent(
                CombatPresentationEventType.ScoreChanged,
                tick,
                "score",
                Payload: RunScore.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private void FinalizeStage(StageMissionData mission)
    {
        _stageFinalized = true;
        var clearBonus = mission.IsFinalStage ? 10000 : 3500;
        var clearSeconds = ActiveGameplayFrames / (float)GameConstants.TickRate;
        var timeBonus = System.Math.Max(
            0,
            (int)((mission.ParTimeSeconds - clearSeconds) * 45.0f));
        var combatScore = StageCombatScore;
        var stageTotal = combatScore + clearBonus + timeBonus;
        AddScore(clearBonus + timeBonus);
        var rank = stageTotal >= mission.RankScoreS ? StageRank.S
            : stageTotal >= mission.RankScoreA ? StageRank.A
            : stageTotal >= mission.RankScoreB ? StageRank.B
            : stageTotal >= mission.RankScoreC ? StageRank.C
            : StageRank.D;
        LastStageResults = new StageResultsData(
            mission.Id,
            mission.StageNumber,
            combatScore,
            clearBonus,
            timeBonus,
            stageTotal,
            RunScore,
            ActiveGameplayFrames,
            MaxCombo,
            EnemiesDefeated,
            Parries,
            CounterHits,
            DamageTaken,
            Deaths,
            rank);
            MvpProgressStore.RecordStageResult(mission, LastStageResults);
    }

    private void AddScore(int value)
    {
        var positive = System.Math.Max(0, value);
        StageCombatScore += positive;
        RunScore += positive;
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
}
