/// <summary>
/// The calendar-aware difference between two civil date-times — years,
/// months, weeks, days, hours, minutes, seconds — as a human would count
/// them (unlike GetDuration, which is exact elapsed time). E.g.
/// 2020-01-15T10:00:00 to 2026-07-21T08:30:00 is 6 years, 6 months, 5 days,
/// 22 hours, 30 minutes. Wraps NodaTime's <c>Period.Between</c>.
/// </summary>
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class GetPeriodNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded GetPeriodInput for this invocation.</param>
    public static CalendarPeriod GetPeriod(IAxiomContext ax, GetPeriodInput input)
    {
        ax.Log().Info("GetPeriod handling");

        var (startZdt, startErr) = TzHelper.ResolveInput(input.Start, dstResolutionToken: null);
        if (startZdt is null) return new CalendarPeriod { Error = startErr };

        var (endZdt, endErr) = TzHelper.ResolveInput(input.End, dstResolutionToken: null);
        if (endZdt is null) return new CalendarPeriod { Error = endErr };

        var startLocal = startZdt.Value.LocalDateTime;
        var endLocal = endZdt.Value.LocalDateTime;
        var negative = endLocal < startLocal;

        // Explicit units (including Weeks, which NodaTime's default overload
        // omits — Period.Between(start, end) alone would always report
        // Weeks=0 even when the remainder is >= 7 days) so every field this
        // message advertises is genuinely computed, not a stub constant.
        const PeriodUnits units = PeriodUnits.Years | PeriodUnits.Months | PeriodUnits.Weeks
            | PeriodUnits.Days | PeriodUnits.Hours | PeriodUnits.Minutes | PeriodUnits.Seconds;
        var period = negative ? Period.Between(endLocal, startLocal, units) : Period.Between(startLocal, endLocal, units);

        return new CalendarPeriod
        {
            Years = period.Years,
            Months = period.Months,
            Weeks = period.Weeks,
            Days = period.Days,
            // Period stores the sub-day components as `long` (it can also
            // represent much larger normalized spans); our requested units
            // bound Hours to < 24, Minutes/Seconds to < 60, so the narrowing
            // to the proto's int32 fields is always safe here.
            Hours = (int)period.Hours,
            Minutes = (int)period.Minutes,
            Seconds = (int)period.Seconds,
            Negative = negative,
            Error = "",
        };
    }
}
