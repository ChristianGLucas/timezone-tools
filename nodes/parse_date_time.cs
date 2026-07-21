/// <summary>
/// Parse text into a zoned point in time with an explicit NodaTime pattern —
/// never locale-guessing natural-language parsing (that is a different
/// package's job). If the pattern includes no zone specifier (`z`/`x`), the
/// zone_id input supplies it (default "UTC"). Wraps NodaTime's
/// <c>ZonedDateTimePattern</c>, always built with
/// <c>CultureInfo.InvariantCulture</c>.
/// </summary>
using Axiom;
using Gen;
using NodaTime;
using NodaTime.Text;

namespace Nodes;

public static class ParseDateTimeNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded ParseDateTimeInput for this invocation.</param>
    public static ZonedInstant ParseDateTime(IAxiomContext ax, ParseDateTimeInput input)
    {
        ax.Log().Info("ParseDateTime handling");

        if (TzHelper.IsBlank(input.Text) || TzHelper.IsBlank(input.Pattern))
        {
            return TzHelper.FailZonedInstant(TzHelper.ErrEmptyInput);
        }

        var fallbackZoneId = TzHelper.IsBlank(input.ZoneId) ? "UTC" : input.ZoneId;
        var fallbackZone = TzHelper.ResolveZone(fallbackZoneId);
        if (fallbackZone is null) return TzHelper.FailZonedInstant(TzHelper.ErrUnknownZone);

        ZonedDateTimePattern pattern;
        try
        {
            pattern = ZonedDateTimePattern
                .CreateWithInvariantCulture(input.Pattern, TzHelper.Tzdb)
                .WithTemplateValue(new Instant().InZone(fallbackZone));
        }
        catch (InvalidPatternException)
        {
            return TzHelper.FailZonedInstant(TzHelper.ErrInvalidPattern);
        }

        var result = pattern.Parse(input.Text);
        if (!result.Success) return TzHelper.FailZonedInstant(TzHelper.ErrInvalidArgument);

        var zdt = result.Value;
        return TzHelper.ToZonedInstant(zdt, zdt.Zone.Id);
    }
}
