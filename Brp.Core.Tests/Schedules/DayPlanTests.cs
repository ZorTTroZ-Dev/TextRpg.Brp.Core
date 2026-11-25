using System;
using Brp.Core.Schedules;
using NUnit.Framework;

namespace Brp.Core.Tests.Schedules;

public class DayPlanTests
{
    #region Constructor

    [Test]
    public void Constructor_FirstSlotAfterMidnight_ThrowsArgumentException()
    {
        Assert.That(() => new DayPlan<string>(new ScheduleSlot<string>(4, "late start")),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Constructor_SlotsNotInIncreasingOrder_ThrowsArgumentException()
    {
        Assert.That(() => new DayPlan<string>(
                new ScheduleSlot<string>(0, "a"), new ScheduleSlot<string>(40, "b"), new ScheduleSlot<string>(40, "c")),
            Throws.InstanceOf<ArgumentException>());
    }

    #endregion
}
