using System;
using System.Linq;
using Brp.Core.Dice;
using NUnit.Framework;

namespace Brp.Core.Tests.Dice;

public class KeyedRngTests
{
    private static readonly RollId Persuade = new() { Kind = "intent.persuade", Actor = "pc", Target = "ana" };
    private static readonly ulong PersuadeKey = Persuade.StableHash;

    #region D100

    [Test]
    public void D100_SameInputs_ReturnsTheSameRoll()
    {
        var first = KeyedRng.D100(42, 7200, PersuadeKey, 0);
        for (var i = 0; i < 100; i++)
        {
            Assert.That(KeyedRng.D100(42, 7200, PersuadeKey, 0), Is.EqualTo(first));
        }
    }

    // Pinned values. If one of these fails, the generator changed and every
    // roll in every existing save and replay script changed with it.
    [TestCase(42UL, 0L, 0, 11)]
    [TestCase(42UL, 7200L, 0, 33)]
    [TestCase(42UL, 7200L, 1, 18)]
    [TestCase(1234567890123UL, 75L, 0, 20)]
    [TestCase(0UL, 0L, 0, 58)]
    public void D100_PinnedInputs_ReturnsPinnedRoll(ulong seed, long tick, int counter, int expected)
    {
        Assert.That(KeyedRng.D100(seed, tick, PersuadeKey, counter), Is.EqualTo(expected));
    }

    [Test]
    public void D100_ManySeeds_StaysInRange()
    {
        for (ulong seed = 0; seed < 10_000; seed++)
        {
            var roll = KeyedRng.D100(seed, 0, PersuadeKey);
            Assert.That(roll, Is.InRange(1, 100));
        }
    }

    [Test]
    public void D100_ManySeeds_IsUniform()
    {
        // Chi-square goodness of fit over 100 faces (99 degrees of freedom).
        const int rolls = 200_000;
        var counts = new int[101];
        for (ulong seed = 0; seed < rolls; seed++)
        {
            counts[KeyedRng.D100(seed, 7200, PersuadeKey)]++;
        }

        const double expected = rolls / 100.0;
        var chiSquare = counts.Skip(1).Sum(count => (count - expected) * (count - expected) / expected);

        // The 99.9th percentile of chi-square with 99 degrees of freedom is about 148.2.
        Assert.That(chiSquare, Is.LessThan(148.2));
    }

    [Test]
    public void D100_ManyTicksForOneSeed_IsUniform()
    {
        const int rolls = 200_000;
        var counts = new int[101];
        for (long tick = 0; tick < rolls; tick++)
        {
            counts[KeyedRng.D100(42, tick, PersuadeKey)]++;
        }

        const double expected = rolls / 100.0;
        var chiSquare = counts.Skip(1).Sum(count => (count - expected) * (count - expected) / expected);
        Assert.That(chiSquare, Is.LessThan(148.2));
    }

    #endregion

    #region Hash

    [Test]
    public void Hash_EachInputChanged_ChangesTheHash()
    {
        var baseline = KeyedRng.Hash(42, 7200, Persuade.StableHash, 0);
        Assert.That(KeyedRng.Hash(43, 7200, Persuade.StableHash, 0), Is.Not.EqualTo(baseline));
        Assert.That(KeyedRng.Hash(42, 7201, Persuade.StableHash, 0), Is.Not.EqualTo(baseline));
        Assert.That(KeyedRng.Hash(42, 7200, Persuade.StableHash + 1, 0), Is.Not.EqualTo(baseline));
        Assert.That(KeyedRng.Hash(42, 7200, Persuade.StableHash, 1), Is.Not.EqualTo(baseline));
    }

    #endregion

    #region StableHash

    [Test]
    public void StableHash_Text_IsFnv1a()
    {
        Assert.That(KeyedRng.StableHash("ana"), Is.EqualTo(0xE6F79C19051FFB05UL));
    }

    #endregion

    #region Below

    [Test]
    public void Below_ZeroCount_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KeyedRng.Below(123, 0));
    }

    #endregion
}
