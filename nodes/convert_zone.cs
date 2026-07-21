/// <summary>
/// Reinterpret a point in time in a different IANA time zone, preserving the
/// underlying instant — e.g. "2026-07-21T12:00:00Z" viewed from
/// "America/New_York" reads as 08:00 EDT (UTC-4); the same instant viewed
/// from "Asia/Tokyo" reads as 21:00 JST (UTC+9). Wraps NodaTime's
/// <c>ZonedDateTime.WithZone</c>.
/// </summary>
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class ConvertZoneNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded ConvertZoneInput for this invocation.</param>
    public static ZonedInstant ConvertZone(IAxiomContext ax, ConvertZoneInput input)
    {
        ax.Log().Info("ConvertZone handling");

        var (zdt, err) = TzHelper.ResolveInput(input.Source, input.DstResolution);
        if (zdt is null) return TzHelper.FailZonedInstant(err);

        if (TzHelper.IsBlank(input.TargetZoneId)) return TzHelper.FailZonedInstant(TzHelper.ErrEmptyInput);
        var targetZone = TzHelper.ResolveZone(input.TargetZoneId);
        if (targetZone is null) return TzHelper.FailZonedInstant(TzHelper.ErrUnknownZone);

        var converted = zdt.Value.WithZone(targetZone);
        return TzHelper.ToZonedInstant(converted, input.TargetZoneId);
    }
}
