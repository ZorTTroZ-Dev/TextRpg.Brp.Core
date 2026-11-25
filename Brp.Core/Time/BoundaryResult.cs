using System;
using System.Collections.Generic;

namespace Brp.Core.Time;

/// <summary>What one handler did at a boundary.</summary>
public readonly record struct BoundaryResult<TState, TEvent>
{
    /// <summary>The state after the handler ran.</summary>
    public required TState State { get; init; }

    /// <summary>The events it raised, in order.</summary>
    public required IReadOnlyList<TEvent> Events { get; init; }

    /// <summary>
    /// Whether time should stop at this boundary (for example combat started or a
    /// sensed event offers the player a choice). The remaining handlers for this
    /// boundary still run; later boundaries do not. False unless set.
    /// </summary>
    public bool Stop { get; init; }

    /// <summary>The state unchanged, with no events.</summary>
    public static BoundaryResult<TState, TEvent> Nothing(TState state)
    {
        return new BoundaryResult<TState, TEvent> { State = state, Events = Array.Empty<TEvent>() };
    }
}
