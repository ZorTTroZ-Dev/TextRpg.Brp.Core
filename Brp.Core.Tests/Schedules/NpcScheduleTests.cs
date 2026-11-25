using System;
using System.Collections.Generic;
using Brp.Core.Schedules;
using NUnit.Framework;
using static Brp.Core.Tests.Schedules.ScheduleResolverTests;

namespace Brp.Core.Tests.Schedules;

public class NpcScheduleTests
{
    #region Constructor

    [Test]
    public void Constructor_DayOfWeekOutOfRange_ThrowsArgumentOutOfRange()
    {
        var byDayOfWeek = new Dictionary<int, DayPlan<string>> { [7] = RestDay };
        Assert.That(() => new NpcSchedule<string, Flags>("ana", Weekday, byDayOfWeek),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    #endregion

    #region PlanFor

    [Test]
    public void PlanFor_DayWithItsOwnPlan_ReturnsThatPlan()
    {
        var schedule = new NpcSchedule<string, Flags>("ana", Weekday, new Dictionary<int, DayPlan<string>> { [6] = RestDay });
        Assert.That(schedule.PlanFor(13), Is.SameAs(RestDay));
        Assert.That(schedule.PlanFor(14), Is.SameAs(Weekday));
    }

    #endregion
}
