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

public class GetDurationTest
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

    // Independent oracle: 2026-01-15T12:00:00Z to 2026-07-15T12:00:00Z spans
    // exactly 181 days by direct calendar count (Jan 16 remaining + Feb 28 +
    // Mar 31 + Apr 30 + May 31 + Jun 30 + Jul 15 = 181), hand-computed
    // independent of this node's own arithmetic.
    [Fact]
    public void TestGetDuration_ExactlyOneHundredEightyOneDays()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetDurationInput { StartUtc = "2026-01-15T12:00:00Z", EndUtc = "2026-07-15T12:00:00Z" };
        var result = GetDurationNode.GetDuration(ax, input);
        Assert.Equal("", result.Error);
        Assert.False(result.Negative);
        Assert.Equal(181, result.Days);
        Assert.Equal(0, result.Hours);
        Assert.Equal(0, result.Minutes);
        Assert.Equal(0, result.Seconds);
        Assert.Equal(181.0 * 24 * 3600, result.TotalSeconds);
        Assert.Equal(181L * 24 * 3600 * 1_000_000_000L, result.TotalNanoseconds);
    }

    [Fact]
    public void TestGetDuration_ReversedOrder_IsNegativeWithSameMagnitude()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetDurationInput { StartUtc = "2026-07-15T12:00:00Z", EndUtc = "2026-01-15T12:00:00Z" };
        var result = GetDurationNode.GetDuration(ax, input);
        Assert.Equal("", result.Error);
        Assert.True(result.Negative);
        Assert.Equal(181, result.Days);
        Assert.Equal(-181L * 24 * 3600 * 1_000_000_000L, result.TotalNanoseconds);
    }

    [Fact]
    public void TestGetDuration_SubSecondPrecisionPreserved()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetDurationInput { StartUtc = "2026-01-15T12:00:00.100Z", EndUtc = "2026-01-15T12:00:00.900Z" };
        var result = GetDurationNode.GetDuration(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(0, result.Days);
        Assert.Equal(0, result.Seconds);
        Assert.Equal(800, result.Milliseconds);
        Assert.Equal(800_000_000L, result.TotalNanoseconds);
    }

    [Fact]
    public void TestGetDuration_MalformedInstant_ReturnsStructuredErrorNotCrash()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetDurationInput { StartUtc = "garbage", EndUtc = "2026-01-15T12:00:00Z" };
        var result = GetDurationNode.GetDuration(ax, input);
        Assert.Equal("INVALID_ARGUMENT", result.Error);
    }

    [Fact]
    public void TestGetDuration_EmptyInput_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var result = GetDurationNode.GetDuration(ax, new GetDurationInput());
        Assert.Equal("EMPTY_INPUT", result.Error);
    }
}
