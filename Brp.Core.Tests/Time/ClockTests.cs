using System;
using System.Linq;
using Brp.Core.Time;
using NUnit.Framework;

namespace Brp.Core.Tests.Time;

public class ClockTests
{
    #region Constructor

    [Test]
    public void Constructor_NegativeTick_ThrowsArgumentOutOfRange()
    {
        Assert.That(() => new Clock(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    #endregion

    #region Properties

    [TestCase(0L, 0L, 0, 0, 0, 0)]
    [TestCase(74L, 0L, 0, 0, 14, 48)]
    [TestCase(75L, 0L, 1, 0, 15, 0)]
    [TestCase(2403L, 0L, 32, 8, 0, 36)]
    [TestCase(7199L, 0L, 95, 23, 59, 48)]
    [TestCase(7200L, 1L, 0, 0, 0, 0)]
    [TestCase(16_215L, 2L, 24, 6, 3, 0)]
    public void Properties_Tick_SplitIntoDayBlockAndTime(long tick, long day, int block, int hour, int minute, int second)
    {
        var clock = new Clock(tick);
        Assert.That(clock.Day, Is.EqualTo(day));
        Assert.That(clock.BlockOfDay, Is.EqualTo(block));
        Assert.That(clock.Hour, Is.EqualTo(hour));
        Assert.That(clock.Minute, Is.EqualTo(minute));
        Assert.That(clock.Second, Is.EqualTo(second));
    }

    #endregion

    #region ToString

    [Test]
    public void ToString_AnyClock_FormatsAsDayAndTime()
    {
        Assert.That(new Clock(16_218).ToString(), Is.EqualTo("Day 2 06:03:36"));
    }

    #endregion

    #region Operators

    [Test]
    public void Operators_AddThenSubtract_RoundTrips()
    {
        var start = new Clock(1234);
        var later = start + Duration.FromMinutes(7);
        Assert.That(later - start, Is.EqualTo(Duration.FromMinutes(7)));
    }

    [Test]
    public void Operators_SubtractALaterClock_ThrowsArgumentOutOfRange()
    {
        Assert.That(() => new Clock(5) - new Clock(6), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    #endregion

    #region BoundariesBetween

    [Test]
    public void BoundariesBetween_InsideOneBlock_ReturnsNone()
    {
        Assert.That(Clock.BoundariesBetween(new Clock(10), new Clock(74)), Is.Empty);
    }

    [Test]
    public void BoundariesBetween_EndOnABoundary_IncludesIt()
    {
        var ticks = Clock.BoundariesBetween(new Clock(10), new Clock(75)).Select(boundary => boundary.Tick);
        Assert.That(ticks, Is.EqualTo(new[] { 75L }));
    }

    [Test]
    public void BoundariesBetween_StartOnABoundary_ExcludesIt()
    {
        Assert.That(Clock.BoundariesBetween(new Clock(75), new Clock(149)), Is.Empty);
    }

    [Test]
    public void BoundariesBetween_SeveralBlocks_ReturnsThemInOrder()
    {
        var ticks = Clock.BoundariesBetween(new Clock(70), new Clock(300)).Select(boundary => boundary.Tick);
        Assert.That(ticks, Is.EqualTo(new[] { 75L, 150L, 225L, 300L }));
    }

    [Test]
    public void BoundariesBetween_AWholeDay_ReturnsNinetySixInOrder()
    {
        var boundaries = Clock.BoundariesBetween(new Clock(7200), new Clock(14_400)).ToList();
        Assert.That(boundaries, Has.Count.EqualTo(96));
        Assert.That(boundaries, Is.Ordered);
        Assert.That(boundaries.All(boundary => boundary.IsOnBoundary));
    }

    [Test]
    public void BoundariesBetween_ZeroLength_ReturnsNone()
    {
        Assert.That(Clock.BoundariesBetween(new Clock(75), new Clock(75)), Is.Empty);
    }

    [Test]
    public void BoundariesBetween_EndBeforeStart_ThrowsArgumentException()
    {
        Assert.That(() => Clock.BoundariesBetween(new Clock(100), new Clock(99)).ToList(),
            Throws.InstanceOf<ArgumentException>());
    }

    #endregion
}
