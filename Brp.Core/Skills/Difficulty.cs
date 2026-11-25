namespace Brp.Core.Skills;

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
