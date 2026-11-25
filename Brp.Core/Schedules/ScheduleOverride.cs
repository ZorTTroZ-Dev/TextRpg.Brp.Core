using System;
using Brp.Core.Time;

namespace Brp.Core.Schedules;

/// <summary>
/// An override rule: while its condition holds, the NPC follows
/// <see cref="Block"/> instead of the baseline. An override interrupts a block
/// already in progress, so its condition is checked at the exact time asked,
/// not only on 15 minute boundaries.
/// </summary>
/// <typeparam name="TBlock">The game's schedule block.</typeparam>
/// <typeparam name="TContext">What conditions read, typically the game's flags.</typeparam>
public record ScheduleOverride<TBlock, TContext>
{
    /// <summary>A stable ID, reported when the override is active.</summary>
    public required string Id { get; init; }

    /// <summary>Higher wins when several hold; ties go to the one listed first.</summary>
    public required int Priority { get; init; }

    /// <summary>Whether the override applies, given the context and the time.</summary>
    public required Func<TContext, Clock, bool> Condition { get; init; }

    /// <summary>What the NPC does while it applies.</summary>
    public required TBlock Block { get; init; }
}
