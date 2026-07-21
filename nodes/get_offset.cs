/// <summary>
/// The UTC offset and DST status of an IANA time zone at a specific instant
/// — e.g. "America/New_York" at 2026-01-15T12:00:00Z is UTC-5 (EST, no DST);
/// at 2026-07-15T12:00:00Z it is UTC-4 (EDT, DST in effect). Wraps NodaTime's
/// <c>Instant.InZone</c> + <c>ZoneInterval</c>.
/// </summary>
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class GetOffsetNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded GetOffsetInput for this invocation.</param>
    public static ZonedInstant GetOffset(IAxiomContext ax, GetOffsetInput input)
    {
        ax.Log().Info("GetOffset handling");

        if (TzHelper.IsBlank(input.InstantUtc) || TzHelper.IsBlank(input.ZoneId))
        {
            return TzHelper.FailZonedInstant(TzHelper.ErrEmptyInput);
        }

        var zone = TzHelper.ResolveZone(input.ZoneId);
        if (zone is null) return TzHelper.FailZonedInstant(TzHelper.ErrUnknownZone);

        if (!TzHelper.TryParseInstant(input.InstantUtc, out var instant))
        {
            return TzHelper.FailZonedInstant(TzHelper.ErrInvalidArgument);
        }

        return TzHelper.ToZonedInstant(instant.InZone(zone), input.ZoneId);
    }
}
