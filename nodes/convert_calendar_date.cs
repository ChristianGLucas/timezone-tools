/// <summary>
/// Reinterpret a calendar date in a different calendar system — e.g.
/// 2026-07-21 (ISO/Gregorian) is 5786-11-07 in the Hebrew civil calendar and
/// 1448-02-05 in the Islamic civil calendar. Wraps NodaTime's
/// <c>LocalDate.WithCalendar</c> across its bundled non-ISO
/// <c>CalendarSystem</c>s.
/// </summary>
using System;
using Axiom;
using Gen;
using NodaTime;

namespace Nodes;

public static class ConvertCalendarDateNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded ConvertCalendarDateInput for this invocation.</param>
    public static CalendarDate ConvertCalendarDate(IAxiomContext ax, ConvertCalendarDateInput input)
    {
        ax.Log().Info("ConvertCalendarDate handling");

        var fromCal = TzHelper.ResolveCalendar(input.FromCalendar);
        if (fromCal is null) return new CalendarDate { Error = TzHelper.ErrUnknownCalendar };

        var toCal = TzHelper.ResolveCalendar(input.ToCalendar);
        if (toCal is null) return new CalendarDate { Error = TzHelper.ErrUnknownCalendar };

        LocalDate sourceDate;
        try
        {
            sourceDate = new LocalDate(input.Year, input.Month, input.Day, fromCal);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new CalendarDate { Error = TzHelper.ErrOutOfRange };
        }

        LocalDate converted;
        LocalDate isoEquivalent;
        try
        {
            converted = sourceDate.WithCalendar(toCal);
            isoEquivalent = sourceDate.WithCalendar(CalendarSystem.Iso);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new CalendarDate { Error = TzHelper.ErrOutOfRange };
        }

        return new CalendarDate
        {
            Year = converted.Year,
            Month = converted.Month,
            Day = converted.Day,
            Era = converted.Era.ToString(),
            Calendar = TzHelper.CanonicalCalendarName(input.ToCalendar),
            IsoDate = $"{isoEquivalent.Year:D4}-{isoEquivalent.Month:D2}-{isoEquivalent.Day:D2}",
            Error = "",
        };
    }
}
