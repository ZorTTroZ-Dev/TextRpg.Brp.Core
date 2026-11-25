using System;
using System.Collections.Generic;

namespace Brp.Core.Time;

/// <summary>
/// The one clock that every action costing time runs through. The cost is
/// added, then each 15 minute boundary crossed is processed one at a time, in
/// the order crossed. The action itself resolves against the block it started
/// in, so the caller resolves it before calling <see cref="Advance{TState,TEvent}"/>.
/// </summary>
public static class EventProcessor
{
    /// <summary>
    /// Advances the clock from <paramref name="from"/> by <paramref name="cost"/>,
    /// running <paramref name="handlers"/> in order at every boundary crossed.
    /// When a handler asks to stop, the clock stays at that boundary (for
    /// example travel stops at the last location reached).
    /// </summary>
    public static AdvanceResult<TState, TEvent> Advance<TState, TEvent>(
        TState state,
        Clock from,
        Duration cost,
        IReadOnlyList<BoundaryHandler<TState, TEvent>> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        var events = new List<TEvent>();
        var end = from + cost;

        foreach (var boundary in Clock.BoundariesBetween(from, end))
        {
            var stop = false;
            foreach (var handler in handlers)
            {
                var result = handler(state, boundary);
                state = result.State;
                events.AddRange(result.Events);
                stop |= result.Stop;
            }

            if (stop)
                return new AdvanceResult<TState, TEvent> { State = state, Clock = boundary, Events = events, Stopped = true };
        }

        return new AdvanceResult<TState, TEvent> { State = state, Clock = end, Events = events, Stopped = false };
    }
}
