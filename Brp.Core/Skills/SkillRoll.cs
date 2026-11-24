using System;
using Brp.Core.Dice;

namespace Brp.Core.Skills;

/// <summary>Degrees of success, ordered from worst to best so they compare naturally.</summary>
public enum SuccessDegree
{
    /// <summary>A roll in the fumble range at the top of the dice (see <see cref="SkillRoll.FumbleCount"/>). 00 always fumbles.</summary>
    Fumble,

    /// <summary>A roll above the chance, or 96 or higher whatever the chance, that is not a fumble.</summary>
    Failure,

    /// <summary>A roll at or below the chance that is not a special or a critical.</summary>
    Success,

    /// <summary>A success at or below 1/5 of the chance (see <see cref="SkillRoll.SpecialRange"/>) that is not a critical.</summary>
    Special,

    /// <summary>A success at or below 1/20 of the chance (see <see cref="SkillRoll.CriticalRange"/>).</summary>
    Critical,
}

/// <summary>Difficulty levels that change the chance. Automatic and Impossible need no roll, so they are left to the caller.</summary>
public enum Difficulty
{
    /// <summary>The rating is doubled.</summary>
    Easy,

    /// <summary>The rating is used as it is.</summary>
    Average,

    /// <summary>The rating is halved, rounded up.</summary>
    Difficult,
}

/// <summary>The roll, the chance it was made against, and the degree of success.</summary>
/// <param name="Roll">The D100 roll, from 1 to 100 (100 is "00").</param>
/// <param name="Chance">The modified chance the roll was made against.</param>
/// <param name="Degree">The degree of success.</param>
public readonly record struct SkillRollResult(int Roll, int Chance, SuccessDegree Degree)
{
    /// <summary>Whether the roll succeeded: a <see cref="SuccessDegree.Success"/>, <see cref="SuccessDegree.Special"/>, or <see cref="SuccessDegree.Critical"/>.</summary>
    public bool Succeeded => Degree >= SuccessDegree.Success;
}

/// <summary>
/// The BRP skill roll, from Basic Roleplaying: Universal Game Engine v1.05,
/// Chapter Five: System: Evaluating Success or Failure (pp. 109–110), the
/// Skill Results Table (p. 110), Skill Rolls (p. 110), and Modifying Action
/// Rolls (pp. 113–114).
/// </summary>
public static class SkillRoll
{
    /// <summary>
    /// The chance to roll against: the rating doubled for Easy or halved
    /// (rounded up) for Difficult, then the situational modifiers added, which
    /// BRP applies after difficulty so they are not doubled or halved (p. 114).
    /// Permanent modifiers belong in <paramref name="rating"/>, since BRP
    /// doubles or halves them with the rest of the skill.
    /// </summary>
    /// <param name="rating">The skill rating, including permanent modifiers.</param>
    /// <param name="difficulty">How hard the task is.</param>
    /// <param name="situational">The sum of the situational modifiers, added after difficulty.</param>
    /// <returns>The chance to roll against. It is not clamped, so it can be below 0 or above 100.</returns>
    public static int Chance(int rating, Difficulty difficulty = Difficulty.Average, int situational = 0)
    {
        var chance = difficulty switch
        {
            Difficulty.Easy => rating * 2,
            Difficulty.Difficult => CeilDiv(rating, 2),
            _ => rating,
        };
        return chance + situational;
    }

    /// <summary>Makes a keyed skill roll: the same inputs always give the same result.</summary>
    /// <param name="seed">The save's seed.</param>
    /// <param name="tick">The game tick the roll is made on.</param>
    /// <param name="id">What is rolled, by whom, and against whom or what.</param>
    /// <param name="rating">The unmodified skill rating, which decides whether 01–05 always succeeds.</param>
    /// <param name="chance">The modified chance from <see cref="Chance"/>.</param>
    /// <param name="counter">Tells apart rolls that share every other input, such as retries: 0 for the first, 1 for the next, and so on.</param>
    /// <returns>The roll, the chance it was made against, and the degree of success.</returns>
    public static SkillRollResult Roll(ulong seed, long tick, RollId id, int rating, int chance, int counter = 0)
    {
        var roll = KeyedRng.D100(seed, tick, id.StableHash, counter);
        return new SkillRollResult(roll, chance, Evaluate(roll, rating, chance));
    }

    /// <summary>Evaluates a D100 roll (1–100, where 100 is "00") against a chance.</summary>
    /// <param name="roll">The D100 roll, from 1 to 100.</param>
    /// <param name="rating">The unmodified skill rating, which decides whether 01–05 always succeeds.</param>
    /// <param name="chance">The modified chance from <see cref="Chance"/>.</param>
    /// <returns>The degree of success.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="roll"/> is below 1 or above 100.</exception>
    public static SuccessDegree Evaluate(int roll, int rating, int chance)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roll, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(roll, 100);

        if (roll > 100 - FumbleCount(chance)) return SuccessDegree.Fumble;

        // However high the chance, 96 or higher fails.
        if (roll >= 96) return SuccessDegree.Failure;

        // A skill normally rated 5% or higher always succeeds on 01–05, even
        // when modifiers take the chance below 5%.
        var success = roll <= chance || (roll <= 5 && rating >= 5);
        if (!success) return SuccessDegree.Failure;

        if (roll <= CriticalRange(chance)) return SuccessDegree.Critical;
        if (roll <= SpecialRange(chance)) return SuccessDegree.Special;
        return SuccessDegree.Success;
    }

    // Rounding: the text on p. 110 says to round 1/5 and 1/20 "normally" (.5
    // and up rounds up), but the Skill Results Table on the same page and the
    // combat rules on p. 126 round up. Rounding up is used because it is the
    // only reading that reproduces the table (a 21% chance crits on 01–02).

    /// <summary>Rolls from 01 up to this value are critical: 1/20 of the chance, rounded up.</summary>
    /// <param name="chance">The modified chance.</param>
    /// <returns>The highest roll that is a critical, or 0 when the chance is 0 or less.</returns>
    public static int CriticalRange(int chance) => chance <= 0 ? 0 : CeilDiv(chance, 20);

    /// <summary>Rolls from 01 up to this value are special (critical included): 1/5 of the chance, rounded up.</summary>
    /// <param name="chance">The modified chance.</param>
    /// <returns>The highest roll that is a special, or 0 when the chance is 0 or less.</returns>
    public static int SpecialRange(int chance) => chance <= 0 ? 0 : CeilDiv(chance, 5);

    /// <summary>
    /// How many rolls at the top of the range (counting down from 00) fumble:
    /// 1/20 of the chance of failure. The rule gives no rounding, so this
    /// reproduces the Skill Results Table, which rounds exact multiples up a
    /// step (a 20% chance fumbles on 96–00). 00 always fumbles, and the table
    /// never fumbles on more than 96–00.
    /// </summary>
    /// <param name="chance">The modified chance.</param>
    /// <returns>How many rolls fumble, from 1 (00 only) to 5 (96–00).</returns>
    public static int FumbleCount(int chance)
    {
        var effective = Math.Max(chance, 1);
        return Math.Max((100 - effective) / 20 + 1, 1);
    }

    private static int CeilDiv(int value, int divisor) => (value + divisor - 1) / divisor;
}
