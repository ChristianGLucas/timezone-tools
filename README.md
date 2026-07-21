# timezone-tools

Composable **time-zone conversion and calendar-system** nodes for the
[Axiom](https://axiomide.com) marketplace, published as
`christiangeorgelucas/timezone-tools`. Exact IANA time-zone conversion, UTC
offset/DST lookup, DST-transition listing, timeline duration vs. calendar
period arithmetic, DST-aware period/duration addition, ISO/Gregorian/Julian/
Hebrew/Islamic/Persian/Coptic/Badi calendar-system conversion, and
explicit-pattern parse/format — entirely offline, deterministic, and
stateless.

Distinct from natural-language date parsing (parses human phrases like "next
Friday") and RRULE recurrence (repeating-event rules) — this package is exact
timezone/calendar *arithmetic* over typed instants and civil date-times.

Written in **C#**, wrapping [NodaTime](https://nodatime.org/) (Apache-2.0,
Jon Skeet's .NET date/time library), which bundles its own compiled IANA
TZDB — no separate timezone-data package or OS tzdata dependency.

## Nodes

| Node | Input → Output | Purpose |
|---|---|---|
| `ConvertZone` | `ConvertZoneInput` → `ZonedInstant` | Reinterpret a point in time in a different IANA zone, preserving the instant |
| `GetOffset` | `GetOffsetInput` → `ZonedInstant` | UTC offset + DST status of a zone at an instant |
| `ListDstTransitions` | `ListDstTransitionsInput` → `ListDstTransitionsResult` | Every DST/offset change a zone undergoes in a bounded range |
| `GetDuration` | `GetDurationInput` → `ElapsedDuration` | Exact elapsed timeline duration between two instants (not calendar-aware) |
| `GetPeriod` | `GetPeriodInput` → `CalendarPeriod` | Calendar-aware difference (years/months/weeks/days/hours/...) between two civil date-times |
| `AddPeriod` | `AddPeriodInput` → `ZonedInstant` | Add a calendar period to a zoned point in time, honoring DST |
| `AddDuration` | `AddDurationInput` → `ZonedInstant` | Add exact elapsed real time to a zoned point in time, honoring DST |
| `ConvertCalendarDate` | `ConvertCalendarDateInput` → `CalendarDate` | Reinterpret a date in a different calendar system |
| `GetCalendarInfo` | `CalendarInfoInput` → `CalendarInfo` | Leap-year status, month count, and month lengths for a calendar system + year |
| `FormatDateTime` | `FormatDateTimeInput` → `FormatResult` | Render a zoned point in time with an explicit NodaTime pattern |
| `ParseDateTime` | `ParseDateTimeInput` → `ZonedInstant` | Parse text into a zoned point in time with an explicit NodaTime pattern |

## The canonical envelope: `ZonedInstant`

Every node that resolves or moves a point in time both accepts and emits
`ZonedInstant` — `instant_utc` (RFC 3339 UTC), `zone_id` (IANA zone id),
`local_date_time` (ISO wall-clock reading), `offset_seconds`, `offset_iso`,
`is_dst`, `zone_abbreviation`, and `error`. As input, set exactly one of
`instant_utc` (authoritative) or `local_date_time` (resolved against
`zone_id`, with DST gaps/overlaps handled per each node's `dst_resolution`
field: `"reject"` returns a structured error, `"earlier"`/`"later"` pick a
resolution). As output, every field is populated.

Every node returns a consistent structured-error contract: on failure the
success fields are empty/zero and `error` carries a stable token
(`EMPTY_INPUT`, `INVALID_ARGUMENT`, `UNKNOWN_ZONE`, `AMBIGUOUS_LOCAL_TIME`,
`SKIPPED_LOCAL_TIME`, `RANGE_TOO_LARGE`, `UNKNOWN_CALENDAR`, `OUT_OF_RANGE`,
`INVALID_PATTERN`, `INTERNAL_ERROR`) — never a crash.

## `AddPeriod` vs `AddDuration`: the classic DST gotcha

Both add "one day" but can produce different wall-clock results across a DST
boundary. `AddPeriod` with `{days: 1}` adds a **calendar** day (the same
wall-clock time tomorrow, forward-shifted if that lands in a gap).
`AddDuration` with `seconds: 86400` adds **24 real hours** on the timeline.
Starting at `2026-03-07T02:30:00` in `America/New_York` (the day before the
2026 spring-forward transition), `AddPeriod` lands at `03:30` the next day
(forward-shifted through the missing hour), while `AddDuration` lands at
`01:30` the next day (24 real hours later, still before the transition
instant) — same nominal "one day," different real-world answer.

## Calendar systems

`ConvertCalendarDate` and `GetCalendarInfo` support `Iso`, `Gregorian`,
`Julian`, `Coptic`, `Hebrew` (civil or scriptural month numbering),
`IslamicCivil`, `Persian` (simple/arithmetic), and `Badi`, via NodaTime's
bundled `CalendarSystem` implementations.

## Determinism & licensing

Every node is a pure, stateless, single-input/single-output function — no
database, filesystem persistence, session state, or external network calls.
Wraps [NodaTime](https://www.nuget.org/packages/NodaTime) 3.3.3 (Apache-2.0),
which for a `net8.0` target has zero further dependencies.

Built for the Axiom marketplace. MIT licensed — see `LICENSE`.
