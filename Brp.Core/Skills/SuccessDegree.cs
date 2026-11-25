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
