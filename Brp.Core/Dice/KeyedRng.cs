using System;
using System.Text;

namespace Brp.Core.Dice;

/// <summary>
/// The core's own counter-based random number generator. Rolls are not drawn
/// in sequence: each one is computed from its inputs, so the same inputs always
/// give the same value on every machine and every .NET version.
/// </summary>
/// <remarks>
/// The mixing function is the SplitMix64 finalizer. Changing anything in this
/// file changes every roll in every existing save, so treat it as frozen.
/// </remarks>
public static class KeyedRng
{
    private const ulong Golden = 0x9E3779B97F4A7C15;

    /// <summary>Combines the inputs into one well-mixed 64-bit value.</summary>
    /// <param name="seed">The save's seed.</param>
    /// <param name="tick">The game tick the roll is made on.</param>
    /// <param name="id">A stable identity for the roll, such as <see cref="RollId.StableHash"/>.</param>
    /// <param name="counter">Tells apart rolls that share every other input, such as retries: 0 for the first, 1 for the next, and so on.</param>
    /// <returns>The combined hash.</returns>
    public static ulong Hash(ulong seed, long tick, ulong id, int counter)
    {
        var h = Mix(seed);
        h = Combine(h, (ulong)tick, 1);
        h = Combine(h, id, 2);
        h = Combine(h, (ulong)(uint)counter, 3);
        return h;
    }

    /// <summary>Combines any number of 64-bit values, in order, into one.</summary>
    /// <param name="first">The first value.</param>
    /// <param name="rest">The values combined after <paramref name="first"/>, in order.</param>
    /// <returns>The combined hash.</returns>
    public static ulong Hash(ulong first, params ReadOnlySpan<ulong> rest)
    {
        var h = Mix(first);
        for (var i = 0; i < rest.Length; i++)
            h = Combine(h, rest[i], (ulong)i + 1);
        return h;
    }

    /// <summary>A D100 roll from 1 to 100 (100 is "00").</summary>
    /// <param name="seed">The save's seed.</param>
    /// <param name="tick">The game tick the roll is made on.</param>
    /// <param name="key">A stable identity for the roll, such as <see cref="RollId.StableHash"/>.</param>
    /// <param name="counter">Tells apart rolls that share every other input, such as retries: 0 for the first, 1 for the next, and so on.</param>
    /// <returns>The roll, from 1 to 100.</returns>
    public static int D100(ulong seed, long tick, ulong key, int counter = 0) =>
        Below(Hash(seed, tick, key, counter), 100) + 1;

    /// <summary>Maps a hash to 0 .. n-1 using its high bits, with no modulo bias worth measuring.</summary>
    /// <param name="hash">A well-mixed 64-bit value, such as one from <see cref="Hash(ulong, long, ulong, int)"/>.</param>
    /// <param name="n">How many results there are. Must be greater than zero.</param>
    /// <returns>A value from 0 to <paramref name="n"/> - 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is zero or negative.</exception>
    public static int Below(ulong hash, int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
        return (int)Math.BigMul(hash, (ulong)n, out _);
    }

    /// <summary>
    /// A 64-bit FNV-1a hash of the string's UTF-8 bytes. Unlike
    /// <see cref="string.GetHashCode()"/>, it is the same in every process.
    /// </summary>
    /// <param name="text">The string to hash.</param>
    /// <returns>The 64-bit hash.</returns>
    public static ulong StableHash(string text)
    {
        var h = 0xCBF29CE484222325UL;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            h ^= b;
            h *= 0x100000001B3UL;
        }
        return h;
    }

    private static ulong Combine(ulong h, ulong value, ulong position) =>
        Mix(h ^ Mix(value + Golden * position));

    private static ulong Mix(ulong z)
    {
        z += Golden;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }
}
