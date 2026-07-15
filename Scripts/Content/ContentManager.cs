using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Data;

namespace ProjectMannequin.Content;

public partial class ContentManager : Node
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly Dictionary<string, CharacterData> _loadedCharacters = new();

    public IReadOnlyDictionary<string, CharacterData> LoadedCharacters => _loadedCharacters;
    public ContentValidationReport LastReport { get; private set; } = new();

    public override void _Ready()
    {
        ScanUserContent();
    }

    public ContentValidationReport ScanUserContent()
    {
        var report = new ContentValidationReport();
        _loadedCharacters.Clear();

        foreach (var root in GetContentRoots())
        {
            var characterRoot = Path.Combine(root, "Characters");
            if (!Directory.Exists(characterRoot))
            {
                continue;
            }

            foreach (var folder in Directory.EnumerateDirectories(characterRoot))
            {
                TryLoadCharacterFolder(folder, report);
            }
        }

        LastReport = report;
        if (report.Issues.Count > 0)
        {
            GD.PushWarning(report.ToSummary());
        }

        return report;
    }

    private void TryLoadCharacterFolder(string folder, ContentValidationReport report)
    {
        var contentId = Path.GetFileName(folder);
        var characterPath = Path.Combine(folder, "character.json");
        var movesetPath = Path.Combine(folder, "moveset.json");

        if (!File.Exists(characterPath))
        {
            report.AddError(contentId, "Missing file: character.json");
            return;
        }

        if (!File.Exists(movesetPath))
        {
            report.AddError(contentId, "Missing file: moveset.json");
            return;
        }

        try
        {
            var character = JsonSerializer.Deserialize<CharacterData>(File.ReadAllText(characterPath), JsonOptions);
            var moveset = JsonSerializer.Deserialize<MovesetFile>(File.ReadAllText(movesetPath), JsonOptions);

            if (character is null)
            {
                report.AddError(contentId, "character.json did not produce a character.");
                return;
            }

            if (moveset is null)
            {
                report.AddError(contentId, "moveset.json did not produce a moveset.");
                return;
            }

            character.Moves = moveset.Moves ?? new List<MoveData>();
            NormalizeCharacter(character);

            var issuesBeforeValidation = report.Issues.Count;
            ValidateCharacter(character, contentId, report);

            var hasNewError = report.Issues
                .Skip(issuesBeforeValidation)
                .Any(issue => issue.Severity == ContentValidationSeverity.Error);

            if (hasNewError)
            {
                return;
            }

            _loadedCharacters[character.Id] = character;
        }
        catch (JsonException exception)
        {
            report.AddError(contentId, $"Invalid JSON: {exception.Message}");
        }
        catch (IOException exception)
        {
            report.AddError(contentId, $"Could not read content: {exception.Message}");
        }
    }

    private void ValidateCharacter(CharacterData character, string contentId, ContentValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(character.Id))
        {
            report.AddError(contentId, "Character id is required.");
        }

        if (_loadedCharacters.ContainsKey(character.Id))
        {
            report.AddError(contentId, $"Duplicate character id: {character.Id}");
        }

        if (character.MaxHealth <= 0 || character.MaxHealth > 10000)
        {
            report.AddError(contentId, "MaxHealth must be between 1 and 10000.");
        }

        if (character.MaxGuardGauge < 0
            || character.GuardBreakFrames < 1
            || character.GuardRecoveryDelayFrames < 0
            || character.GuardRecoveryPerSecond < 0.0f)
        {
            report.AddError(contentId, "Guard gauge and recovery settings are invalid.");
        }

        if (character.Moves.Count == 0)
        {
            report.AddError(contentId, "Character must define at least one move.");
        }

        ValidateBox(character.Hurtbox, contentId, "hurtbox", report);
        ValidateBox(character.Pushbox, contentId, "pushbox", report);
        ValidateBox(character.CrouchingHurtbox, contentId, "crouching hurtbox", report);
        ValidateBox(character.CrouchingPushbox, contentId, "crouching pushbox", report);

        var moveIds = new HashSet<string>();
        foreach (var move in character.Moves)
        {
            ValidateMove(move, contentId, moveIds, report);
        }

        // Richer Castagne-style authoring validation (invalid cancel targets,
        // infinite-combo risk, proration range, impossible hitbox windows) is
        // surfaced as warnings so bad move data fails loudly during loading.
        foreach (var issue in MoveValidation.ValidateForm(character))
        {
            report.AddWarning(contentId, issue);
        }

        ValidateBossPhases(character.BossPhases, contentId, moveIds, report);
        ValidateCpuProfile(character.CpuProfile, contentId, report);
        ValidateArcadeEnemyProfile(character.ArcadeEnemyProfile, contentId, report);
    }

    private static void ValidateMove(
        MoveData move,
        string contentId,
        HashSet<string> moveIds,
        ContentValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(move.Id))
        {
            report.AddError(contentId, "Move id is required.");
            return;
        }

        if (!moveIds.Add(move.Id))
        {
            report.AddError(contentId, $"Duplicate move id: {move.Id}");
        }

        if (move.StartupFrames < 0 || move.ActiveFrames < 0 || move.RecoveryFrames < 0)
        {
            report.AddError(contentId, $"Invalid move {move.Id}: frame counts cannot be negative.");
        }

        if (move.Damage < 0)
        {
            report.AddError(contentId, $"Invalid move {move.Id}: damage cannot be negative.");
        }

        if (move.GuardDamage < 0
            || move.SuperFreezeFrames < 0
            || move.InvulnerableStartupFrames < 0
            || move.InvulnerableStartupFrames > move.TotalFrames)
        {
            report.AddError(contentId, $"Invalid move {move.Id}: guard damage, super freeze, or invulnerability timing is invalid.");
        }

        if (move.MinimumDamageScale is < 0.0f or > 1.0f)
        {
            report.AddError(contentId, $"Invalid move {move.Id}: minimum damage scale must be between 0 and 1.");
        }

        if ((move.JumpCancelStartFrame < 0) != (move.JumpCancelEndFrame < 0)
            || move.JumpCancelEndFrame < move.JumpCancelStartFrame
            || move.JumpCancelEndFrame >= move.TotalFrames)
        {
            report.AddError(contentId, $"Invalid move {move.Id}: jump-cancel window is outside the move timeline.");
        }

        foreach (var box in move.CombatBoxes)
        {
            ValidateBox(box, contentId, $"move {move.Id} box {box.Id}", report);
        }
    }

    private static void ValidateBox(
        CombatBoxDefinition box,
        string contentId,
        string label,
        ContentValidationReport report)
    {
        if (box.SizeX <= 0.0f || box.SizeY <= 0.0f || box.SizeZ <= 0.0f)
        {
            report.AddError(contentId, $"Invalid {label}: box dimensions must be positive.");
        }

        if (box.StartFrame > box.EndFrame)
        {
            report.AddError(contentId, $"Invalid {label}: StartFrame cannot be after EndFrame.");
        }
    }

    private static void ValidateBossPhases(
        IEnumerable<BossPhaseData> phases,
        string contentId,
        IReadOnlySet<string> moveIds,
        ContentValidationReport report)
    {
        foreach (var phase in phases)
        {
            if (string.IsNullOrWhiteSpace(phase.Id))
            {
                report.AddError(contentId, "Boss phase id is required.");
            }

            if (phase.HealthThreshold is < 0.0f or > 1.0f)
            {
                report.AddError(contentId, $"Boss phase {phase.Id}: HealthThreshold must be between 0 and 1.");
            }

            foreach (var moveId in phase.EnabledMoveIds)
            {
                if (!moveIds.Contains(moveId))
                {
                    report.AddError(contentId, $"Boss phase {phase.Id}: unknown move {moveId}.");
                }
            }
        }
    }

    private static void ValidateCpuProfile(
        CpuFighterProfileData? profile,
        string contentId,
        ContentValidationReport report)
    {
        if (profile is null)
        {
            return;
        }

        if (profile.ReactionFrames < 0 || profile.DecisionIntervalFrames < 1 || profile.GuardHoldFrames < 1)
        {
            report.AddError(contentId, "CPU frame settings must be non-negative and decision/guard frames must be at least 1.");
        }

        if (profile.PreferredRangeMin < 0.0f || profile.PreferredRangeMax < profile.PreferredRangeMin)
        {
            report.AddError(contentId, "CPU preferred range is invalid.");
        }

        var chances = new[]
        {
            profile.Aggression,
            profile.GuardChance,
            profile.AntiAirChance,
            profile.PunishChance,
            profile.RetreatChance,
            profile.JumpEvadeChance,
            profile.MistakeChance,
        };
        if (chances.Any(chance => chance is < 0.0f or > 1.0f))
        {
            report.AddError(contentId, "CPU probabilities must be between 0 and 1.");
        }
    }

    private static void ValidateArcadeEnemyProfile(
        ArcadeEnemyProfileData? profile,
        string contentId,
        ContentValidationReport report)
    {
        if (profile is null)
        {
            return;
        }

        if (profile.AttackRange <= 0.0f
            || profile.PositionTolerance <= 0.0f
            || profile.LaneTolerance <= 0.0f
            || profile.RetreatDistance <= profile.AttackRange)
        {
            report.AddError(contentId, "Arcade enemy spacing values are invalid.");
        }

        if (profile.RetreatFrames < 1
            || profile.ReengageDelayFrames < 1
            || profile.ApproachSpeedMultiplier <= 0.0f
            || profile.RetreatSpeedMultiplier <= 0.0f)
        {
            report.AddError(contentId, "Arcade enemy timing and speed values must be positive.");
        }
    }

    private static void NormalizeCharacter(CharacterData character)
    {
        character.RoleTags ??= new List<string>();
        character.SynergyTags ??= new List<string>();
        character.Moves ??= new List<MoveData>();
        character.BossPhases ??= new List<BossPhaseData>();
        character.Hurtbox ??= new CombatBoxDefinition { Id = "standing_hurtbox", BoxType = CombatBoxType.Hurtbox };
        character.Pushbox ??= new CombatBoxDefinition { Id = "standing_pushbox", BoxType = CombatBoxType.Pushbox };
        character.CrouchingHurtbox ??= new CombatBoxDefinition
        {
            Id = "crouching_hurtbox",
            BoxType = CombatBoxType.Hurtbox,
            OffsetY = 0.72f,
            SizeY = 1.42f,
        };
        character.CrouchingPushbox ??= new CombatBoxDefinition
        {
            Id = "crouching_pushbox",
            BoxType = CombatBoxType.Pushbox,
            OffsetY = 0.68f,
            SizeY = 1.34f,
        };

        foreach (var move in character.Moves)
        {
            move.CancelIntoMoveIds ??= new List<string>();
            move.CancelTags ??= new List<string>();
            move.Tags ??= new List<string>();
            move.CombatBoxes ??= new List<CombatBoxDefinition>();
            move.ProjectileSpawns ??= new List<ProjectileSpawnData>();
        }
    }

    private static IEnumerable<string> GetContentRoots()
    {
        yield return ProjectSettings.GlobalizePath("res://UserContent");
        yield return ProjectSettings.GlobalizePath("user://UserContent");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class MovesetFile
    {
        public List<MoveData> Moves { get; set; } = new();
    }
}

public sealed class ContentValidationReport
{
    public List<ContentValidationIssue> Issues { get; } = new();

    public void AddError(string contentId, string message)
    {
        Issues.Add(new ContentValidationIssue(contentId, ContentValidationSeverity.Error, message));
    }

    public void AddWarning(string contentId, string message)
    {
        Issues.Add(new ContentValidationIssue(contentId, ContentValidationSeverity.Warning, message));
    }

    public string ToSummary()
    {
        if (Issues.Count == 0)
        {
            return "Content validation passed.";
        }

        return string.Join(System.Environment.NewLine, Issues.Select(issue => $"{issue.ContentId}: {issue.Severity}: {issue.Message}"));
    }
}

public sealed record ContentValidationIssue(
    string ContentId,
    ContentValidationSeverity Severity,
    string Message);

public enum ContentValidationSeverity
{
    Warning,
    Error,
}
