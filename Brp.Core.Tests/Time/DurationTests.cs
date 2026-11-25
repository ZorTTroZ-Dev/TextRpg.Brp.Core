using System;
using Brp.Core.Time;
using NUnit.Framework;

namespace Brp.Core.Tests.Time;

public class DurationTests
{
    #region Constants

    [Test]
    public void Constants_TickBlockAndDayLengths_MatchTheDesign()
    {
        Assert.That(Duration.SecondsPerTick, Is.EqualTo(12));
        Assert.That(Duration.TicksPerBlock, Is.EqualTo(75));
        Assert.That(Duration.TicksPerDay, Is.EqualTo(7200));
    }

    #endregion

    #region Constructor

    [Test]
    public void Constructor_NegativeTicks_ThrowsArgumentOutOfRange()
    {
        Assert.That(() => new Duration(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    #endregion

    #region FromSeconds

    [TestCase(0, 0L)]
    [TestCase(24, 2L)]
    [TestCase(60, 5L)]
    [TestCase(900, 75L)]
    public void FromSeconds_WholeTicks_ReturnsTicks(int seconds, long ticks)
    {
        Assert.That(Duration.FromSeconds(seconds).Ticks, Is.EqualTo(ticks));
    }

    [TestCase(1)]
    [TestCase(30)]
    [TestCase(-12)]
    public void FromSeconds_NotWholeTicks_ThrowsArgumentException(int seconds)
    {
        Assert.That(() => Duration.FromSeconds(seconds), Throws.InstanceOf<ArgumentException>());
    }

    #endregion

    #region FromMinutes

    [Test]
    public void FromMinutes_FifteenMinutes_ReturnsOneBlock()
    {
        Assert.That(Duration.FromMinutes(15).Ticks, Is.EqualTo(75));
    }

    #endregion

    #region FromHours

    [Test]
    public void FromHours_TwentyFourHours_ReturnsOneDay()
    {
        Assert.That(Duration.FromHours(24).Ticks, Is.EqualTo(7200));
    }

    #endregion

    #region FromRounds

    [Test]
    public void FromRounds_ThreeRounds_ReturnsThreeTicks()
    {
        Assert.That(Duration.FromRounds(3).Ticks, Is.EqualTo(3));
    }

    #endregion
}
