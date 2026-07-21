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

public class GetCalendarInfoTest
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

    // Independent oracle: ISO/Gregorian 2024 is a well-known leap year
    // (divisible by 4, not a century exception) with 12 months.
    [Fact]
    public void TestGetCalendarInfo_Iso2024_IsLeapYear()
    {
        IAxiomContext ax = new TestContext();
        var input = new CalendarInfoInput { Calendar = "Iso", Year = 2024 };
        var result = GetCalendarInfoNode.GetCalendarInfo(ax, input);
        Assert.Equal("", result.Error);
        Assert.True(result.IsLeapYear);
        Assert.Equal(12, result.MonthsInYear);
        Assert.Equal(12, result.DaysInMonth.Count);
        Assert.Equal(29, result.DaysInMonth[1]); // February
    }

    [Fact]
    public void TestGetCalendarInfo_Iso2023_IsNotLeapYear()
    {
        IAxiomContext ax = new TestContext();
        var input = new CalendarInfoInput { Calendar = "Iso", Year = 2023 };
        var result = GetCalendarInfoNode.GetCalendarInfo(ax, input);
        Assert.Equal("", result.Error);
        Assert.False(result.IsLeapYear);
        Assert.Equal(28, result.DaysInMonth[1]);
    }

    // Independent oracle: Hebrew year 5787 (2026-2027 CE) is a documented
    // leap year in the 19-year Hebrew (Metonic) cycle, carrying a 13th
    // month (Adar I/Adar II) instead of the usual 12.
    [Fact]
    public void TestGetCalendarInfo_HebrewLeapYear_HasThirteenMonths()
    {
        IAxiomContext ax = new TestContext();
        var input = new CalendarInfoInput { Calendar = "Hebrew", Year = 5787 };
        var result = GetCalendarInfoNode.GetCalendarInfo(ax, input);
        Assert.Equal("", result.Error);
        Assert.True(result.IsLeapYear);
        Assert.Equal(13, result.MonthsInYear);
        Assert.Equal(13, result.DaysInMonth.Count);
    }

    // The adjacent non-leap Hebrew year has the ordinary 12 months —
    // proving MonthsInYear genuinely varies by year, not a stub constant.
    [Fact]
    public void TestGetCalendarInfo_HebrewNonLeapYear_HasTwelveMonths()
    {
        IAxiomContext ax = new TestContext();
        var input = new CalendarInfoInput { Calendar = "Hebrew", Year = 5786 };
        var result = GetCalendarInfoNode.GetCalendarInfo(ax, input);
        Assert.Equal("", result.Error);
        Assert.False(result.IsLeapYear);
        Assert.Equal(12, result.MonthsInYear);
    }

    [Fact]
    public void TestGetCalendarInfo_UnknownCalendar_ReturnsStructuredError()
    {
        IAxiomContext ax = new TestContext();
        var input = new CalendarInfoInput { Calendar = "Klingon", Year = 2024 };
        var result = GetCalendarInfoNode.GetCalendarInfo(ax, input);
        Assert.Equal("UNKNOWN_CALENDAR", result.Error);
    }

    [Fact]
    public void TestGetCalendarInfo_YearOutOfRange_ReturnsStructuredErrorNotCrash()
    {
        IAxiomContext ax = new TestContext();
        var input = new CalendarInfoInput { Calendar = "Iso", Year = 99999 };
        var result = GetCalendarInfoNode.GetCalendarInfo(ax, input);
        Assert.Equal("OUT_OF_RANGE", result.Error);
    }
}
