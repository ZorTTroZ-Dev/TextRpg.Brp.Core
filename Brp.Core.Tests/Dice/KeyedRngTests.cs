using System;
using System.Linq;
using Brp.Core.Dice;
using NUnit.Framework;

namespace Brp.Core.Tests.Dice;

public class KeyedRngTests
{
    private static readonly RollId Persuade = new("intent.persuade", "pc", "ana");
    private static readonly ulong PersuadeKey = Persuade.StableHash;

    [Test]
    public void SameInputsAlwaysGiveTheSameRoll()
    {
        var first = KeyedRng.D100(42, 7200, PersuadeKey, 0);
        for (var i = 0; i < 100; i++)
            Assert.That(KeyedRng.D100(42, 7200, PersuadeKey, 0), Is.EqualTo(first));
    }

    // Pinned values. If one of these fails, the generator changed and every
    // roll in every existing save and replay script changed with it.
    [TestCase(42UL, 0L, 0, 11)]
    [TestCase(42UL, 7200L, 0, 33)]
    [TestCase(42UL, 7200L, 1, 18)]
    [TestCase(1234567890123UL, 75L, 0, 20)]
    [TestCase(0UL, 0L, 0, 58)]
    public void RollsMatchPinnedValues(ulong seed, long tick, int counter, int expected) =>
        Assert.That(KeyedRng.D100(seed, tick, PersuadeKey, counter), Is.EqualTo(expected));

    [Test]
    public void StableHashIsFnv1a() =>
        Assert.That(KeyedRng.StableHash("ana"), Is.EqualTo(0xE6F79C19051FFB05UL));

    [Test]
    public void RollIdHashIsPinned() =>
        Assert.That(Persuade.StableHash, Is.EqualTo(0xC1DF890D5EB5D74DUL));

    [Test]
    public void RollIdFieldsDoNotRunTogether() =>
        Assert.That(new RollId("ab", "c", "d").StableHash,
            Is.Not.EqualTo(new RollId("a", "bc", "d").StableHash));

    [Test]
    public void EachInputChangesTheHash()
    {
        var baseline = KeyedRng.Hash(42, 7200, Persuade.StableHash, 0);
        Assert.Multiple(() =>
        {
            Assert.That(KeyedRng.Hash(43, 7200, Persuade.StableHash, 0), Is.Not.EqualTo(baseline));
            Assert.That(KeyedRng.Hash(42, 7201, Persuade.StableHash, 0), Is.Not.EqualTo(baseline));
            Assert.That(KeyedRng.Hash(42, 7200, Persuade.StableHash + 1, 0), Is.Not.EqualTo(baseline));
            Assert.That(KeyedRng.Hash(42, 7200, Persuade.StableHash, 1), Is.Not.EqualTo(baseline));
        });
    }

    [Test]
    public void D100StaysInRange()
    {
        for (ulong seed = 0; seed < 10_000; seed++)
        {
            var roll = KeyedRng.D100(seed, 0, PersuadeKey);
            Assert.That(roll, Is.InRange(1, 100));
        }
    }

    [Test]
    public void D100IsUniformAcrossSeeds()
    {
        // Chi-square goodness of fit over 100 faces (99 degrees of freedom).
        const int rolls = 200_000;
        var counts = new int[101];
        for (ulong seed = 0; seed < rolls; seed++)
            counts[KeyedRng.D100(seed, 7200, PersuadeKey)]++;

        const double expected = rolls / 100.0;
        var chiSquare = counts.Skip(1).Sum(c => (c - expected) * (c - expected) / expected);

        // The 99.9th percentile of chi-square with 99 degrees of freedom is about 148.2.
        Assert.That(chiSquare, Is.LessThan(148.2));
    }

    [Test]
    public void D100IsUniformAcrossTicksForOneSeed()
    {
        const int rolls = 200_000;
        var counts = new int[101];
        for (long tick = 0; tick < rolls; tick++)
            counts[KeyedRng.D100(42, tick, PersuadeKey)]++;

        const double expected = rolls / 100.0;
        var chiSquare = counts.Skip(1).Sum(c => (c - expected) * (c - expected) / expected);
        Assert.That(chiSquare, Is.LessThan(148.2));
    }

    [Test]
    public void BelowRejectsNonPositiveRange() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => KeyedRng.Below(123, 0));
}
