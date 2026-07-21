/// <summary>
/// Add a calendar period (years/months/weeks/days/hours/minutes/seconds) to
/// a zoned point in time, honoring DST — "one month later" lands on the same
/// wall-clock time next month even across a DST boundary, and if the shifted
/// wall-clock time itself falls in a DST gap/overlap, dst_resolution decides
/// how. Contrast with AddDuration, which adds exact elapsed real time
/// instead. Wraps NodaTime's <c>LocalDateTime</c> + <c>Period</c> addition
/// followed by zone resolution.
/// </summary>
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class AddPeriodNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded AddPeriodInput for this invocation.</param>
    public static ZonedInstant AddPeriod(IAxiomContext ax, AddPeriodInput input)
    {
        ax.Log().Info("AddPeriod handling");

        if (input.Period is null) return TzHelper.FailZonedInstant(TzHelper.ErrEmptyInput);

        // Resolving `zoned` itself defaults to "reject" (dst_resolution here
        // governs the *shifted* result, not the starting point).
        var (startZdt, startErr) = TzHelper.ResolveInput(input.Zoned, dstResolutionToken: null);
        if (startZdt is null) return TzHelper.FailZonedInstant(startErr);

        var mode = TzHelper.NormalizeDstResolution(input.DstResolution, defaultMode: "later");
        if (mode is null) return TzHelper.FailZonedInstant(TzHelper.ErrInvalidArgument);

        var period = new PeriodBuilder
        {
            Years = input.Period.Years,
            Months = input.Period.Months,
            Weeks = input.Period.Weeks,
            Days = input.Period.Days,
            Hours = input.Period.Hours,
            Minutes = input.Period.Minutes,
            Seconds = input.Period.Seconds,
        }.Build();

        LocalDateTime shiftedLocal;
        try
        {
            shiftedLocal = startZdt.Value.LocalDateTime + period;
        }
        catch (System.Exception)
        {
            return TzHelper.FailZonedInstant(TzHelper.ErrOutOfRange);
        }

        var zoneId = input.Zoned.ZoneId;
        var zone = TzHelper.ResolveZone(zoneId)!; // already validated by ResolveInput above
        var (resolvedZdt, resolveErr) = TzHelper.ResolveLocal(zone, shiftedLocal, mode);
        if (resolvedZdt is null) return TzHelper.FailZonedInstant(resolveErr);

        return TzHelper.ToZonedInstant(resolvedZdt.Value, zoneId);
    }
}
