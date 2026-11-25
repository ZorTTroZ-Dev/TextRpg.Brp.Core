using System;
using System.Collections.Generic;
using Brp.Core.Time;

namespace Brp.Core.Schedules;

/// <summary>
/// One stretch of a day's baseline: from <see cref="StartBlock"/> until the next
/// slot starts. With more than one variant, one is picked from the save's seed,
/// the day and the NPC, so the world varies from day to day but a reload gives
/// the same schedule.
/// </summary>
/// <typeparam name="TBlock">What the NPC is doing: the game's schedule block (location, activity, mood).</typeparam>
public class ScheduleSlot<TBlock>
{
    /// <summary>Creates a slot starting at <paramref name="startBlock"/> (0–95) with one or more variants.</summary>
    public ScheduleSlot(int startBlock, IReadOnlyList<TBlock> variants)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startBlock);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startBlock, Clock.BlocksPerDay);
        ArgumentNullException.ThrowIfNull(variants);
        if (variants.Count == 0)
            throw new ArgumentException("A slot needs at least one variant.", nameof(variants));

        StartBlock = startBlock;
        Variants = variants;
    }

    /// <summary>Creates a slot with a single block and no variation.</summary>
    public ScheduleSlot(int startBlock, TBlock block) : this(startBlock, new[] { block })
    {
    }

    /// <summary>The 15 minute block of the day this slot starts at, 0–95.</summary>
    public int StartBlock { get; }

    /// <summary>The authored alternates; a single entry means no variation.</summary>
    public IReadOnlyList<TBlock> Variants { get; }
}
