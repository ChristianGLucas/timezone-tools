/// <summary>
/// The exact elapsed timeline duration between two instants — NOT
/// calendar-aware (use GetPeriod for years/months/days). E.g.
/// 2026-01-15T12:00:00Z to 2026-07-15T12:00:00Z is exactly 181 days, 0h 0m
/// 0s. Wraps NodaTime's <c>Instant</c> subtraction, which returns a
/// <c>Duration</c>.
/// </summary>
using System;
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class GetDurationNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded GetDurationInput for this invocation.</param>
    public static ElapsedDuration GetDuration(IAxiomContext ax, GetDurationInput input)
    {
        ax.Log().Info("GetDuration handling");

        if (TzHelper.IsBlank(input.StartUtc) || TzHelper.IsBlank(input.EndUtc))
        {
            return new ElapsedDuration { Error = TzHelper.ErrEmptyInput };
        }

        if (!TzHelper.TryParseInstant(input.StartUtc, out var start) || !TzHelper.TryParseInstant(input.EndUtc, out var end))
        {
            return new ElapsedDuration { Error = TzHelper.ErrInvalidArgument };
        }

        var dur = end - start;
        var negative = dur < Duration.Zero;

        // Days/Hours/Minutes/Seconds/SubsecondNanoseconds are a signed,
        // consistent decomposition (all share the same sign as the overall
        // duration) — take the magnitude of each and report the sign once,
        // via `negative`, so every field below is always non-negative.
        var absDays = Math.Abs(dur.Days);
        var absHours = Math.Abs(dur.Hours);
        var absMinutes = Math.Abs(dur.Minutes);
        var absSeconds = Math.Abs(dur.Seconds);
        var absSubsecondNanos = Math.Abs(dur.SubsecondNanoseconds);
        var absMilliseconds = absSubsecondNanos / 1_000_000;

        var magnitudeNanos = ((((long)absDays * 24 + absHours) * 60 + absMinutes) * 60 + absSeconds) * 1_000_000_000L + absSubsecondNanos;

        return new ElapsedDuration
        {
            TotalNanoseconds = negative ? -magnitudeNanos : magnitudeNanos,
            TotalSeconds = dur.TotalSeconds,
            Days = absDays,
            Hours = absHours,
            Minutes = absMinutes,
            Seconds = absSeconds,
            Milliseconds = absMilliseconds,
            Negative = negative,
            Error = "",
        };
    }
}
