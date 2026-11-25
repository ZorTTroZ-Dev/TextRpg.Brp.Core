using Brp.Core.Dice;
using NUnit.Framework;

namespace Brp.Core.Tests.Dice;

public class RollIdTests
{
    #region StableHash

    [Test]
    public void StableHash_PersuadeId_ReturnsPinnedHash()
    {
        var persuade = new RollId { Kind = "intent.persuade", Actor = "pc", Target = "ana" };
        Assert.That(persuade.StableHash, Is.EqualTo(0xC1DF890D5EB5D74DUL));
    }

    [Test]
    public void StableHash_SameTextSplitDifferently_ReturnsDifferentHashes()
    {
        var first = new RollId { Kind = "ab", Actor = "c", Target = "d" };
        var second = new RollId { Kind = "a", Actor = "bc", Target = "d" };
        Assert.That(first.StableHash, Is.Not.EqualTo(second.StableHash));
    }

    #endregion
}
