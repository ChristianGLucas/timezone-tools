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

public class AddPeriodTest
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

    // Ordinary case, no DST boundary crossed: one month after 2026-03-01
    // 02:30 local is 2026-04-01 02:30 local, unchanged wall-clock time.
    [Fact]
    public void TestAddPeriod_OneMonth_NoDstBoundary()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddPeriodInput
        {
            Zoned = new ZonedInstant { LocalDateTime = "2026-03-01T02:30:00", ZoneId = "America/New_York" },
            Period = new CalendarPeriod { Months = 1 },
        };
        var result = AddPeriodNode.AddPeriod(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-04-01T02:30:00", result.LocalDateTime);
    }

    // Independent oracle: adding +1 day to 2026-03-07T02:30:00 local in
    // America/New_York lands on 2026-03-08T02:30:00, which is inside the
    // known spring-forward gap (02:00-03:00 local does not exist that day).
    // The default resolution ("later") forward-shifts to 03:30 local — the
    // wall clock effectively skips the missing hour, matching real DST
    // behavior (verified independently against the IANA transition instant
    // 2026-03-08T07:00:00Z).
    [Fact]
    public void TestAddPeriod_OneDayLandingInDstGap_ForwardShiftsByDefault()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddPeriodInput
        {
            Zoned = new ZonedInstant { LocalDateTime = "2026-03-07T02:30:00", ZoneId = "America/New_York" },
            Period = new CalendarPeriod { Days = 1 },
        };
        var result = AddPeriodNode.AddPeriod(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-03-08T03:30:00", result.LocalDateTime);
        Assert.Equal("2026-03-08T07:30:00Z", result.InstantUtc);
        Assert.Equal(-14400, result.OffsetSeconds);
    }

    // Explicit dst_resolution=reject on the same gap-landing shift surfaces
    // the structured error instead of silently guessing.
    [Fact]
    public void TestAddPeriod_OneDayLandingInDstGap_RejectsWhenAsked()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddPeriodInput
        {
            Zoned = new ZonedInstant { LocalDateTime = "2026-03-07T02:30:00", ZoneId = "America/New_York" },
            Period = new CalendarPeriod { Days = 1 },
            DstResolution = "reject",
        };
        var result = AddPeriodNode.AddPeriod(ax, input);
        Assert.Equal("SKIPPED_LOCAL_TIME", result.Error);
    }

    // Negative period fields subtract.
    [Fact]
    public void TestAddPeriod_NegativeMonths_Subtracts()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddPeriodInput
        {
            Zoned = new ZonedInstant { LocalDateTime = "2026-04-01T02:30:00", ZoneId = "America/New_York" },
            Period = new CalendarPeriod { Months = -1 },
        };
        var result = AddPeriodNode.AddPeriod(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal("2026-03-01T02:30:00", result.LocalDateTime);
    }

    [Fact]
    public void TestAddPeriod_MissingPeriod_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var input = new AddPeriodInput
        {
            Zoned = new ZonedInstant { LocalDateTime = "2026-04-01T02:30:00", ZoneId = "America/New_York" },
        };
        var result = AddPeriodNode.AddPeriod(ax, input);
        Assert.Equal("EMPTY_INPUT", result.Error);
    }
}
