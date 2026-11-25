using System;
using System.Collections.Generic;

namespace Brp.Core.Time;

/// <summary>
/// A point in game time: an integer count of 12-second ticks since the start
/// of day 0. A 15 minute block is 75 ticks and a day is 7,200.
/// </summary>
public readonly record struct Clock : IComparable<Clock>
{
    /// <summary>The start of day 0.</summary>
    public static readonly Clock Start = new(0);

    /// <summary>15 minute blocks in one day.</summary>
    public const int BlocksPerDay = Duration.TicksPerDay / Duration.TicksPerBlock;

    /// <summary>Days in one week.</summary>
    public const int DaysPerWeek = 7;

    /// <summary>The day of the week, 0–6, of day number <paramref name="day"/>. Day 0 is day 0 of the week; the game names the days.</summary>
    public static int DayOfWeekOf(long day)
    {
        return (int)(day % DaysPerWeek);
    }

    /// <summary>Creates a clock at <paramref name="tick"/>, which must not be negative.</summary>
    public Clock(long tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);
        Tick = tick;
    }

    /// <summary>Ticks since the start of day 0.</summary>
    public long Tick { get; }

    /// <summary>The day number, counting from 0.</summary>
    public long Day => Tick / Duration.TicksPerDay;

    /// <summary>The day of the week, 0–6.</summary>
    public int DayOfWeek => DayOfWeekOf(Day);

    /// <summary>The 15 minute block of the day, from 0 (00:00–00:15) to 95 (23:45–24:00).</summary>
    public int BlockOfDay => (int)(Tick % Duration.TicksPerDay / Duration.TicksPerBlock);

    /// <summary>Ticks since midnight.</summary>
    public int TickOfDay => (int)(Tick % Duration.TicksPerDay);

    /// <summary>The hour of the day, 0–23.</summary>
    public int Hour => TickOfDay * Duration.SecondsPerTick / 3600;

    /// <summary>The minute of the hour, 0–59.</summary>
    public int Minute => TickOfDay * Duration.SecondsPerTick / 60 % 60;

    /// <summary>The second of the minute: 0, 12, 24, 36 or 48.</summary>
    public int Second => TickOfDay * Duration.SecondsPerTick % 60;

    /// <summary>Whether this clock sits exactly on a 15 minute boundary.</summary>
    public bool IsOnBoundary => Tick % Duration.TicksPerBlock == 0;

    /// <summary>
    /// The 15 minute boundaries crossed when moving from <paramref name="from"/>
    /// to <paramref name="to"/>, in the order crossed. A boundary exactly at
    /// <paramref name="from"/> is not included (it was processed when the clock
    /// reached it); one exactly at <paramref name="to"/> is.
    /// </summary>
    public static IEnumerable<Clock> BoundariesBetween(Clock from, Clock to)
    {
        if (to < from)
            throw new ArgumentException("The end is before the start.", nameof(to));

        var firstTick = (from.Tick / Duration.TicksPerBlock + 1) * Duration.TicksPerBlock;
        for (var tick = firstTick; tick <= to.Tick; tick += Duration.TicksPerBlock)
            yield return new Clock(tick);
    }

    /// <summary>The clock moved forward by <paramref name="duration"/>.</summary>
    public static Clock operator +(Clock clock, Duration duration)
    {
        return new Clock(clock.Tick + duration.Ticks);
    }

    /// <summary>The time from <paramref name="earlier"/> to <paramref name="later"/>, which must not be negative.</summary>
    public static Duration operator -(Clock later, Clock earlier)
    {
        return new Duration(later.Tick - earlier.Tick);
    }

    /// <inheritdoc/>
    public int CompareTo(Clock other)
    {
        return Tick.CompareTo(other.Tick);
    }

    /// <summary>Whether <paramref name="left"/> is earlier than <paramref name="right"/>.</summary>
    public static bool operator <(Clock left, Clock right)
    {
        return left.Tick < right.Tick;
    }

    /// <summary>Whether <paramref name="left"/> is later than <paramref name="right"/>.</summary>
    public static bool operator >(Clock left, Clock right)
    {
        return left.Tick > right.Tick;
    }

    /// <summary>Whether <paramref name="left"/> is no later than <paramref name="right"/>.</summary>
    public static bool operator <=(Clock left, Clock right)
    {
        return left.Tick <= right.Tick;
    }

    /// <summary>Whether <paramref name="left"/> is no earlier than <paramref name="right"/>.</summary>
    public static bool operator >=(Clock left, Clock right)
    {
        return left.Tick >= right.Tick;
    }

    /// <summary>Day and time, for example "Day 2 08:15:36".</summary>
    public override string ToString()
    {
        return $"Day {Day} {Hour:00}:{Minute:00}:{Second:00}";
    }
}
