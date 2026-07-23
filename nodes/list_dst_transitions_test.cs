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

public class ListDstTransitionsTest
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

    // Independent oracle: the IANA tzdata rules for America/New_York put the
    // 2026 spring-forward transition at 2026-03-08T07:00:00Z (02:00 local
    // EST -> 03:00 local EDT) and the fall-back transition at
    // 2026-11-01T06:00:00Z (02:00 local EDT -> 01:00 local EST) — exactly
    // two transitions across the year, in that order.
    [Fact]
    public void TestListDstTransitions_NewYork2026_MatchesKnownTransitionDates()
    {
        IAxiomContext ax = new TestContext();
        var input = new ListDstTransitionsInput
        {
            ZoneId = "America/New_York",
            StartUtc = "2026-01-01T00:00:00Z",
            EndUtc = "2027-01-01T00:00:00Z",
        };
        var result = ListDstTransitionsNode.ListDstTransitions(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(2, result.Transitions.Count);

        var springForward = result.Transitions[0];
        Assert.Equal("2026-03-08T07:00:00Z", springForward.InstantUtc);
        Assert.Equal(-18000, springForward.OffsetBeforeSeconds);
        Assert.Equal(-14400, springForward.OffsetAfterSeconds);
        Assert.True(springForward.IsDstAfter);
        Assert.Equal("EST", springForward.AbbreviationBefore);
        Assert.Equal("EDT", springForward.AbbreviationAfter);

        var fallBack = result.Transitions[1];
        Assert.Equal("2026-11-01T06:00:00Z", fallBack.InstantUtc);
        Assert.Equal(-14400, fallBack.OffsetBeforeSeconds);
        Assert.Equal(-18000, fallBack.OffsetAfterSeconds);
        Assert.False(fallBack.IsDstAfter);
        Assert.Equal("EDT", fallBack.AbbreviationBefore);
        Assert.Equal("EST", fallBack.AbbreviationAfter);
    }

    // A zone with no DST at all (UTC) returns an empty, error-free list
    // rather than failing.
    [Fact]
    public void TestListDstTransitions_UtcHasNoTransitions()
    {
        IAxiomContext ax = new TestContext();
        var input = new ListDstTransitionsInput
        {
            ZoneId = "UTC",
            StartUtc = "2026-01-01T00:00:00Z",
            EndUtc = "2027-01-01T00:00:00Z",
        };
        var result = ListDstTransitionsNode.ListDstTransitions(ax, input);
        Assert.Equal("", result.Error);
        Assert.Empty(result.Transitions);
    }

    [Fact]
    public void TestListDstTransitions_EndBeforeStart_ReturnsInvalidArgument()
    {
        IAxiomContext ax = new TestContext();
        var input = new ListDstTransitionsInput
        {
            ZoneId = "America/New_York",
            StartUtc = "2026-06-01T00:00:00Z",
            EndUtc = "2026-01-01T00:00:00Z",
        };
        var result = ListDstTransitionsNode.ListDstTransitions(ax, input);
        Assert.Equal("INVALID_ARGUMENT", result.Error);
    }

    [Fact]
    public void TestListDstTransitions_UnknownZone_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var input = new ListDstTransitionsInput
        {
            ZoneId = "Not/AZone",
            StartUtc = "2026-01-01T00:00:00Z",
            EndUtc = "2027-01-01T00:00:00Z",
        };
        var result = ListDstTransitionsNode.ListDstTransitions(ax, input);
        Assert.Equal("UNKNOWN_ZONE", result.Error);
    }
}
