using System.Collections.Generic;

namespace Brp.Core.Time;

/// <summary>The outcome of advancing the clock.</summary>
public record AdvanceResult<TState, TEvent>
{
    /// <summary>The state after every processed boundary.</summary>
    public required TState State { get; init; }

    /// <summary>
    /// Where the clock ended: the full cost when nothing stopped it, otherwise the
    /// boundary that stopped it.
    /// </summary>
    public required Clock Clock { get; init; }

    /// <summary>Every event raised, in the order raised.</summary>
    public required IReadOnlyList<TEvent> Events { get; init; }

    /// <summary>
    /// Whether a handler asked to stop. This can be true with the full cost spent,
    /// when the stop came at a boundary exactly at the end.
    /// </summary>
    public required bool Stopped { get; init; }
}
