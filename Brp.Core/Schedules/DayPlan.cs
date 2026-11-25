using System;
using System.Collections.Generic;
using System.Linq;

namespace Brp.Core.Schedules;

/// <summary>A whole day's baseline: slots in order, the first starting at midnight.</summary>
/// <typeparam name="TBlock">The game's schedule block.</typeparam>
public class DayPlan<TBlock>
{
    /// <summary>
    /// Creates a day plan. The slots must start at block 0 and be in strictly
    /// increasing order, so every block of the day belongs to exactly one slot.
    /// </summary>
    public DayPlan(IReadOnlyList<ScheduleSlot<TBlock>> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        if (slots.Count == 0 || slots[0].StartBlock != 0)
            throw new ArgumentException("A day plan must start with a slot at block 0.", nameof(slots));
        for (var i = 1; i < slots.Count; i++)
            if (slots[i].StartBlock <= slots[i - 1].StartBlock)
                throw new ArgumentException("Slots must be in strictly increasing order of start block.", nameof(slots));

        Slots = slots;
    }

    /// <summary>Creates a day plan from slots listed in order.</summary>
    public DayPlan(params ScheduleSlot<TBlock>[] slots) : this((IReadOnlyList<ScheduleSlot<TBlock>>)slots)
    {
    }

    /// <summary>The slots, in order of start block.</summary>
    public IReadOnlyList<ScheduleSlot<TBlock>> Slots { get; }

    /// <summary>The slot that covers <paramref name="blockOfDay"/>.</summary>
    public ScheduleSlot<TBlock> SlotAt(int blockOfDay)
    {
        return Slots.Last(slot => slot.StartBlock <= blockOfDay);
    }
}
