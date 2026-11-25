using System.Collections.Generic;
using System.Linq;
using Brp.Core.Time;
using NUnit.Framework;

namespace Brp.Core.Tests.Time;

public class EventProcessorTests
{
    // A handler that records the boundary it saw, tagged with its own name.
    private static BoundaryHandler<int, string> Log(string name)
    {
        return (state, boundary) => new BoundaryResult<int, string> { State = state, Events = new[] { $"{name}@{boundary.Tick}" } };
    }

    private static AdvanceResult<int, string> Advance(long from, long ticks, params BoundaryHandler<int, string>[] handlers)
    {
        return EventProcessor.Advance(0, new Clock(from), new Duration(ticks), handlers);
    }

    #region Advance

    [Test]
    public void Advance_CostInsideOneBlock_ProcessesNoBoundary()
    {
        var result = Advance(10, 30, Log("a"));
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.Clock.Tick, Is.EqualTo(40));
        Assert.That(result.Stopped, Is.False);
    }

    [Test]
    public void Advance_SeveralBoundaries_ProcessesThemOneAtATimeInOrder()
    {
        var result = Advance(70, 160, Log("a"), Log("b"));
        Assert.That(result.Events, Is.EqualTo(new[] { "a@75", "b@75", "a@150", "b@150", "a@225", "b@225" }));
        Assert.That(result.Clock.Tick, Is.EqualTo(230));
    }

    [Test]
    public void Advance_SeveralHandlers_EachSeesTheStateLeftByTheOneBefore()
    {
        BoundaryHandler<int, string> add = (state, _) => BoundaryResult<int, string>.Nothing(state + 1);
        BoundaryHandler<int, string> report = (state, boundary) =>
            new BoundaryResult<int, string> { State = state, Events = new[] { $"{state}@{boundary.Tick}" } };

        var result = EventProcessor.Advance(0, new Clock(0), new Duration(150), new[] { add, report });
        Assert.That(result.Events, Is.EqualTo(new[] { "1@75", "2@150" }));
        Assert.That(result.State, Is.EqualTo(2));
    }

    [Test]
    public void Advance_HandlerStops_HoldsTheClockAtThatBoundary()
    {
        BoundaryHandler<int, string> stopAt150 = (state, boundary) =>
            new BoundaryResult<int, string>
            {
                State = state,
                Events = new[] { $"stop?@{boundary.Tick}" },
                Stop = boundary.Tick == 150,
            };

        var result = Advance(0, 7200, stopAt150, Log("after"));
        Assert.That(result.Stopped, Is.True);
        Assert.That(result.Clock.Tick, Is.EqualTo(150));
        // Every handler at the stopping boundary still runs; no later boundary does.
        Assert.That(result.Events, Is.EqualTo(new[] { "stop?@75", "after@75", "stop?@150", "after@150" }));
    }

    [Test]
    public void Advance_StopOnTheLastBoundary_IsStillReported()
    {
        BoundaryHandler<int, string> stop = (state, _) => new BoundaryResult<int, string> { State = state, Events = new string[0], Stop = true };

        var result = Advance(0, 75, stop);
        Assert.That(result.Stopped, Is.True);
        Assert.That(result.Clock.Tick, Is.EqualTo(75));
    }

    [Test]
    public void Advance_MultiDaySkip_ProcessesEveryBoundary()
    {
        var seen = new List<long>();
        BoundaryHandler<int, string> record = (state, boundary) =>
        {
            seen.Add(boundary.Tick);
            return BoundaryResult<int, string>.Nothing(state);
        };

        var result = EventProcessor.Advance(0, new Clock(100), Duration.FromHours(72), new[] { record });
        Assert.That(seen, Has.Count.EqualTo(3 * 96));
        Assert.That(seen, Is.Ordered.And.Unique);
        Assert.That(seen.All(tick => tick % 75 == 0));
        Assert.That(result.Clock.Tick, Is.EqualTo(100 + 3 * 7200));
    }

    [Test]
    public void Advance_ZeroCost_ProcessesNothing()
    {
        var result = Advance(75, 0, Log("a"));
        Assert.That(result.Events, Is.Empty);
        Assert.That(result.Clock.Tick, Is.EqualTo(75));
    }

    [Test]
    public void Advance_CostSplitInTwo_GivesTheSameEventsAsOneAdvance()
    {
        var whole = Advance(40, 500, Log("a"));
        var first = Advance(40, 200, Log("a"));
        var second = Advance(first.Clock.Tick, 300, Log("a"));
        Assert.That(first.Events.Concat(second.Events), Is.EqualTo(whole.Events));
    }

    #endregion
}
