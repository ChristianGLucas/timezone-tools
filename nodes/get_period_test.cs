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

public class GetPeriodTest
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

    // Independent oracle: 2020-01-15T10:00:00 to 2026-07-21T08:30:00, hand-
    // counted: 6 full years to 2026-01-15, then 6 more months to 2026-07-15,
    // then 5 days 22h 30m to 2026-07-21T08:30:00.
    [Fact]
    public void TestGetPeriod_SixYearsSixMonthsFiveDays()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetPeriodInput
        {
            Start = new ZonedInstant { LocalDateTime = "2020-01-15T10:00:00", ZoneId = "UTC" },
            End = new ZonedInstant { LocalDateTime = "2026-07-21T08:30:00", ZoneId = "UTC" },
        };
        var result = GetPeriodNode.GetPeriod(ax, input);
        Assert.Equal("", result.Error);
        Assert.False(result.Negative);
        Assert.Equal(6, result.Years);
        Assert.Equal(6, result.Months);
        Assert.Equal(0, result.Weeks);
        Assert.Equal(5, result.Days);
        Assert.Equal(22, result.Hours);
        Assert.Equal(30, result.Minutes);
        Assert.Equal(0, result.Seconds);
    }

    // Weeks genuinely normalize when the day remainder is >= 7 (e.g. 10 days
    // -> 1 week + 3 days), proving Weeks isn't a stub constant.
    [Fact]
    public void TestGetPeriod_TenDaysNormalizesToOneWeekThreeDays()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetPeriodInput
        {
            Start = new ZonedInstant { LocalDateTime = "2026-01-01T00:00:00", ZoneId = "UTC" },
            End = new ZonedInstant { LocalDateTime = "2026-01-11T00:00:00", ZoneId = "UTC" },
        };
        var result = GetPeriodNode.GetPeriod(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(1, result.Weeks);
        Assert.Equal(3, result.Days);
    }

    // Reversed order reports non-negative magnitude fields with negative=true.
    [Fact]
    public void TestGetPeriod_ReversedOrder_IsNegativeWithNonNegativeFields()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetPeriodInput
        {
            Start = new ZonedInstant { LocalDateTime = "2026-07-21T08:30:00", ZoneId = "UTC" },
            End = new ZonedInstant { LocalDateTime = "2020-01-15T10:00:00", ZoneId = "UTC" },
        };
        var result = GetPeriodNode.GetPeriod(ax, input);
        Assert.Equal("", result.Error);
        Assert.True(result.Negative);
        Assert.Equal(6, result.Years);
        Assert.Equal(6, result.Months);
    }

    [Fact]
    public void TestGetPeriod_UnknownZone_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetPeriodInput
        {
            Start = new ZonedInstant { LocalDateTime = "2020-01-15T10:00:00", ZoneId = "Not/AZone" },
            End = new ZonedInstant { LocalDateTime = "2026-07-21T08:30:00", ZoneId = "UTC" },
        };
        var result = GetPeriodNode.GetPeriod(ax, input);
        Assert.Equal("UNKNOWN_ZONE", result.Error);
    }
}
