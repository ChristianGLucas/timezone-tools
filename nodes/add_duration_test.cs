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

public class AddDurationTest
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
    public void TestAddDuration_OneHour_NoDstBoundary()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddDurationInput
        {
            Zoned = new ZonedInstant { InstantUtc = "2026-01-15T10:00:00Z", ZoneId = "UTC" },
            Seconds = 3600,
        };
        var result = AddDurationNode.AddDuration(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-01-15T11:00:00Z", result.InstantUtc);
    }

    // The classic DST gotcha, and the direct contrast with AddPeriod: adding
    // 24 REAL hours to 2026-03-07T01:30:00 local in America/New_York lands
    // on 2026-03-08T01:30:00 local at UTC-5 (still EST, since the actual
    // spring-forward transition, 2026-03-08T07:00:00Z, has not yet been
    // reached by 24 elapsed hours from 2026-03-07T06:30:00Z) — a DIFFERENT
    // wall-clock result than AddPeriod's "+1 day", which lands at 03:30
    // local (forward-shifted through the gap). Same starting point, same
    // nominal "one day", different real-world answer — verified
    // independently against the IANA transition instant.
    [Fact]
    public void TestAddDuration_TwentyFourHours_DiffersFromAddPeriodOneDay()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddDurationInput
        {
            Zoned = new ZonedInstant { LocalDateTime = "2026-03-07T01:30:00", ZoneId = "America/New_York" },
            Seconds = 24 * 3600,
        };
        var result = AddDurationNode.AddDuration(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-03-08T01:30:00", result.LocalDateTime);
        Assert.Equal(-18000, result.OffsetSeconds); // still EST (UTC-5)
        Assert.False(result.IsDst);
    }

    [Fact]
    public void TestAddDuration_NegativeSeconds_Subtracts()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddDurationInput
        {
            Zoned = new ZonedInstant { InstantUtc = "2026-01-15T10:00:00Z", ZoneId = "UTC" },
            Seconds = -1800,
        };
        var result = AddDurationNode.AddDuration(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-01-15T09:30:00Z", result.InstantUtc);
    }

    [Fact]
    public void TestAddDuration_NaNSeconds_ReturnsStructuredErrorNotCrash()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddDurationInput
        {
            Zoned = new ZonedInstant { InstantUtc = "2026-01-15T10:00:00Z", ZoneId = "UTC" },
            Seconds = double.NaN,
        };
        var result = AddDurationNode.AddDuration(ax, input);
        Assert.Equal("INVALID_ARGUMENT", result.Error);
    }

    [Fact]
    public void TestAddDuration_EmptyZoned_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var result = AddDurationNode.AddDuration(ax, new AddDurationInput { Zoned = new ZonedInstant { ZoneId = "UTC" } });
        Assert.Equal("EMPTY_INPUT", result.Error);
    }
}
