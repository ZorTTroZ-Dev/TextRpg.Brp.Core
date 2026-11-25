using System;
using Brp.Core.Dice;
using Brp.Core.Time;

namespace Brp.Core.Schedules;

/// <summary>
/// Resolves an NPC's schedule. NPCs away from the player are lookups, not
/// simulations: the highest-priority override whose condition holds wins,
/// otherwise the baseline block for the current time applies.
/// </summary>
public static class ScheduleResolver
{
    /// <summary>Resolves <paramref name="schedule"/> at <paramref name="at"/>.</summary>
    /// <param name="schedule">The NPC's schedule.</param>
    /// <param name="at">The time to resolve at.</param>
    /// <param name="context">What override conditions read, typically the flags.</param>
    /// <param name="seed">The save's seed, for picks among authored alternates.</param>
    public static ScheduleResolution<TBlock> Resolve<TBlock, TContext>(
        NpcSchedule<TBlock, TContext> schedule, Clock at, TContext context, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        ScheduleOverride<TBlock, TContext>? winner = null;
        foreach (var scheduleOverride in schedule.Overrides)
            if ((winner is null || scheduleOverride.Priority > winner.Priority) && scheduleOverride.Condition(context, at))
                winner = scheduleOverride;
        if (winner is not null)
            return new ScheduleResolution<TBlock> { Block = winner.Block, OverrideId = winner.Id };

        var slot = schedule.PlanFor(at.Day).SlotAt(at.BlockOfDay);
        var block = Pick(slot, seed, at.Day, schedule.NpcId);
        return new ScheduleResolution<TBlock> { Block = block, OverrideId = null };
    }

    /// <summary>
    /// The variant a slot uses on <paramref name="day"/>: keyed from the seed,
    /// the day, the NPC and the slot's start, so it holds for the whole slot,
    /// is the same after a reload, and differs between slots and NPCs.
    /// </summary>
    public static TBlock Pick<TBlock>(ScheduleSlot<TBlock> slot, ulong seed, long day, string npcId)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (slot.Variants.Count == 1)
            return slot.Variants[0];

        var hash = KeyedRng.Hash(seed, (ulong)day, KeyedRng.StableHash(npcId), (ulong)slot.StartBlock);
        return slot.Variants[KeyedRng.Below(hash, slot.Variants.Count)];
    }
}
