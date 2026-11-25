namespace Brp.Core.Schedules;

/// <summary>Where an NPC is and what it is doing, and why.</summary>
/// <typeparam name="TBlock">The game's schedule block.</typeparam>
public readonly record struct ScheduleResolution<TBlock>
{
    /// <summary>The active block.</summary>
    public required TBlock Block { get; init; }

    /// <summary>The active override's ID, or null when the baseline applies.</summary>
    public required string? OverrideId { get; init; }

    /// <summary>Whether an override, not the baseline, decided the block.</summary>
    public bool IsOverride => OverrideId is not null;
}
