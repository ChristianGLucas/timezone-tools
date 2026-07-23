// Shared helper logic for the timezone-tools node handlers.
//
// This is NOT a node — it is plain shared source under nodes/ that every node
// handler calls into: parsing/formatting always through explicit NodaTime
// pattern APIs with CultureInfo.InvariantCulture (never the ambient/system
// culture — Axiom compiles C# with InvariantGlobalization=true, and NodaTime
// bundles its own IANA TZDB so timezone data itself is unaffected, but text
// formatting must still never depend on ambient culture), a uniform
// structured-error convention, and the ZonedInstant <-> ZonedDateTime bridge
// shared by every node that resolves or moves a point in time.
using System;
using System.Globalization;
using Gen;
using NodaTime;
using NodaTime.Calendars;
using NodaTime.Text;
using NodaTime.TimeZones;

namespace Nodes;

internal static class TzHelper
{
    // Stable structured-error tokens returned in every message's `error` field.
    public const string ErrEmptyInput = "EMPTY_INPUT";
    public const string ErrInvalidArgument = "INVALID_ARGUMENT";
    public const string ErrUnknownZone = "UNKNOWN_ZONE";
    public const string ErrAmbiguousLocalTime = "AMBIGUOUS_LOCAL_TIME";
    public const string ErrSkippedLocalTime = "SKIPPED_LOCAL_TIME";
    public const string ErrUnknownCalendar = "UNKNOWN_CALENDAR";
    public const string ErrOutOfRange = "OUT_OF_RANGE";
    public const string ErrInvalidPattern = "INVALID_PATTERN";
    public const string ErrInternal = "INTERNAL_ERROR";

    // NodaTime's bundled IANA TZDB provider — no network/filesystem access,
    // no separate data package, and independent of the OS's own tzdata.
    public static readonly IDateTimeZoneProvider Tzdb = DateTimeZoneProviders.Tzdb;

    private static readonly InstantPattern InstantFmt = InstantPattern.ExtendedIso;
    private static readonly LocalDateTimePattern LocalFmt = LocalDateTimePattern.ExtendedIso;

    public static bool IsBlank(string? s) => string.IsNullOrWhiteSpace(s);

    public static DateTimeZone? ResolveZone(string? zoneId)
    {
        if (IsBlank(zoneId)) return null;
        return Tzdb.GetZoneOrNull(zoneId!);
    }

    public static bool TryParseInstant(string text, out Instant instant)
    {
        var result = InstantFmt.Parse(text);
        if (result.Success)
        {
            instant = result.Value;
            return true;
        }
        instant = default;
        return false;
    }

    public static bool TryParseLocalDateTime(string text, out LocalDateTime ldt)
    {
        var result = LocalFmt.Parse(text);
        if (result.Success)
        {
            ldt = result.Value;
            return true;
        }
        ldt = default;
        return false;
    }

    public static string FormatInstant(Instant i) => InstantFmt.Format(i);

    public static string FormatLocalDateTime(LocalDateTime ldt) => LocalFmt.Format(ldt);

    // Always renders a fixed-width "+HH:mm" (or "+HH:mm:ss" for the rare
    // historical sub-minute offset) instead of NodaTime's own default Offset
    // formatting, which truncates trailing zero components (e.g. "-05"
    // instead of "-05:00") — inconsistent for a machine-readable field.
    public static string FormatOffsetIso(Offset offset)
    {
        var totalSeconds = offset.Seconds;
        var sign = totalSeconds < 0 ? "-" : "+";
        var abs = Math.Abs(totalSeconds);
        var hh = abs / 3600;
        var mm = (abs % 3600) / 60;
        var ss = abs % 60;
        return ss == 0
            ? $"{sign}{hh:D2}:{mm:D2}"
            : $"{sign}{hh:D2}:{mm:D2}:{ss:D2}";
    }

    // Builds the full OUTPUT ZonedInstant from a resolved ZonedDateTime.
    public static ZonedInstant ToZonedInstant(ZonedDateTime zdt, string zoneId)
    {
        var interval = zdt.GetZoneInterval();
        return new ZonedInstant
        {
            InstantUtc = FormatInstant(zdt.ToInstant()),
            ZoneId = zoneId,
            LocalDateTime = FormatLocalDateTime(zdt.LocalDateTime),
            OffsetSeconds = zdt.Offset.Seconds,
            OffsetIso = FormatOffsetIso(zdt.Offset),
            IsDst = interval.Savings != Offset.Zero,
            ZoneAbbreviation = interval.Name,
            Error = "",
        };
    }

    public static ZonedInstant FailZonedInstant(string code) => new ZonedInstant { Error = code };

    // Resolves a `dst_resolution` token ("" / "reject" / "earlier" / "later")
    // to a normalized, validated mode, or null if the token is unrecognized.
    // `defaultMode` is used only when the token is empty.
    public static string? NormalizeDstResolution(string? token, string defaultMode = "reject")
    {
        var mode = IsBlank(token) ? defaultMode : token!.Trim().ToLowerInvariant();
        return mode is "reject" or "earlier" or "later" ? mode : null;
    }

    // Resolves a wall-clock LocalDateTime against a zone per `mode`
    // ("reject"/"earlier"/"later" — see ConvertZoneInput.dst_resolution).
    // A skipped (DST-gap) local time has no "earlier" reading (it never
    // legally existed), so both "earlier" and "later" resolve a gap the same
    // conventional way: forward-shifted to the first valid instant after it.
    // Only the ambiguous (DST-overlap) case distinguishes the two offsets.
    public static (ZonedDateTime? Zdt, string Error) ResolveLocal(DateTimeZone zone, LocalDateTime ldt, string mode)
    {
        if (mode == "reject")
        {
            try
            {
                return (zone.AtStrictly(ldt), "");
            }
            catch (SkippedTimeException)
            {
                return (null, ErrSkippedLocalTime);
            }
            catch (AmbiguousTimeException)
            {
                return (null, ErrAmbiguousLocalTime);
            }
        }

        var ambiguousResolver = mode == "earlier" ? Resolvers.ReturnEarlier : Resolvers.ReturnLater;
        var resolver = Resolvers.CreateMappingResolver(ambiguousResolver, Resolvers.ReturnForwardShifted);
        return (zone.ResolveLocal(ldt, resolver), "");
    }

    // Resolves a ZonedInstant INPUT (instant_utc authoritative, else
    // local_date_time+zone_id) to a NodaTime ZonedDateTime.
    public static (ZonedDateTime? Zdt, string Error) ResolveInput(ZonedInstant? src, string? dstResolutionToken, string defaultMode = "reject")
    {
        if (src is null) return (null, ErrEmptyInput);

        var zone = ResolveZone(src.ZoneId);
        if (zone is null) return (null, ErrUnknownZone);

        if (!IsBlank(src.InstantUtc))
        {
            if (!TryParseInstant(src.InstantUtc, out var instant)) return (null, ErrInvalidArgument);
            return (instant.InZone(zone), "");
        }

        if (!IsBlank(src.LocalDateTime))
        {
            var mode = NormalizeDstResolution(dstResolutionToken, defaultMode);
            if (mode is null) return (null, ErrInvalidArgument);
            if (!TryParseLocalDateTime(src.LocalDateTime, out var ldt)) return (null, ErrInvalidArgument);
            return ResolveLocal(zone, ldt, mode);
        }

        return (null, ErrEmptyInput);
    }

    // The calendar-system vocabulary shared by ConvertCalendarDate and
    // GetCalendarInfo. Case-insensitive; empty defaults to ISO. Returns null
    // for an unrecognized token.
    public static CalendarSystem? ResolveCalendar(string? name)
    {
        var key = IsBlank(name) ? "iso" : name!.Trim().ToLowerInvariant();
        return key switch
        {
            "iso" => CalendarSystem.Iso,
            "gregorian" => CalendarSystem.Gregorian,
            "julian" => CalendarSystem.Julian,
            "coptic" => CalendarSystem.Coptic,
            "hebrew" or "hebrewcivil" => CalendarSystem.HebrewCivil,
            "hebrewscriptural" => CalendarSystem.HebrewScriptural,
            "islamic" or "islamiccivil" => CalendarSystem.GetIslamicCalendar(IslamicLeapYearPattern.Base15, IslamicEpoch.Civil),
            "persian" or "persiansimple" => CalendarSystem.PersianSimple,
            "badi" => CalendarSystem.Badi,
            _ => null,
        };
    }

    // The canonical spelling of a resolved calendar token, IN OUR OWN input
    // vocabulary (never NodaTime's own CalendarSystem.Id, e.g. "Hebrew
    // Civil" or "Hijri Civil-Base15") — so a node's output `calendar` field
    // is always itself a valid `from_calendar`/`to_calendar`/`calendar`
    // input to any other node in this package (composability: an edge can
    // pipe ConvertCalendarDate.calendar straight into GetCalendarInfo.calendar
    // with no translation). Must be kept in exact 1:1 sync with
    // ResolveCalendar above. Returns "" for an unrecognized token (callers
    // only reach this after ResolveCalendar already succeeded).
    public static string CanonicalCalendarName(string? name)
    {
        var key = IsBlank(name) ? "iso" : name!.Trim().ToLowerInvariant();
        return key switch
        {
            "iso" => "Iso",
            "gregorian" => "Gregorian",
            "julian" => "Julian",
            "coptic" => "Coptic",
            "hebrew" or "hebrewcivil" => "HebrewCivil",
            "hebrewscriptural" => "HebrewScriptural",
            "islamic" or "islamiccivil" => "IslamicCivil",
            "persian" or "persiansimple" => "PersianSimple",
            "badi" => "Badi",
            _ => "",
        };
    }
}
