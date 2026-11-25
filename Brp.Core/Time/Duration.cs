using System;

namespace Brp.Core.Time;

/// <summary>
/// A length of game time, counted in whole 12-second ticks (one BRP combat
/// round). Every time cost is a multiple of 12 seconds, so a cost that does
/// not divide evenly is rejected rather than rounded.
/// </summary>
public readonly record struct Duration : IComparable<Duration>
{
    /// <summary>Seconds in one tick: one BRP combat round.</summary>
    public const int SecondsPerTick = 12;

    /// <summary>Ticks in one 15 minute block.</summary>
    public const int TicksPerBlock = 15 * 60 / SecondsPerTick;

    /// <summary>Ticks in one day.</summary>
    public const int TicksPerDay = 24 * 60 * 60 / SecondsPerTick;

    /// <summary>No time at all.</summary>
    public static readonly Duration Zero = new(0);

    /// <summary>Creates a duration of <paramref name="ticks"/> ticks, which must not be negative.</summary>
    public Duration(long ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        Ticks = ticks;
    }

    /// <summary>The length in 12-second ticks.</summary>
    public long Ticks { get; }

    /// <summary>The length in seconds.</summary>
    public long TotalSeconds => Ticks * SecondsPerTick;

    /// <summary>A duration of whole combat rounds (12 seconds each).</summary>
    public static Duration FromRounds(long rounds)
    {
        return new Duration(rounds);
    }

    /// <summary>A duration in seconds, which must be a multiple of 12.</summary>
    public static Duration FromSeconds(long seconds)
    {
        if (seconds % SecondsPerTick != 0)
            throw new ArgumentException($"{seconds} seconds is not a multiple of {SecondsPerTick}.", nameof(seconds));
        return new Duration(seconds / SecondsPerTick);
    }

    /// <summary>A duration in whole minutes.</summary>
    public static Duration FromMinutes(long minutes)
    {
        return FromSeconds(minutes * 60);
    }

    /// <summary>A duration in whole hours.</summary>
    public static Duration FromHours(long hours)
    {
        return FromMinutes(hours * 60);
    }

    /// <summary>The sum of two durations.</summary>
    public static Duration operator +(Duration left, Duration right)
    {
        return new Duration(left.Ticks + right.Ticks);
    }

    /// <inheritdoc/>
    public int CompareTo(Duration other)
    {
        return Ticks.CompareTo(other.Ticks);
    }

    /// <summary>Whether <paramref name="left"/> is shorter than <paramref name="right"/>.</summary>
    public static bool operator <(Duration left, Duration right)
    {
        return left.Ticks < right.Ticks;
    }

    /// <summary>Whether <paramref name="left"/> is longer than <paramref name="right"/>.</summary>
    public static bool operator >(Duration left, Duration right)
    {
        return left.Ticks > right.Ticks;
    }

    /// <summary>Whether <paramref name="left"/> is no longer than <paramref name="right"/>.</summary>
    public static bool operator <=(Duration left, Duration right)
    {
        return left.Ticks <= right.Ticks;
    }

    /// <summary>Whether <paramref name="left"/> is no shorter than <paramref name="right"/>.</summary>
    public static bool operator >=(Duration left, Duration right)
    {
        return left.Ticks >= right.Ticks;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Ticks} ticks ({TotalSeconds}s)";
    }
}
