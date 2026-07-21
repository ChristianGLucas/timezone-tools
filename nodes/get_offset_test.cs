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

public class GetOffsetTest
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

    // Independent oracle: America/New_York is documented UTC-5 (EST) in
    // January (standard time, no DST).
    [Fact]
    public void TestGetOffset_NewYorkJanuary_IsUtcMinus5NoDst()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetOffsetInput { InstantUtc = "2026-01-15T12:00:00Z", ZoneId = "America/New_York" };
        var result = GetOffsetNode.GetOffset(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(-18000, result.OffsetSeconds);
        Assert.Equal("-05:00", result.OffsetIso);
        Assert.False(result.IsDst);
        Assert.Equal("EST", result.ZoneAbbreviation);
    }

    // Independent oracle: America/New_York is documented UTC-4 (EDT) in July
    // (daylight saving time in effect).
    [Fact]
    public void TestGetOffset_NewYorkJuly_IsUtcMinus4Dst()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetOffsetInput { InstantUtc = "2026-07-15T12:00:00Z", ZoneId = "America/New_York" };
        var result = GetOffsetNode.GetOffset(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(-14400, result.OffsetSeconds);
        Assert.Equal("-04:00", result.OffsetIso);
        Assert.True(result.IsDst);
        Assert.Equal("EDT", result.ZoneAbbreviation);
    }

    // Independent oracle: Asia/Kolkata is documented UTC+5:30 year-round
    // (India has no DST), exercising the non-hour-aligned offset path.
    [Fact]
    public void TestGetOffset_Kolkata_IsUtcPlus5_30NoDst()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetOffsetInput { InstantUtc = "2026-07-15T12:00:00Z", ZoneId = "Asia/Kolkata" };
        var result = GetOffsetNode.GetOffset(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(19800, result.OffsetSeconds);
        Assert.Equal("+05:30", result.OffsetIso);
        Assert.False(result.IsDst);
    }

    [Fact]
    public void TestGetOffset_Utc_IsZeroOffset()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetOffsetInput { InstantUtc = "2026-07-15T12:00:00Z", ZoneId = "UTC" };
        var result = GetOffsetNode.GetOffset(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(0, result.OffsetSeconds);
        Assert.Equal("+00:00", result.OffsetIso);
    }

    [Fact]
    public void TestGetOffset_UnknownZone_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetOffsetInput { InstantUtc = "2026-07-15T12:00:00Z", ZoneId = "Not/AZone" };
        var result = GetOffsetNode.GetOffset(ax, input);
        Assert.Equal("UNKNOWN_ZONE", result.Error);
    }

    [Fact]
    public void TestGetOffset_MalformedInstant_ReturnsStructuredErrorNotCrash()
    {
        IAxiomContext ax = new TestContext();
        var input = new GetOffsetInput { InstantUtc = "not-a-timestamp", ZoneId = "UTC" };
        var result = GetOffsetNode.GetOffset(ax, input);
        Assert.Equal("INVALID_ARGUMENT", result.Error);
    }

    [Fact]
    public void TestGetOffset_EmptyInput_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var result = GetOffsetNode.GetOffset(ax, new GetOffsetInput());
        Assert.Equal("EMPTY_INPUT", result.Error);
    }
}
