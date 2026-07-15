using System;
using System.Collections.Generic;
using ProjectMannequin.Combat;

namespace ProjectMannequin.Progression;

public class RunSessionManager
{
    private static RunSessionManager? _instance;

    public static RunSessionManager Instance => _instance ??= new RunSessionManager();

    public bool HasActiveRun => !string.IsNullOrWhiteSpace(CurrentWorldId);

    // The world the current run is taking place in
    public string CurrentWorldId { get; set; } = "";

    // The player's remaining TMNT-style lives
    public int RemainingLives { get; set; } = 3;

    // Move Cards acquired during this run
    public List<string> EquippedMoveCards { get; set; } = new();

    // Artifacts/Modifiers acquired during this run
    public List<string> ActiveArtifacts { get; set; } = new();

    // The index of the stage they are currently on within the world
    public int CurrentStageIndex { get; set; } = 0;

    public int PlayerHealth { get; private set; } = -1;
    public int PlayerMeter { get; private set; }
    public string CurrentFormId { get; private set; } = "blank_mannequin";
    public List<string> EquippedFormIds { get; private set; } = new();
    public int RunScore { get; set; }
    public int ExtraLifeThresholdIndex { get; set; }
    public RunStageCheckpoint? StageCheckpoint { get; private set; }
    public RunScoreManager ScoreManager { get; } = new();

    public void StartNewRun(string worldId)
    {
        CurrentWorldId = worldId;
        RemainingLives = 3;
        CurrentStageIndex = 0;
        PlayerHealth = -1;
        PlayerMeter = 0;
        CurrentFormId = "blank_mannequin";
        EquippedFormIds.Clear();
        RunScore = 0;
        ExtraLifeThresholdIndex = 0;
        StageCheckpoint = null;
        ScoreManager.ResetRun();
        
        // For move persistence: we clear some, but potentially keep some base ones depending on meta-progression
        // Currently starting with an empty list for the run session
        EquippedMoveCards.Clear();
        ActiveArtifacts.Clear();
        if (!MvpProgressStore.IsPersistenceDisabled)
        {
            RunSaveStore.Delete();
        }
    }

    public bool TryLoadCommittedRun()
    {
        if (HasActiveRun)
        {
            return true;
        }

        if (MvpProgressStore.IsPersistenceDisabled)
        {
            return false;
        }

        if (!RunSaveStore.TryLoad(out var checkpoint))
        {
            return false;
        }

        StageCheckpoint = CloneCheckpoint(checkpoint);
        RestoreStageCheckpoint();
        return true;
    }

    public void EnsureStageEntryCheckpoint(CombatActor player)
    {
        if (!HasActiveRun)
        {
            return;
        }

        if (StageCheckpoint is not null
            && StageCheckpoint.WorldId == CurrentWorldId
            && StageCheckpoint.StageIndex == CurrentStageIndex)
        {
            ApplyToPlayer(player);
            return;
        }

        CapturePlayerState(player);
        CommitCurrentStateAsStageEntry();
    }

    public void CapturePlayerState(CombatActor player)
    {
        PlayerHealth = player.Health;
        PlayerMeter = player.Meter;
        CurrentFormId = player.CurrentForm.Id;
        EquippedFormIds = new List<string>(player.FormArchive.ActiveLoadout);
    }

    public void ApplyToPlayer(CombatActor player)
    {
        if (!HasActiveRun)
        {
            return;
        }

        if (EquippedFormIds.Count > 0)
        {
            player.FormArchive.SetActiveLoadout(EquippedFormIds);
        }

        var selectedForm = player.FormArchive.GetForm(CurrentFormId);
        if (selectedForm is not null)
        {
            player.SetForm(selectedForm, resetHealth: false);
        }

        if (PlayerHealth >= 0)
        {
            player.HydrateRunResources(PlayerHealth, PlayerMeter);
        }
    }

    public bool AdvanceToNextStage(CombatActor player, int stageCount)
    {
        if (!HasActiveRun || CurrentStageIndex + 1 >= stageCount)
        {
            return false;
        }

        CapturePlayerState(player);
        CurrentStageIndex++;
        CommitCurrentStateAsStageEntry();
        return true;
    }

    public void RestoreStageCheckpoint()
    {
        if (StageCheckpoint is null)
        {
            return;
        }

        CurrentWorldId = StageCheckpoint.WorldId;
        CurrentStageIndex = StageCheckpoint.StageIndex;
        RemainingLives = StageCheckpoint.RemainingLives;
        PlayerHealth = StageCheckpoint.PlayerHealth;
        PlayerMeter = StageCheckpoint.PlayerMeter;
        CurrentFormId = StageCheckpoint.CurrentFormId;
        EquippedFormIds = new List<string>(StageCheckpoint.EquippedFormIds);
        EquippedMoveCards = new List<string>(StageCheckpoint.EquippedMoveCards);
        ActiveArtifacts = new List<string>(StageCheckpoint.ActiveArtifacts);
        RunScore = StageCheckpoint.RunScore;
        ExtraLifeThresholdIndex = StageCheckpoint.ExtraLifeThresholdIndex;
        ScoreManager.RestoreRunScore(RunScore);
    }

    public void CompleteRun()
    {
        StageCheckpoint = null;
        CurrentWorldId = "";
        CurrentStageIndex = 0;
        if (!MvpProgressStore.IsPersistenceDisabled)
        {
            RunSaveStore.Delete();
        }
    }

    private void CommitCurrentStateAsStageEntry()
    {
        RunScore = ScoreManager.RunScore;
        StageCheckpoint = new RunStageCheckpoint
        {
            WorldId = CurrentWorldId,
            StageIndex = CurrentStageIndex,
            RemainingLives = RemainingLives,
            PlayerHealth = PlayerHealth,
            PlayerMeter = PlayerMeter,
            CurrentFormId = CurrentFormId,
            EquippedFormIds = new List<string>(EquippedFormIds),
            EquippedMoveCards = new List<string>(EquippedMoveCards),
            ActiveArtifacts = new List<string>(ActiveArtifacts),
            RunScore = RunScore,
            ExtraLifeThresholdIndex = ExtraLifeThresholdIndex,
        };
        if (!MvpProgressStore.IsPersistenceDisabled)
        {
            RunSaveStore.Save(StageCheckpoint);
        }
    }

    private static RunStageCheckpoint CloneCheckpoint(RunStageCheckpoint checkpoint)
    {
        return new RunStageCheckpoint
        {
            SchemaVersion = checkpoint.SchemaVersion,
            WorldId = checkpoint.WorldId,
            StageIndex = checkpoint.StageIndex,
            RemainingLives = checkpoint.RemainingLives,
            PlayerHealth = checkpoint.PlayerHealth,
            PlayerMeter = checkpoint.PlayerMeter,
            CurrentFormId = checkpoint.CurrentFormId,
            EquippedFormIds = new List<string>(checkpoint.EquippedFormIds),
            EquippedMoveCards = new List<string>(checkpoint.EquippedMoveCards),
            ActiveArtifacts = new List<string>(checkpoint.ActiveArtifacts),
            RunScore = checkpoint.RunScore,
            ExtraLifeThresholdIndex = checkpoint.ExtraLifeThresholdIndex,
        };
    }

    public void LoseLife()
    {
        if (RemainingLives > 0)
        {
            RemainingLives--;
        }
    }

    /// <summary>
    /// Run-wide damage bonus (percent) from Move Card grid adjacency synergies.
    /// </summary>
    public int GetSynergyDamageBonusPercent()
    {
        if (EquippedMoveCards.Count == 0)
        {
            return 0;
        }

        return MoveCardSynergyRules.Evaluate(BuildGridCells()).BonusPercent;
    }

    /// <summary>The names of the currently active grid synergies, for the HUD.</summary>
    public List<string> GetActiveSynergyNames()
    {
        return MoveCardSynergyRules.Evaluate(BuildGridCells())
            .Synergies.ConvertAll(synergy => synergy.Name);
    }

    private List<MoveCardType> BuildGridCells()
    {
        var cells = new List<MoveCardType>();
        foreach (var cardId in EquippedMoveCards)
        {
            var card = MoveCardCatalog.GetCard(cardId);
            if (card is not null)
            {
                cells.Add(card.CardType);
            }
        }

        return cells;
    }
}
