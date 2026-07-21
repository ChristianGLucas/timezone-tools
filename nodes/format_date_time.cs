/// <summary>
/// Render a zoned point in time with an explicit NodaTime pattern — never
/// the ambient/system culture. E.g. pattern "uuuu-MM-dd HH:mm:ss o&lt;+HH:mm&gt;
/// z" on 2026-07-15T12:00:00Z in "America/New_York" renders
/// "2026-07-15 08:00:00 -04:00 America/New_York". Wraps NodaTime's
/// <c>ZonedDateTimePattern</c>, always built with
/// <c>CultureInfo.InvariantCulture</c>.
/// </summary>
using Axiom;
using Gen;
using NodaTime;
using NodaTime.Text;

namespace Nodes;

public static class FormatDateTimeNode
{
    /// <param name="ax">The AxiomContext: logging, secrets, reflection, mutation.</param>
    /// <param name="input">The decoded FormatDateTimeInput for this invocation.</param>
    public static FormatResult FormatDateTime(IAxiomContext ax, FormatDateTimeInput input)
    {
        ax.Log().Info("FormatDateTime handling");

        if (TzHelper.IsBlank(input.Pattern)) return new FormatResult { Error = TzHelper.ErrEmptyInput };

        var (zdt, err) = TzHelper.ResolveInput(input.Zoned, dstResolutionToken: null);
        if (zdt is null) return new FormatResult { Error = err };

        ZonedDateTimePattern pattern;
        try
        {
            // CreateWithInvariantCulture: the pattern is compiled against
            // CultureInfo.InvariantCulture explicitly, never the process's
            // ambient culture (which InvariantGlobalization pins to
            // invariant anyway, but we never want this to depend on that).
            pattern = ZonedDateTimePattern.CreateWithInvariantCulture(input.Pattern, TzHelper.Tzdb);
        }
        catch (InvalidPatternException)
        {
            return new FormatResult { Error = TzHelper.ErrInvalidPattern };
        }

        try
        {
            return new FormatResult { Text = pattern.Format(zdt.Value), Error = "" };
        }
        catch (System.Exception)
        {
            return new FormatResult { Error = TzHelper.ErrInternal };
        }
    }
}
