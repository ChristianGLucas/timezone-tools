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

public class ConvertZoneTest
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

    // Independent oracle: America/New_York is a documented, well-known UTC-4
    // (EDT) offset in July (verified independently against the IANA tzdata
    // "America/New_York" rules, not derived from this node's own code).
    [Fact]
    public void TestConvertZone_UtcToNewYorkSummer_MatchesKnownOffset()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertZoneInput
        {
            Source = new ZonedInstant { InstantUtc = "2026-07-15T12:00:00Z", ZoneId = "UTC" },
            TargetZoneId = "America/New_York",
        };
        var result = ConvertZoneNode.ConvertZone(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-07-15T12:00:00Z", result.InstantUtc);
        Assert.Equal("2026-07-15T08:00:00", result.LocalDateTime);
        Assert.Equal(-14400, result.OffsetSeconds);
        Assert.Equal("-04:00", result.OffsetIso);
        Assert.True(result.IsDst);
        Assert.Equal("EDT", result.ZoneAbbreviation);
    }

    // Same zone, winter: UTC-5 (EST), no DST — the classic contrast pair.
    [Fact]
    public void TestConvertZone_UtcToNewYorkWinter_MatchesKnownOffset()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertZoneInput
        {
            Source = new ZonedInstant { InstantUtc = "2026-01-15T12:00:00Z", ZoneId = "UTC" },
            TargetZoneId = "America/New_York",
        };
        var result = ConvertZoneNode.ConvertZone(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-01-15T07:00:00", result.LocalDateTime);
        Assert.Equal(-18000, result.OffsetSeconds);
        Assert.Equal("-05:00", result.OffsetIso);
        Assert.False(result.IsDst);
        Assert.Equal("EST", result.ZoneAbbreviation);
    }

    // A skipped local time (US spring-forward gap, 2026-03-08 02:00-03:00
    // local does not exist in America/New_York) must be rejected by default,
    // not silently guessed at.
    [Fact]
    public void TestConvertZone_SkippedLocalTime_RejectsByDefault()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertZoneInput
        {
            Source = new ZonedInstant { LocalDateTime = "2026-03-08T02:30:00", ZoneId = "America/New_York" },
            TargetZoneId = "UTC",
        };
        var result = ConvertZoneNode.ConvertZone(ax, input);
        Assert.Equal("SKIPPED_LOCAL_TIME", result.Error);
    }

    // With dst_resolution=later, the same skipped local time resolves
    // forward-shifted to the first valid instant after the gap.
    [Fact]
    public void TestConvertZone_SkippedLocalTime_LaterResolvesForward()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertZoneInput
        {
            Source = new ZonedInstant { LocalDateTime = "2026-03-08T02:30:00", ZoneId = "America/New_York" },
            TargetZoneId = "UTC",
            DstResolution = "later",
        };
        var result = ConvertZoneNode.ConvertZone(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-03-08T07:30:00Z", result.InstantUtc);
    }

    // An ambiguous local time (US fall-back overlap, 2026-11-01 01:30 local
    // happens twice) is rejected by default...
    [Fact]
    public void TestConvertZone_AmbiguousLocalTime_RejectsByDefault()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertZoneInput
        {
            Source = new ZonedInstant { LocalDateTime = "2026-11-01T01:30:00", ZoneId = "America/New_York" },
            TargetZoneId = "UTC",
        };
        var result = ConvertZoneNode.ConvertZone(ax, input);
        Assert.Equal("AMBIGUOUS_LOCAL_TIME", result.Error);
    }

    // ...and "earlier"/"later" pick the two distinct, one-hour-apart instants.
    [Fact]
    public void TestConvertZone_AmbiguousLocalTime_EarlierAndLaterDifferByOneHour()
    {
        IAxiomContext ax = new TestContext();
        var earlierInput = new ConvertZoneInput
        {
            Source = new ZonedInstant { LocalDateTime = "2026-11-01T01:30:00", ZoneId = "America/New_York" },
            TargetZoneId = "UTC",
            DstResolution = "earlier",
        };
        var laterInput = new ConvertZoneInput
        {
            Source = new ZonedInstant { LocalDateTime = "2026-11-01T01:30:00", ZoneId = "America/New_York" },
            TargetZoneId = "UTC",
            DstResolution = "later",
        };
        var earlier = ConvertZoneNode.ConvertZone(ax, earlierInput);
        var later = ConvertZoneNode.ConvertZone(ax, laterInput);
        Assert.Equal("2026-11-01T05:30:00Z", earlier.InstantUtc);
        Assert.Equal("2026-11-01T06:30:00Z", later.InstantUtc);
    }

    [Fact]
    public void TestConvertZone_UnknownZone_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertZoneInput
        {
            Source = new ZonedInstant { InstantUtc = "2026-07-15T12:00:00Z", ZoneId = "UTC" },
            TargetZoneId = "Not/AZone",
        };
        var result = ConvertZoneNode.ConvertZone(ax, input);
        Assert.Equal("UNKNOWN_ZONE", result.Error);
    }

    [Fact]
    public void TestConvertZone_EmptySource_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertZoneInput
        {
            Source = new ZonedInstant { ZoneId = "UTC" },
            TargetZoneId = "America/New_York",
        };
        var result = ConvertZoneNode.ConvertZone(ax, input);
        Assert.Equal("EMPTY_INPUT", result.Error);
    }
}
