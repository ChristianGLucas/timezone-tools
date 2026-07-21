// TESTS — delete this block when done ─────────────────────────────────────────
// Tests are required to publish this package. The publish pipeline runs your
// tests as a quality gate — a package will not be published if tests fail or
// do not meet the minimum requirements.
//
// Requirements checked before publishing:
//   - At least one test per node
//   - All tests must pass
//   - Output fields must be meaningfully asserted — not just null-checked
//
// The generated test below is a starting point. Replace the TODO comment with
// real assertions that verify your node returns correct data for known inputs.
// Think: given a specific input, what should the output fields contain?
//
// Run your tests locally at any time:
//   axiom test

using Axiom;
using Gen;
using System.Collections.Generic;
using Xunit;

namespace Nodes;

public class ParseDateTimeTest
{
    // A no-op AxiomContext a node author edits to drive a specific scenario.
    // Reflection exposes an empty graph, mutation is a sink. Implement only
    // what your assertions need.
    private sealed class TestContext : IAxiomContext
    {
        public IAxiomContext.ILogger Log() => new NoopLog();
        public IAxiomContext.ISecrets Secrets() => new NoopSecrets();
        public string ExecutionId() => "test-execution-id";
        public string FlowId() => "test-flow-id";
        public string TenantId() => "test-tenant-id";
        public IAxiomContext.IReflection Reflection() => new NoopReflection();
        public IAxiomContext.IMutation Mutation() => new NoopMutation();

        private sealed class NoopLog : IAxiomContext.ILogger
        {
            public void Debug(string m, IDictionary<string, string>? a = null) {}
            public void Info(string m, IDictionary<string, string>? a = null) {}
            public void Warn(string m, IDictionary<string, string>? a = null) {}
            public void Error(string m, IDictionary<string, string>? a = null) {}
        }
        private sealed class NoopSecrets : IAxiomContext.ISecrets
        {
            public (string Value, bool Found) Get(string name) => ("", false);
            public IAxiomContext.SecretStatus Status(string name) => IAxiomContext.SecretStatus.Unset;
        }
        private sealed class NoopReflection : IAxiomContext.IReflection
        {
            public IAxiomContext.IFlowReflection Flow() => new NoopFlow();
            private sealed class NoopFlow : IAxiomContext.IFlowReflection
            {
                public IReadOnlyList<IAxiomContext.ReflectionNode> Nodes() => new List<IAxiomContext.ReflectionNode>();
                public IReadOnlyList<IAxiomContext.ReflectionEdge> Edges() => new List<IAxiomContext.ReflectionEdge>();
                public IReadOnlyList<IAxiomContext.ReflectionEdge> LoopEdges() => new List<IAxiomContext.ReflectionEdge>();
                public IAxiomContext.FlowPosition Position() => new IAxiomContext.FlowPosition(0, 0, new Dictionary<int, int>(), new List<string>());
                public string GraphId() => "";
            }
        }
        private sealed class NoopMutation : IAxiomContext.IMutation
        {
            public IAxiomContext.IFlowMutation Flow() => new FlowMut();
            private sealed class FlowMut : IAxiomContext.IFlowMutation
            {
                public int AddNode(string pkg, string ver, IAxiomContext.CanvasPosition? pos) => 0;
                public void AddEdge(int src, int dst, IAxiomContext.EdgeCondition? cond) {}
            }
        }
    }

    [Fact]
    public void TestParseDateTime_TextWithZoneToken_ParsesToExactInstant()
    {
        IAxiomContext ax = new TestContext();
        var input = new ParseDateTimeInput
        {
            Text = "2026-07-15T08:00:00 -04:00 America/New_York",
            Pattern = "uuuu-MM-dd'T'HH:mm:ss o<+HH:mm> z",
        };
        var result = ParseDateTimeNode.ParseDateTime(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-07-15T12:00:00Z", result.InstantUtc);
        Assert.Equal("America/New_York", result.ZoneId);
        Assert.Equal(-14400, result.OffsetSeconds);
    }

    // Round trip: FormatDateTime's own output text is exactly what
    // ParseDateTime accepts back, with the same pattern, proving the two
    // nodes are genuine inverses for a shared pattern.
    [Fact]
    public void TestParseDateTime_RoundTripsWithFormatDateTime()
    {
        IAxiomContext ax = new TestContext();
        const string pattern = "uuuu-MM-dd'T'HH:mm:ss o<+HH:mm> z";
        var original = new ZonedInstant { InstantUtc = "2026-11-01T05:30:00Z", ZoneId = "America/New_York" };

        var formatted = FormatDateTimeNode.FormatDateTime(ax, new FormatDateTimeInput { Zoned = original, Pattern = pattern });
        Assert.Equal("", formatted.Error);

        var reparsed = ParseDateTimeNode.ParseDateTime(ax, new ParseDateTimeInput { Text = formatted.Text, Pattern = pattern });
        Assert.Equal("", reparsed.Error);
        Assert.Equal(original.InstantUtc, reparsed.InstantUtc);
        Assert.Equal(original.ZoneId, reparsed.ZoneId);
    }

    // When the pattern has no zone specifier, zone_id supplies the fallback
    // zone used to interpret the parsed local wall-clock reading.
    [Fact]
    public void TestParseDateTime_NoZoneInPattern_UsesZoneIdFallback()
    {
        IAxiomContext ax = new TestContext();
        var input = new ParseDateTimeInput
        {
            Text = "2026-07-15 08:00:00",
            Pattern = "uuuu-MM-dd HH:mm:ss",
            ZoneId = "America/New_York",
        };
        var result = ParseDateTimeNode.ParseDateTime(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-07-15T12:00:00Z", result.InstantUtc);
        Assert.Equal("America/New_York", result.ZoneId);
    }

    // With no zone_id given and no zone in the pattern, defaults to UTC.
    [Fact]
    public void TestParseDateTime_NoZoneAnywhere_DefaultsToUtc()
    {
        IAxiomContext ax = new TestContext();
        var input = new ParseDateTimeInput
        {
            Text = "2026-07-15 08:00:00",
            Pattern = "uuuu-MM-dd HH:mm:ss",
        };
        var result = ParseDateTimeNode.ParseDateTime(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("UTC", result.ZoneId);
        Assert.Equal("2026-07-15T08:00:00Z", result.InstantUtc);
    }

    [Fact]
    public void TestParseDateTime_TextDoesNotMatchPattern_ReturnsStructuredErrorNotCrash()
    {
        IAxiomContext ax = new TestContext();
        var input = new ParseDateTimeInput { Text = "not a date at all", Pattern = "uuuu-MM-dd HH:mm:ss" };
        var result = ParseDateTimeNode.ParseDateTime(ax, input);
        Assert.Equal("INVALID_ARGUMENT", result.Error);
    }

    [Fact]
    public void TestParseDateTime_EmptyInput_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var result = ParseDateTimeNode.ParseDateTime(ax, new ParseDateTimeInput());
        Assert.Equal("EMPTY_INPUT", result.Error);
    }
}
