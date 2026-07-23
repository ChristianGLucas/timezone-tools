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

            cursor = interval.End;
        }

        return result;
    }
}
