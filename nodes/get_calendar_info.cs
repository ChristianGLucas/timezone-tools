/// <summary>
/// Structural facts about a calendar system for a given year — leap-year
/// status, month count, and each month's day count, which can vary by
/// calendar and (for lunisolar calendars like Hebrew) by year: a Hebrew leap
/// year has 13 months instead of 12. Wraps NodaTime's <c>CalendarSystem</c>
/// query methods.
/// </summary>
using System;
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class GetCalendarInfoNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded CalendarInfoInput for this invocation.</param>
    public static CalendarInfo GetCalendarInfo(IAxiomContext ax, CalendarInfoInput input)
    {
        ax.Log().Info("GetCalendarInfo handling");

        var cal = TzHelper.ResolveCalendar(input.Calendar);
        if (cal is null) return new CalendarInfo { Error = TzHelper.ErrUnknownCalendar };

        if (input.Year < cal.MinYear || input.Year > cal.MaxYear)
        {
            return new CalendarInfo { Error = TzHelper.ErrOutOfRange };
        }

        try
        {
            var isLeap = cal.IsLeapYear(input.Year);
            var monthsInYear = cal.GetMonthsInYear(input.Year);

            var info = new CalendarInfo
            {
                IsLeapYear = isLeap,
                MonthsInYear = monthsInYear,
                MinYear = cal.MinYear,
                MaxYear = cal.MaxYear,
                Error = "",
            };
            for (var month = 1; month <= monthsInYear; month++)
            {
                info.DaysInMonth.Add(cal.GetDaysInMonth(input.Year, month));
            }

            return info;
        }
        catch (ArgumentOutOfRangeException)
        {
            return new CalendarInfo { Error = TzHelper.ErrOutOfRange };
        }
    }
}
