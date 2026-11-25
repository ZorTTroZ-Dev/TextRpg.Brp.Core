using System.Collections.Generic;
using System.Linq;
using Brp.Core.Schedules;
using Brp.Core.Time;
using NUnit.Framework;

namespace Brp.Core.Tests.Schedules;

public class ScheduleResolverTests
{
    // Flags as a simple set of names, standing in for the game's flags.
    internal record Flags(params string[] Set)
    {
        public bool Has(string name)
        {
            return Set.Contains(name);
        }
    }

    private static readonly Flags NoFlags = new();

    internal static Clock At(long day, int hour, int minute = 0)
    {
        return new Clock(day * Duration.TicksPerDay) + Duration.FromMinutes(hour * 60 + minute);
    }

    internal static int Block(int hour, int minute = 0)
    {
        return (hour * 60 + minute) / 15;
    }

    // The design document's example NPC: hauling crates in the morning, eating at noon.
    internal static readonly DayPlan<string> Weekday = new(
        new ScheduleSlot<string>(0, "quarters: asleep"),
        new ScheduleSlot<string>(Block(8), "cargo bay: hauling crates"),
        new ScheduleSlot<string>(Block(12), "mess hall: eating"),
        new ScheduleSlot<string>(Block(13), "cargo bay: hauling crates"),
        new ScheduleSlot<string>(Block(22), "quarters: asleep"));

    internal static readonly DayPlan<string> RestDay = new(
        new ScheduleSlot<string>(0, "quarters: asleep"),
        new ScheduleSlot<string>(Block(10), "observation deck: reading"));

    private static readonly ScheduleOverride<string, Flags> Fire = new()
    {
        Id = "fire_in_cargo_bay",
        Priority = 10,
        Condition = (flags, _) => flags.Has("fire"),
        Block = "security office: frightened",
    };

    private static readonly ScheduleOverride<string, Flags> Hostile = new()
    {
        Id = "hostile_to_player",
        Priority = 5,
        Condition = (flags, _) => flags.Has("hostile"),
        Block = "avoiding the player",
    };

    private static NpcSchedule<string, Flags> Ana(params ScheduleOverride<string, Flags>[] overrides)
    {
        return new NpcSchedule<string, Flags>("ana", Weekday, new Dictionary<int, DayPlan<string>> { [6] = RestDay }, overrides);
    }

    private static NpcSchedule<string, Flags> Varied(string npcId)
    {
        return new NpcSchedule<string, Flags>(npcId, new DayPlan<string>(
            new ScheduleSlot<string>(0, "asleep"),
            new ScheduleSlot<string>(Block(12), new[] { "mess hall", "gym", "chapel" }),
            new ScheduleSlot<string>(Block(14), new[] { "bridge", "engineering" })));
    }

    private static string Resolve(NpcSchedule<string, Flags> schedule, Clock at, Flags flags, ulong seed = 42)
    {
        return ScheduleResolver.Resolve(schedule, at, flags, seed).Block;
    }

    #region Resolve

    [TestCase(7, 59, "quarters: asleep")]
    [TestCase(8, 0, "cargo bay: hauling crates")]
    [TestCase(11, 59, "cargo bay: hauling crates")]
    [TestCase(12, 0, "mess hall: eating")]
    [TestCase(12, 45, "mess hall: eating")]
    [TestCase(13, 0, "cargo bay: hauling crates")]
    [TestCase(23, 59, "quarters: asleep")]
    public void Resolve_NoOverrides_FollowsTheBaselineByTheClock(int hour, int minute, string expected)
    {
        Assert.That(Resolve(Ana(), At(0, hour, minute), NoFlags), Is.EqualTo(expected));
    }

    [Test]
    public void Resolve_DayOfTheWeekWithItsOwnPlan_UsesThatPlan()
    {
        Assert.That(Resolve(Ana(), At(6, 12), NoFlags), Is.EqualTo("observation deck: reading"));
        Assert.That(Resolve(Ana(), At(13, 12), NoFlags), Is.EqualTo("observation deck: reading"));
        Assert.That(Resolve(Ana(), At(7, 12), NoFlags), Is.EqualTo("mess hall: eating"));
    }

    [Test]
    public void Resolve_OverrideConditionHolds_ReturnsTheOverride()
    {
        var resolution = ScheduleResolver.Resolve(Ana(Fire), At(0, 12), new Flags("fire"), 42);
        Assert.That(resolution.Block, Is.EqualTo("security office: frightened"));
        Assert.That(resolution.OverrideId, Is.EqualTo("fire_in_cargo_bay"));
        Assert.That(resolution.IsOverride, Is.True);
    }

    [Test]
    public void Resolve_OverrideConditionFails_ReturnsTheBaseline()
    {
        var resolution = ScheduleResolver.Resolve(Ana(Fire), At(0, 12), NoFlags, 42);
        Assert.That(resolution.Block, Is.EqualTo("mess hall: eating"));
        Assert.That(resolution.IsOverride, Is.False);
    }

    [Test]
    public void Resolve_SeveralOverridesHold_HighestPriorityWins()
    {
        var bothFlags = new Flags("fire", "hostile");
        Assert.That(Resolve(Ana(Hostile, Fire), At(0, 12), bothFlags), Is.EqualTo("security office: frightened"));
        Assert.That(Resolve(Ana(Fire, Hostile), At(0, 12), bothFlags), Is.EqualTo("security office: frightened"));
        Assert.That(Resolve(Ana(Hostile, Fire), At(0, 12), new Flags("hostile")), Is.EqualTo("avoiding the player"));
    }

    [Test]
    public void Resolve_OverridesTiedOnPriority_FirstListedWins()
    {
        var first = new ScheduleOverride<string, Flags>
        {
            Id = "a",
            Priority = 1,
            Condition = (_, _) => true,
            Block = "first",
        };
        var second = new ScheduleOverride<string, Flags>
        {
            Id = "b",
            Priority = 1,
            Condition = (_, _) => true,
            Block = "second",
        };
        Assert.That(Resolve(Ana(first, second), At(0, 12), NoFlags), Is.EqualTo("first"));
    }

    [Test]
    public void Resolve_OverrideStartsPartwayThroughABlock_InterruptsTheBlock()
    {
        // A deadline hint that starts at 12:05, partway through the 12:00 block.
        var hint = new ScheduleOverride<string, Flags>
        {
            Id = "deadline_hint",
            Priority = 1,
            Condition = (_, time) => time >= At(0, 12, 5),
            Block = "mess hall: warns about the deadline",
        };

        Assert.That(Resolve(Ana(hint), At(0, 12, 4), NoFlags), Is.EqualTo("mess hall: eating"));
        Assert.That(Resolve(Ana(hint), At(0, 12, 5), NoFlags), Is.EqualTo("mess hall: warns about the deadline"));
    }

    [Test]
    public void Resolve_DuringBoundaryProcessing_OverrideBeatsTheBaseline()
    {
        // The fire starts at 12:20. Boundary processing resolves the NPC at each
        // boundary and reports when the block changes.
        var schedule = Ana(new ScheduleOverride<string, Flags>
        {
            Id = "fire_in_cargo_bay",
            Priority = 10,
            Condition = (_, time) => time >= At(0, 12, 20),
            Block = "security office: frightened",
        });

        var current = Resolve(schedule, At(0, 11, 50), NoFlags);
        BoundaryHandler<string, string> applyScheduleChanges = (block, boundary) =>
        {
            var now = Resolve(schedule, boundary, NoFlags);
            return now == block
                ? BoundaryResult<string, string>.Nothing(block)
                : new BoundaryResult<string, string> { State = now, Events = new[] { $"{boundary.Hour:00}:{boundary.Minute:00} {now}" } };
        };

        var result = EventProcessor.Advance(current, At(0, 11, 50), Duration.FromHours(1), new[] { applyScheduleChanges });
        Assert.That(result.Events, Is.EqualTo(new[]
        {
            "12:00 mess hall: eating",
            "12:30 security office: frightened",
        }));
    }

    [Test]
    public void Resolve_SlotWithVariants_HoldsOneVariantForTheWholeSlot()
    {
        var schedule = Varied("ana");
        var picks = Enumerable.Range(0, 8)
            .Select(quarter => Resolve(schedule, At(3, 12, quarter * 15), NoFlags))
            .Distinct();
        Assert.That(picks.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Resolve_SameSeedDayAndNpc_PicksTheSameVariant()
    {
        var schedule = Varied("ana");
        var first = Enumerable.Range(0, 50).Select(day => Resolve(schedule, At(day, 12), NoFlags, 7)).ToList();
        var again = Enumerable.Range(0, 50).Select(day => Resolve(schedule, At(day, 12), NoFlags, 7)).ToList();
        Assert.That(again, Is.EqualTo(first));
    }

    [Test]
    public void Resolve_DifferentDaysSeedsOrNpcs_PicksVary()
    {
        List<string> PicksOverFiftyDays(string npcId, ulong seed)
        {
            return Enumerable.Range(0, 50).Select(day => Resolve(Varied(npcId), At(day, 12), NoFlags, seed)).ToList();
        }

        Assert.That(PicksOverFiftyDays("ana", 7).Distinct().Count(), Is.EqualTo(3));
        Assert.That(PicksOverFiftyDays("ana", 8), Is.Not.EqualTo(PicksOverFiftyDays("ana", 7)));
        Assert.That(PicksOverFiftyDays("bo", 7), Is.Not.EqualTo(PicksOverFiftyDays("ana", 7)));
    }

    [Test]
    public void Resolve_ManyDays_PicksEachVariantAboutEqually()
    {
        var schedule = Varied("ana");
        var counts = Enumerable.Range(0, 30_000)
            .GroupBy(day => Resolve(schedule, At(day, 12), NoFlags))
            .ToDictionary(group => group.Key, group => group.Count());

        // 10,000 expected each; five standard errors is about 410.
        Assert.That(counts.Values, Has.All.InRange(9_590, 10_410));
    }

    #endregion
}
