using NUnit.Framework;
using BS.Npc;
using BS.Quest;

public class NpcCoreTests
{
    [Test] public void CampFactsAreLocalAndEmptyBeforeRun()
    {
        string facts = FactBlockBuilder.Build(null, null);
        StringAssert.Contains("当前背包：空", facts);
        StringAssert.DoesNotContain("可修改", facts);
        Assert.IsTrue(DialogueRouter.TryRoute(DialogueSurface.Camp, out int campLimit));
        Assert.AreEqual(600, campLimit);
    }
    [Test] public void ValidatorRejectsPromisesAndAllowsPlainText()
    {
        Assert.IsTrue(NpcResponseValidator.IsSafe("合同条件以本地记录为准。"));
        Assert.IsFalse(NpcResponseValidator.IsSafe("我保证完成合同并直接给你物品。"));
        Assert.IsFalse(NpcResponseValidator.IsSafe(new string('字', 601)));
    }
}
