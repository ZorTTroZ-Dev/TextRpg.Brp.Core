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
    private static readonly object[] SkillResultsTable =
    {
        new object[] { 1, 5, 1, 1, 96 },
        new object[] { 6, 10, 1, 2, 96 },
        new object[] { 11, 15, 1, 3, 96 },
        new object[] { 16, 20, 1, 4, 96 },
        new object[] { 21, 25, 2, 5, 97 },
        new object[] { 26, 30, 2, 6, 97 },
        new object[] { 31, 35, 2, 7, 97 },
        new object[] { 36, 40, 2, 8, 97 },
        new object[] { 41, 45, 3, 9, 98 },
        new object[] { 46, 50, 3, 10, 98 },
        new object[] { 51, 55, 3, 11, 98 },
        new object[] { 56, 60, 3, 12, 98 },
        new object[] { 61, 65, 4, 13, 99 },
        new object[] { 66, 70, 4, 14, 99 },
        new object[] { 71, 75, 4, 15, 99 },
        new object[] { 76, 80, 4, 16, 99 },
        new object[] { 81, 85, 5, 17, 100 },
        new object[] { 86, 90, 5, 18, 100 },
        new object[] { 91, 95, 5, 19, 100 },
        new object[] { 96, 100, 5, 20, 100 },
        new object[] { 101, 105, 6, 21, 100 },
        new object[] { 106, 110, 6, 22, 100 },
        new object[] { 111, 115, 6, 23, 100 },
        new object[] { 116, 120, 6, 24, 100 },
    };

    #region CriticalRange

    [TestCaseSource(nameof(SkillResultsTable))]
    public void CriticalRange_ChanceInTableBand_MatchesTheSkillResultsTable(int low, int high, int critical, int special, int firstFumble)
    {
        for (var chance = low; chance <= high; chance++)
        {
            Assert.That(SkillRoll.CriticalRange(chance), Is.EqualTo(critical), $"critical at {chance}%");
        }
    }

    #endregion

    #region SpecialRange

    [TestCaseSource(nameof(SkillResultsTable))]
    public void SpecialRange_ChanceInTableBand_MatchesTheSkillResultsTable(int low, int high, int critical, int special, int firstFumble)
    {
        for (var chance = low; chance <= high; chance++)
        {
            Assert.That(SkillRoll.SpecialRange(chance), Is.EqualTo(special), $"special at {chance}%");
        }
    }

    #endregion

    #region FumbleCount

    [TestCaseSource(nameof(SkillResultsTable))]
    public void FumbleCount_ChanceInTableBand_MatchesTheSkillResultsTable(int low, int high, int critical, int special, int firstFumble)
    {
        for (var chance = low; chance <= high; chance++)
        {
            Assert.That(101 - SkillRoll.FumbleCount(chance), Is.EqualTo(firstFumble), $"fumble at {chance}%");
        }
    }

    #endregion

    #region Evaluate

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
    public void Evaluate_RollInEachRange_ReturnsThatDegree(int chance, int roll, SuccessDegree expected)
    {
        Assert.That(SkillRoll.Evaluate(roll, chance, chance), Is.EqualTo(expected));
    }

    [TestCase(96)]
    [TestCase(97)]
    [TestCase(98)]
    [TestCase(99)]
    public void Evaluate_NinetySixOrHigherAgainstAHighChance_ReturnsFailure(int roll)
    {
        Assert.That(SkillRoll.Evaluate(roll, 150, 150), Is.EqualTo(SuccessDegree.Failure));
    }

    [TestCase(0)]
    [TestCase(50)]
    [TestCase(200)]
    public void Evaluate_DoubleZero_ReturnsFumble(int chance)
    {
        Assert.That(SkillRoll.Evaluate(100, chance, chance), Is.EqualTo(SuccessDegree.Fumble));
    }

    [Test]
    public void Evaluate_ModifiersPushASkilledChanceBelowFive_OneToFiveSucceeds()
    {
        // A 10% skill made Difficult and penalised to 2%.
        var chance = SkillRoll.Chance(10, Difficulty.Difficult, -3);
        Assert.That(chance, Is.EqualTo(2));
        Assert.That(SkillRoll.Evaluate(5, 10, chance), Is.EqualTo(SuccessDegree.Success));
        Assert.That(SkillRoll.Evaluate(6, 10, chance), Is.EqualTo(SuccessDegree.Failure));
    }

    [Test]
    public void Evaluate_SkillRatedBelowFive_OneToFiveFails()
    {
        Assert.That(SkillRoll.Evaluate(3, 2, 2), Is.EqualTo(SuccessDegree.Failure));
    }

    [Test]
    public void Evaluate_ZeroChance_NeverCrits()
    {
        Assert.That(SkillRoll.Evaluate(1, 0, 0), Is.EqualTo(SuccessDegree.Failure));
    }

    [TestCase(0)]
    [TestCase(101)]
    public void Evaluate_RollOutsideD100_ThrowsArgumentOutOfRange(int roll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SkillRoll.Evaluate(roll, 50, 50));
    }

    #endregion

    #region Chance

    [TestCase(45, Difficulty.Average, 0, 45)]
    [TestCase(45, Difficulty.Easy, 0, 90)]
    [TestCase(45, Difficulty.Difficult, 0, 23)]
    [TestCase(45, Difficulty.Difficult, -10, 13)]
    [TestCase(45, Difficulty.Easy, 10, 100)]
    public void Chance_DifficultyAndSituationalModifiers_AppliesDifficultyFirst(int rating, Difficulty difficulty, int situational, int expected)
    {
        Assert.That(SkillRoll.Chance(rating, difficulty, situational), Is.EqualTo(expected));
    }

    #endregion

    #region Roll

    [Test]
    public void Roll_SameInputs_ReturnsTheSameResult()
    {
        var id = new RollId { Kind = "intent.persuade", Actor = "pc", Target = "ana" };
        var first = SkillRoll.Roll(42, 7200, id, 60, 60);
        for (var i = 0; i < 100; i++)
        {
            Assert.That(SkillRoll.Roll(42, 7200, id, 60, 60), Is.EqualTo(first));
        }
    }

    // Over many seeds, each degree turns up as often as the ranges say it should.
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(20)]
    [TestCase(45)]
    [TestCase(70)]
    [TestCase(97)]
    [TestCase(120)]
    public void Roll_ManySeeds_DegreeRatesMatchTheOdds(int chance)
    {
        const int rolls = 200_000;
        var id = new RollId { Kind = "skill.spot", Actor = "pc", Target = "locker_3" };
        var counts = new Dictionary<SuccessDegree, int>();
        for (ulong seed = 0; seed < rolls; seed++)
        {
            var degree = SkillRoll.Roll(seed, 0, id, chance, chance).Degree;
            counts[degree] = counts.GetValueOrDefault(degree) + 1;
        }

        // Expected rolls out of 100 for each degree, counted from the rules.
        var expected = Enum.GetValues<SuccessDegree>().ToDictionary(degree => degree, _ => 0);
        for (var roll = 1; roll <= 100; roll++)
        {
            expected[SkillRoll.Evaluate(roll, chance, chance)]++;
        }

        foreach (var (degree, outOf100) in expected)
        {
            var probability = outOf100 / 100.0;
            var observed = counts.GetValueOrDefault(degree) / (double)rolls;
            // Five standard errors; a degree with no rolls in range must never appear.
            var tolerance = 5 * Math.Sqrt(probability * (1 - probability) / rolls);
            Assert.That(observed, Is.EqualTo(probability).Within(tolerance), $"{degree} at {chance}%");
        }
    }

    #endregion
}
