using System;
using System.Collections.Generic;
using System.Linq;
using ProjectMannequin.Core;

namespace ProjectMannequin.LocalInput;

/// <summary>
/// Parses fighting-game command notation and resolves the best matching command
/// from a player's input buffer. This is the single grammar for the project.
///
/// Notation:
/// <list type="bullet">
/// <item>Directions use numpad notation relative to facing: 1=down-back,
/// 2=down, 3=down-forward, 4=back, 5=neutral, 6=forward, 7=up-back, 8=up,
/// 9=up-forward. Motions are read from the input history, newest to oldest.</item>
/// <item>Attack buttons are two-letter tokens: LP, MP, HP, LK, MK, HK. Legacy
/// single-letter tokens (J jump, B block, G grab, D dash, F form-swap,
/// A assist) are also accepted.</item>
/// <item>Separators '+', ',', '&gt;', and spaces are ignored, so "236LP",
/// "236 LP", and "2,3,6+LP" are equivalent. Multiple buttons in one token are
/// simultaneous (for example "MP+MK" for parry).</item>
/// </list>
///
/// Resolution rules (see <see cref="FindBestCommand"/>): candidates are ranked
/// by <c>Priority</c> descending, then by direction-step count descending
/// (motions beat normals), then by shorter notation. The first candidate whose
/// buttons and motion match the buffer wins. <c>DirectionLeniency</c> (0-2)
/// widens diagonal matching. Already-consumed presses are excluded with
/// <c>afterTick</c> so one press cannot trigger a move twice.
///
/// Not yet supported: charge motions (for example [4]6) are not parsed; charge
/// windows would be a future grammar extension and should be authored as
/// dedicated moves until then.
/// </summary>
public sealed class CommandInterpreter
{
    public CommandDefinition Parse(
        string name,
        string notation,
        int priority = 0,
        int maxFrames = 15,
        int directionLeniency = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Command name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(notation))
        {
            throw new ArgumentException("Command notation cannot be empty.", nameof(notation));
        }

        var directions = new List<int>();
        var buttons = InputButtons.None;

        var normalized = notation.Trim().ToUpperInvariant();
        for (var index = 0; index < normalized.Length;)
        {
            var raw = normalized[index];
            if (char.IsWhiteSpace(raw) || raw == '+' || raw == ',' || raw == '>')
            {
                index++;
                continue;
            }

            if (raw >= '1' && raw <= '9')
            {
                directions.Add(raw - '0');
                index++;
                continue;
            }

            if (index + 1 < normalized.Length
                && (raw is 'L' or 'M' or 'H')
                && normalized[index + 1] is 'P' or 'K')
            {
                buttons |= ParseAttackButton(normalized.Substring(index, 2));
                index += 2;
                continue;
            }

            buttons |= ParseLegacyButton(raw);
            index++;
        }

        if (directions.Count == 0 && buttons == InputButtons.None)
        {
            throw new ArgumentException($"Command '{notation}' did not contain a direction or button.");
        }

        return new CommandDefinition(
            name,
            notation,
            directions,
            buttons,
            priority,
            Math.Clamp(maxFrames, 1, GameConstants.InputBufferSize),
            Math.Clamp(directionLeniency, 0, 2));
    }

    public CommandMatch? FindBestCommand(
        IEnumerable<CommandDefinition> commands,
        PlayerInputBuffer buffer,
        bool facingRight,
        int afterTick = -1)
    {
        foreach (var command in commands
                     .OrderByDescending(command => command.Priority)
                     .ThenByDescending(command => command.DirectionSteps.Count)
                     .ThenBy(command => command.Notation.Length))
        {
            if (TryMatch(command, buffer, facingRight, afterTick, out var inputAgeFrames))
            {
                return new CommandMatch(command, inputAgeFrames);
            }
        }

        return null;
    }

    /// <summary>
    /// Debug-only: evaluates every command in ranked resolution order and reports
    /// whether each matched. Used for input-grammar readouts and tests so the
    /// consumed command and rejected higher-priority commands are inspectable.
    /// Not called on the gameplay hot path (allocates a list per call).
    /// </summary>
    public IReadOnlyList<CommandCandidate> Diagnose(
        IEnumerable<CommandDefinition> commands,
        PlayerInputBuffer buffer,
        bool facingRight,
        int afterTick = -1)
    {
        var results = new List<CommandCandidate>();
        foreach (var command in commands
                     .OrderByDescending(command => command.Priority)
                     .ThenByDescending(command => command.DirectionSteps.Count)
                     .ThenBy(command => command.Notation.Length))
        {
            var matched = TryMatch(command, buffer, facingRight, afterTick, out var inputAgeFrames);
            results.Add(new CommandCandidate(command, matched, matched ? inputAgeFrames : -1));
        }

        return results;
    }

    private static bool TryMatch(
        CommandDefinition command,
        PlayerInputBuffer buffer,
        bool facingRight,
        int afterTick,
        out int inputAgeFrames)
    {
        inputAgeFrames = -1;

        if (buffer.Count == 0)
        {
            return false;
        }

        var maxFrames = Math.Min(command.MaxFrames, buffer.Count - 1);

        for (var buttonFrameBack = 0; buttonFrameBack <= maxFrames; buttonFrameBack++)
        {
            var frame = buffer.GetFrame(buttonFrameBack);
            if (frame.Tick <= afterTick)
            {
                continue;
            }

            if (!frame.Pressed.HasAll(command.RequiredButtons))
            {
                continue;
            }

            if (command.DirectionSteps.Count == 0)
            {
                inputAgeFrames = buttonFrameBack;
                return true;
            }

            var stepIndex = command.DirectionSteps.Count - 1;
            int? lastAcceptedDirection = null;
            var requireDirectionChange = false;

            // Walk backward through the whole buffer window. This is intentionally
            // history-based so motions do not accidentally check only the current
            // direction on the button frame.
            for (var framesBack = buttonFrameBack; framesBack <= maxFrames && stepIndex >= 0; framesBack++)
            {
                var actualDirection = buffer.GetNumericDirection(framesBack, facingRight);
                var expectedDirection = command.DirectionSteps[stepIndex];

                // Special case: For single-direction "2" commands (crouch normals), 
                // check Crouch button instead of Down directional
                if (command.DirectionSteps.Count == 1 && expectedDirection == 2)
                {
                    var frameData = buffer.GetFrame(framesBack);
                    // Only Crouch button should trigger crouch normals, not Down directional
                    if (frameData.Held.HasAny(InputButtons.Crouch))
                    {
                        actualDirection = 2;
                    }
                    else
                    {
                        actualDirection = 5; // Neutral - prevent Down from matching
                    }
                }

                if (requireDirectionChange && actualDirection == lastAcceptedDirection)
                {
                    continue;
                }

                if (requireDirectionChange)
                {
                    requireDirectionChange = false;
                }

                if (DirectionMatches(actualDirection, expectedDirection, command.DirectionLeniency))
                {
                    lastAcceptedDirection = actualDirection;
                    requireDirectionChange = true;
                    stepIndex--;
                }
            }

            if (stepIndex < 0)
            {
                inputAgeFrames = buttonFrameBack;
                return true;
            }
        }

        return false;
    }

    private static InputButtons ParseAttackButton(string button)
    {
        return button switch
        {
            "LP" => InputButtons.LightPunch,
            "MP" => InputButtons.MediumPunch,
            "HP" => InputButtons.HeavyPunch,
            "LK" => InputButtons.LightKick,
            "MK" => InputButtons.MediumKick,
            "HK" => InputButtons.HeavyKick,
            _ => throw new ArgumentException($"Unknown attack button '{button}'."),
        };
    }

    private static InputButtons ParseLegacyButton(char button)
    {
        return button switch
        {
            'L' => InputButtons.LightPunch,
            'M' => InputButtons.MediumPunch,
            'H' => InputButtons.HeavyPunch,
            'J' => InputButtons.Jump,
            'B' => InputButtons.Block,
            'G' => InputButtons.Grab,
            'D' => InputButtons.Dash,
            'F' => InputButtons.FormSwap,
            'A' => InputButtons.Assist,
            _ => throw new ArgumentException($"Unknown command button '{button}'."),
        };
    }

    private static bool DirectionMatches(int actual, int expected, int leniency)
    {
        if (actual == expected)
        {
            return true;
        }

        if (leniency <= 0 || actual == 5 || expected == 5)
        {
            return false;
        }

        var actualVector = DirectionToVector(actual);
        var expectedVector = DirectionToVector(expected);

        var expectedIsCardinal = expectedVector.X == 0 || expectedVector.Y == 0;
        if (expectedIsCardinal)
        {
            return expectedVector.X != 0
                ? actualVector.X == expectedVector.X
                : actualVector.Y == expectedVector.Y;
        }

        return leniency >= 2
            && ((actualVector.X == expectedVector.X && actualVector.Y == 0)
                || (actualVector.Y == expectedVector.Y && actualVector.X == 0));
    }

    private static DirectionVector DirectionToVector(int direction)
    {
        return direction switch
        {
            1 => new DirectionVector(-1, -1),
            2 => new DirectionVector(0, -1),
            3 => new DirectionVector(1, -1),
            4 => new DirectionVector(-1, 0),
            6 => new DirectionVector(1, 0),
            7 => new DirectionVector(-1, 1),
            8 => new DirectionVector(0, 1),
            9 => new DirectionVector(1, 1),
            _ => new DirectionVector(0, 0),
        };
    }

    private readonly record struct DirectionVector(int X, int Y);
}

public sealed record CommandDefinition(
    string Name,
    string Notation,
    IReadOnlyList<int> DirectionSteps,
    InputButtons RequiredButtons,
    int Priority,
    int MaxFrames,
    int DirectionLeniency);

public readonly record struct CommandMatch(CommandDefinition Command, int InputAgeFrames);

public readonly record struct CommandCandidate(
    CommandDefinition Command,
    bool Matched,
    int InputAgeFrames);
