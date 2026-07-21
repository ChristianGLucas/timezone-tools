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

public class ConvertCalendarDateTest
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

    // Independent oracle: 2026-07-21 (Gregorian) is documented as 7 Av 5786
    // on the Hebrew civil calendar (cross-checked against a Hebrew calendar
    // converter independent of this node's own NodaTime call).
    [Fact]
    public void TestConvertCalendarDate_IsoToHebrew_MatchesKnownConversion()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertCalendarDateInput { Year = 2026, Month = 7, Day = 21, FromCalendar = "Iso", ToCalendar = "Hebrew" };
        var result = ConvertCalendarDateNode.ConvertCalendarDate(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(5786, result.Year);
        Assert.Equal(11, result.Month);
        Assert.Equal(7, result.Day);
        Assert.Equal("AM", result.Era);
        Assert.Equal("2026-07-21", result.IsoDate);
    }

    // Round trip back to ISO recovers the original date exactly.
    [Fact]
    public void TestConvertCalendarDate_HebrewBackToIso_RoundTrips()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertCalendarDateInput { Year = 5786, Month = 11, Day = 7, FromCalendar = "Hebrew", ToCalendar = "Iso" };
        var result = ConvertCalendarDateNode.ConvertCalendarDate(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(2026, result.Year);
        Assert.Equal(7, result.Month);
        Assert.Equal(21, result.Day);
    }

    // Independent oracle: 2026-07-21 (Gregorian) is documented as 5 Sha'ban
    // 1448 on the Islamic civil calendar.
    [Fact]
    public void TestConvertCalendarDate_IsoToIslamicCivil_MatchesKnownConversion()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertCalendarDateInput { Year = 2026, Month = 7, Day = 21, FromCalendar = "Iso", ToCalendar = "IslamicCivil" };
        var result = ConvertCalendarDateNode.ConvertCalendarDate(ax, input);
        Assert.Equal("", result.Error);
        Assert.Equal(1448, result.Year);
        Assert.Equal(2, result.Month);
        Assert.Equal(5, result.Day);
        Assert.Equal("EH", result.Era);
    }

    [Fact]
    public void TestConvertCalendarDate_UnknownCalendar_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertCalendarDateInput { Year = 2026, Month = 7, Day = 21, FromCalendar = "Iso", ToCalendar = "Klingon" };
        var result = ConvertCalendarDateNode.ConvertCalendarDate(ax, input);
        Assert.Equal("UNKNOWN_CALENDAR", result.Error);
    }

    [Fact]
    public void TestConvertCalendarDate_InvalidMonth_ReturnsOutOfRangeNotCrash()
    {
        IAxiomContext ax = new TestContext();
        var input = new ConvertCalendarDateInput { Year = 2026, Month = 13, Day = 1, FromCalendar = "Iso", ToCalendar = "Gregorian" };
        var result = ConvertCalendarDateNode.ConvertCalendarDate(ax, input);
        Assert.Equal("OUT_OF_RANGE", result.Error);
    }
}
