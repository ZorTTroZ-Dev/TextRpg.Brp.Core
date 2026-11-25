namespace Brp.Core.Skills;

/// <summary>The roll, the chance it was made against, and the degree of success.</summary>
public readonly record struct SkillRollResult
{
    /// <summary>The D100 roll, from 1 to 100 (100 is "00").</summary>
    public required int Roll { get; init; }

    /// <summary>The modified chance the roll was made against.</summary>
    public required int Chance { get; init; }

    /// <summary>The degree of success.</summary>
    public required SuccessDegree Degree { get; init; }

    /// <summary>Whether the roll succeeded: a <see cref="SuccessDegree.Success"/>, <see cref="SuccessDegree.Special"/>, or <see cref="SuccessDegree.Critical"/>.</summary>
    public bool Succeeded => Degree >= SuccessDegree.Success;
}
