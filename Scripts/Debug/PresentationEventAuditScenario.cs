using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// A passive acceptance observer: it drives nothing, it only censuses the
/// presentation event stream so an actual boss fight can confirm the Phase 5
/// events (Reflect Guard, Phase Burst, Rush Throw) and the core hit/block/parry
/// beats really fire at runtime. Enable it alongside any boss smoke with
/// PROJECT_MANNEQUIN_ACCEPTANCE_SMOKE_TEST=1.
/// </summary>
public sealed class PresentationEventAuditScenario
{
    private const int SummaryTick = 520;

    private static readonly CombatPresentationEventType[] Phase5Events =
    {
        CombatPresentationEventType.ReflectGuard,
        CombatPresentationEventType.PhaseBurst,
        CombatPresentationEventType.RushThrow,
    };

    private readonly Dictionary<CombatPresentationEventType, int> _counts = new();
    private bool _summaryPrinted;

    public void CaptureAfterSimulation(int tick, IReadOnlyCollection<CombatPresentationEvent> events)
    {
        foreach (var presentationEvent in events)
        {
            var firstSighting = !_counts.ContainsKey(presentationEvent.Type);
            _counts[presentationEvent.Type] = _counts.GetValueOrDefault(presentationEvent.Type) + 1;

            if (firstSighting && Phase5Events.Contains(presentationEvent.Type))
            {
                GD.Print(
                    $"[EventAudit] tick {tick}: Phase-5 event {presentationEvent.Type} fired "
                    + $"({presentationEvent.SourceActorId} '{presentationEvent.Payload}')");
            }
        }

        if (tick < SummaryTick || _summaryPrinted)
        {
            return;
        }

        _summaryPrinted = true;
        var fired = Phase5Events.Where(_counts.ContainsKey).Select(type => type.ToString()).ToList();
        var coreBeats = new[]
        {
            CombatPresentationEventType.HitConnected,
            CombatPresentationEventType.Blocked,
            CombatPresentationEventType.Parried,
        };
        var coreSeen = coreBeats.Count(_counts.ContainsKey);
        GD.Print(
            $"[EventAudit] SUMMARY distinctTypes={_counts.Count} totalEvents={_counts.Values.Sum()} "
            + $"coreBeats={coreSeen}/{coreBeats.Length} phase5Fired=[{string.Join(",", fired)}]");
    }
}
