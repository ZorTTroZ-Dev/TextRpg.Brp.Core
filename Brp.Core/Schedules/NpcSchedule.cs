using System;
using System.Collections.Generic;
using Brp.Core.Time;

namespace Brp.Core.Schedules;

/// <summary>
/// One NPC's authored schedule: a baseline day, optional baselines for
/// particular days of the week, and override rules.
/// </summary>
/// <typeparam name="TBlock">The game's schedule block.</typeparam>
/// <typeparam name="TContext">What override conditions read.</typeparam>
public class NpcSchedule<TBlock, TContext>
{
    /// <summary>Creates an NPC schedule.</summary>
    /// <param name="npcId">The NPC's stable string ID; it keys the variant picks.</param>
    /// <param name="baseline">The day plan used on any day without its own.</param>
    /// <param name="byDayOfWeek">Day plans for particular days of the week (0–6), if any.</param>
    /// <param name="overrides">The override rules, if any.</param>
    public NpcSchedule(
        string npcId,
        DayPlan<TBlock> baseline,
        IReadOnlyDictionary<int, DayPlan<TBlock>>? byDayOfWeek = null,
        IReadOnlyList<ScheduleOverride<TBlock, TContext>>? overrides = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(npcId);
        ArgumentNullException.ThrowIfNull(baseline);
        byDayOfWeek ??= new Dictionary<int, DayPlan<TBlock>>();
        foreach (var day in byDayOfWeek.Keys)
            if (day is < 0 or >= Clock.DaysPerWeek)
                throw new ArgumentOutOfRangeException(nameof(byDayOfWeek), day, "Days of the week run from 0 to 6.");

        NpcId = npcId;
        Baseline = baseline;
        ByDayOfWeek = byDayOfWeek;
        Overrides = overrides ?? Array.Empty<ScheduleOverride<TBlock, TContext>>();
    }

    /// <summary>The NPC's stable string ID.</summary>
    public string NpcId { get; }

    /// <summary>The day plan used on any day without its own.</summary>
    public DayPlan<TBlock> Baseline { get; }

    /// <summary>Day plans for particular days of the week.</summary>
    public IReadOnlyDictionary<int, DayPlan<TBlock>> ByDayOfWeek { get; }

    /// <summary>The override rules, in authored order.</summary>
    public IReadOnlyList<ScheduleOverride<TBlock, TContext>> Overrides { get; }

    /// <summary>The baseline day plan for <paramref name="day"/>.</summary>
    public DayPlan<TBlock> PlanFor(long day)
    {
        return ByDayOfWeek.TryGetValue(Clock.DayOfWeekOf(day), out var plan) ? plan : Baseline;
    }
}
