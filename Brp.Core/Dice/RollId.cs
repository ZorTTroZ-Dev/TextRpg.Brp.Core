namespace Brp.Core.Dice;

/// <summary>
/// The stable identity of a roll: what kind of roll it is, who makes it, and
/// against whom or what. Uses the game's stable string IDs (such as "ana" or
/// "crate_14c"), so the same action always keys the same roll.
/// </summary>
/// <param name="Kind">What kind of roll it is, such as "intent.persuade".</param>
/// <param name="Actor">The stable ID of who makes the roll, such as "pc".</param>
/// <param name="Target">The stable ID of whom or what the roll is against, such as "ana".</param>
public readonly record struct RollId(string Kind, string Actor, string Target)
{
    /// <summary>
    /// A hash that is the same in every process. Each field is hashed on its
    /// own, so ("ab", "c") and ("a", "bc") give different values.
    /// </summary>
    public ulong StableHash => KeyedRng.Hash(
        KeyedRng.StableHash(Kind),
        KeyedRng.StableHash(Actor),
        KeyedRng.StableHash(Target));
}
