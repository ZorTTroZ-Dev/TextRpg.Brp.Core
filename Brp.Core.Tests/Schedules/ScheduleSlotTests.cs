using System;
using Brp.Core.Schedules;
using NUnit.Framework;

namespace Brp.Core.Tests.Schedules;

public class ScheduleSlotTests
{
    #region Constructor

    [Test]
    public void Constructor_StartPastTheLastBlock_ThrowsArgumentOutOfRange()
    {
        Assert.That(() => new ScheduleSlot<string>(96, "past midnight"), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_NoVariants_ThrowsArgumentException()
    {
        Assert.That(() => new ScheduleSlot<string>(0, Array.Empty<string>()), Throws.InstanceOf<ArgumentException>());
    }

    #endregion
}
