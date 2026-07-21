/// <summary>
/// Every UTC-offset/DST change an IANA zone undergoes within a bounded
/// instant range — e.g. "America/New_York" across 2026 lists the
/// spring-forward transition (2026-03-08T07:00:00Z, EST-&gt;EDT) and the
/// fall-back transition (2026-11-01T06:00:00Z, EDT-&gt;EST). Walks NodaTime's
/// <c>DateTimeZone.GetZoneInterval</c> chain.
/// </summary>
using System;
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class ListDstTransitionsNode
{
    private const int MaxYearsSpan = 100;
    private const int MaxTransitions = 500;

    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded ListDstTransitionsInput for this invocation.</param>
    public static ListDstTransitionsResult ListDstTransitions(IAxiomContext ax, ListDstTransitionsInput input)
    {
        ax.Log().Info("ListDstTransitions handling");

        if (TzHelper.IsBlank(input.ZoneId) || TzHelper.IsBlank(input.StartUtc) || TzHelper.IsBlank(input.EndUtc))
        {
            return new ListDstTransitionsResult { Error = TzHelper.ErrEmptyInput };
        }

        var zone = TzHelper.ResolveZone(input.ZoneId);
        if (zone is null) return new ListDstTransitionsResult { Error = TzHelper.ErrUnknownZone };

        if (!TzHelper.TryParseInstant(input.StartUtc, out var start) || !TzHelper.TryParseInstant(input.EndUtc, out var end))
        {
            return new ListDstTransitionsResult { Error = TzHelper.ErrInvalidArgument };
        }

        if (end <= start) return new ListDstTransitionsResult { Error = TzHelper.ErrInvalidArgument };

        // Bound the walk on the RAW input before doing any work: reject a span
        // longer than MaxYearsSpan outright rather than silently truncating —
        // a caller who actually wants the full range should page it themselves.
        var cappedEnd = SafeAddYears(start, MaxYearsSpan);
        if (end > cappedEnd) return new ListDstTransitionsResult { Error = TzHelper.ErrRangeTooLarge };

        var result = new ListDstTransitionsResult();
        var cursor = start;
        while (true)
        {
            var interval = zone.GetZoneInterval(cursor);
            if (!interval.HasEnd || interval.End >= end)
            {
                break;
            }

            var nextInterval = zone.GetZoneInterval(interval.End);
            result.Transitions.Add(new DstTransition
            {
                InstantUtc = TzHelper.FormatInstant(interval.End),
                OffsetBeforeSeconds = interval.WallOffset.Seconds,
                OffsetAfterSeconds = nextInterval.WallOffset.Seconds,
                IsDstAfter = nextInterval.Savings != Offset.Zero,
                AbbreviationBefore = interval.Name,
                AbbreviationAfter = nextInterval.Name,
            });

            if (result.Transitions.Count >= MaxTransitions)
            {
                result.Truncated = true;
                break;
            }

            cursor = interval.End;
        }

        return result;
    }

    // Instant has no direct "add calendar years" operation (it is a pure
    // timeline point) — approximate the 100-year cap generously via whole
    // days so it never rejects a span that is actually within bounds.
    private static Instant SafeAddYears(Instant from, int years)
    {
        try
        {
            return from + Duration.FromDays(years * 366L);
        }
        catch (OverflowException)
        {
            return Instant.MaxValue;
        }
    }
}
