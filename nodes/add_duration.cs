/// <summary>
/// Add an exact elapsed duration (real seconds, NOT calendar units) to a
/// zoned point in time, then reproject onto the same zone — e.g. 24 real
/// hours added just before a spring-forward transition lands on a different
/// wall-clock time than AddPeriod's "+1 day" would, because a 24-hour day
/// and a calendar day are not always the same length across a DST boundary.
/// Wraps NodaTime's <c>Instant</c> + <c>Duration</c> addition.
/// </summary>
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class AddDurationNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded AddDurationInput for this invocation.</param>
    public static ZonedInstant AddDuration(IAxiomContext ax, AddDurationInput input)
    {
        ax.Log().Info("AddDuration handling");

        var (startZdt, startErr) = TzHelper.ResolveInput(input.Zoned, dstResolutionToken: null);
        if (startZdt is null) return TzHelper.FailZonedInstant(startErr);

        if (double.IsNaN(input.Seconds) || double.IsInfinity(input.Seconds))
        {
            return TzHelper.FailZonedInstant(TzHelper.ErrInvalidArgument);
        }

        Instant shifted;
        try
        {
            shifted = startZdt.Value.ToInstant() + Duration.FromSeconds(input.Seconds);
        }
        catch (System.Exception)
        {
            return TzHelper.FailZonedInstant(TzHelper.ErrOutOfRange);
        }

        var zoneId = input.Zoned.ZoneId;
        var zone = TzHelper.ResolveZone(zoneId)!; // already validated by ResolveInput above
        return TzHelper.ToZonedInstant(shifted.InZone(zone), zoneId);
    }
}
