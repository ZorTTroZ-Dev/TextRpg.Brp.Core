using System;
using System.Collections.Generic;
using System.Linq;
using Brp.Core.Dice;
using Brp.Core.Skills;
using NUnit.Framework;

namespace Brp.Core.Tests.Skills;

public class SkillRollTests
{
    // The Skill Results Table (BRP Universal Game Engine v1.05, p. 110): for every
    // chance in the band, the top of the critical and special ranges and the
    // first fumbling roll (100 is "00").
    [TestCase(1, 5, 1, 1, 96)]
    [TestCase(6, 10, 1, 2, 96)]
    [TestCase(11, 15, 1, 3, 96)]
    [TestCase(16, 20, 1, 4, 96)]
    [TestCase(21, 25, 2, 5, 97)]
    [TestCase(26, 30, 2, 6, 97)]
    [TestCase(31, 35, 2, 7, 97)]
    [TestCase(36, 40, 2, 8, 97)]
    [TestCase(41, 45, 3, 9, 98)]
    [TestCase(46, 50, 3, 10, 98)]
    [TestCase(51, 55, 3, 11, 98)]
    [TestCase(56, 60, 3, 12, 98)]
    [TestCase(61, 65, 4, 13, 99)]
    [TestCase(66, 70, 4, 14, 99)]
    [TestCase(71, 75, 4, 15, 99)]
    [TestCase(76, 80, 4, 16, 99)]
    [TestCase(81, 85, 5, 17, 100)]
    [TestCase(86, 90, 5, 18, 100)]
    [TestCase(91, 95, 5, 19, 100)]
    [TestCase(96, 100, 5, 20, 100)]
    [TestCase(101, 105, 6, 21, 100)]
    [TestCase(106, 110, 6, 22, 100)]
    [TestCase(111, 115, 6, 23, 100)]
    [TestCase(116, 120, 6, 24, 100)]
    public void RangesMatchTheSkillResultsTable(int low, int high, int critical, int special, int firstFumble)
    {
        Assert.Multiple(() =>
        {
            for (var chance = low; chance <= high; chance++)
            {
                Assert.That(SkillRoll.CriticalRange(chance), Is.EqualTo(critical), $"critical at {chance}%");
                Assert.That(SkillRoll.SpecialRange(chance), Is.EqualTo(special), $"special at {chance}%");
                Assert.That(101 - SkillRoll.FumbleCount(chance), Is.EqualTo(firstFumble), $"fumble at {chance}%");
            }
        });
    }

    [TestCase(70, 1, SuccessDegree.Critical)]
    [TestCase(70, 4, SuccessDegree.Critical)]
    [TestCase(70, 5, SuccessDegree.Special)]
    [TestCase(70, 14, SuccessDegree.Special)]
    [TestCase(70, 15, SuccessDegree.Success)]
    [TestCase(70, 70, SuccessDegree.Success)]
    [TestCase(70, 71, SuccessDegree.Failure)]
    [TestCase(70, 98, SuccessDegree.Failure)]
    [TestCase(70, 99, SuccessDegree.Fumble)]
    [TestCase(70, 100, SuccessDegree.Fumble)]
    public void EvaluatesEachDegree(int chance, int roll, SuccessDegree expected) =>
        Assert.That(SkillRoll.Evaluate(roll, chance, chance), Is.EqualTo(expected));

    [TestCase(96)]
    [TestCase(97)]
    [TestCase(98)]
    [TestCase(99)]
    public void NinetySixOrHigherAlwaysFails(int roll) =>
        Assert.That(SkillRoll.Evaluate(roll, 150, 150), Is.EqualTo(SuccessDegree.Failure));

    [TestCase(0)]
    [TestCase(50)]
    [TestCase(200)]
    public void DoubleZeroAlwaysFumbles(int chance) =>
        Assert.That(SkillRoll.Evaluate(100, chance, chance), Is.EqualTo(SuccessDegree.Fumble));

    [Test]
    public void OneToFiveSucceedsWhenModifiersPushASkilledChanceBelowFive()
    {
        // A 10% skill made Difficult and penalised to 2%.
        var chance = SkillRoll.Chance(10, Difficulty.Difficult, -3);
        Assert.Multiple(() =>
        {
            Assert.That(chance, Is.EqualTo(2));
            Assert.That(SkillRoll.Evaluate(5, 10, chance), Is.EqualTo(SuccessDegree.Success));
            Assert.That(SkillRoll.Evaluate(6, 10, chance), Is.EqualTo(SuccessDegree.Failure));
        });
    }

    [Test]
    public void OneToFiveDoesNotSucceedForASkillRatedBelowFive() =>
        Assert.That(SkillRoll.Evaluate(3, 2, 2), Is.EqualTo(SuccessDegree.Failure));

    [Test]
    public void ZeroChanceNeverCrits() =>
        Assert.That(SkillRoll.Evaluate(1, 0, 0), Is.EqualTo(SuccessDegree.Failure));

    [TestCase(45, Difficulty.Average, 0, 45)]
    [TestCase(45, Difficulty.Easy, 0, 90)]
    [TestCase(45, Difficulty.Difficult, 0, 23)]
    [TestCase(45, Difficulty.Difficult, -10, 13)]
    [TestCase(45, Difficulty.Easy, 10, 100)]
    public void DifficultyThenSituationalModifiers(int rating, Difficulty difficulty, int situational, int expected) =>
        Assert.That(SkillRoll.Chance(rating, difficulty, situational), Is.EqualTo(expected));

    [TestCase(0)]
    [TestCase(101)]
    public void RejectsRollsOutsideD100(int roll) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => SkillRoll.Evaluate(roll, 50, 50));

    [Test]
    public void SameInputsAlwaysGiveTheSameResult()
    {
        var id = new RollId("intent.persuade", "pc", "ana");
        var first = SkillRoll.Roll(42, 7200, id, 60, 60);
        for (var i = 0; i < 100; i++)
            Assert.That(SkillRoll.Roll(42, 7200, id, 60, 60), Is.EqualTo(first));
    }

    // Over many seeds, each degree turns up as often as the ranges say it should.
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(20)]
    [TestCase(45)]
    [TestCase(70)]
    [TestCase(97)]
    [TestCase(120)]
    public void DegreeRatesMatchTheOdds(int chance)
    {
        const int rolls = 200_000;
        var id = new RollId("skill.spot", "pc", "locker_3");
        var counts = new Dictionary<SuccessDegree, int>();
        for (ulong seed = 0; seed < rolls; seed++)
        {
            var degree = SkillRoll.Roll(seed, 0, id, chance, chance).Degree;
            counts[degree] = counts.GetValueOrDefault(degree) + 1;
        }

        // Expected rolls out of 100 for each degree, counted from the rules.
        var expected = Enum.GetValues<SuccessDegree>().ToDictionary(d => d, _ => 0);
        for (var roll = 1; roll <= 100; roll++)
            expected[SkillRoll.Evaluate(roll, chance, chance)]++;

        Assert.Multiple(() =>
        {
            foreach (var (degree, outOf100) in expected)
            {
                var p = outOf100 / 100.0;
                var observed = counts.GetValueOrDefault(degree) / (double)rolls;
                // Five standard errors; a degree with no rolls in range must never appear.
                var tolerance = 5 * Math.Sqrt(p * (1 - p) / rolls);
                Assert.That(observed, Is.EqualTo(p).Within(tolerance), $"{degree} at {chance}%");
            }
        });
    }
}
