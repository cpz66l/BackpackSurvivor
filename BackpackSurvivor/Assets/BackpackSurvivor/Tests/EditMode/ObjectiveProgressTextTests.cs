using System.Collections.Generic;
using NUnit.Framework;
using BS.Inventory;
using BS.Quest;
public class ObjectiveProgressTextTests
{
    [TestCase(ObjectiveType.CarryTag,"当前 1 / 3")]
    [TestCase(ObjectiveType.CarryTagSet,"当前 2 / 3")]
    [TestCase(ObjectiveType.CarryRarity,"当前 2 / 3")]
    [TestCase(ObjectiveType.CarryItem,"当前 2 / 3")]
    [TestCase(ObjectiveType.CarryItemAtLevel,"当前 1 / 3")]
    [TestCase(ObjectiveType.CarryAnyItemAtLevel,"当前 2 / 3")]
    [TestCase(ObjectiveType.CarryItemAnyOf,"当前 2 / 3")]
    [TestCase(ObjectiveType.KillTotal,"当前 7 / 3")]
    [TestCase(ObjectiveType.KillElite,"当前 2 / 3")]
    [TestCase(ObjectiveType.OpenChestAtLeast,"当前 1 / 3")]
    [TestCase(ObjectiveType.ReachLevel,"当前 2 / 3")]
    [TestCase(ObjectiveType.SurviveToSecond,"当前 12 / 3")]
    [TestCase(ObjectiveType.ExcludeTag,"禁带物品 1 件 / 要求 0 件")]
    [TestCase(ObjectiveType.ExcludeRarity,"禁带物品 2 件 / 要求 0 件")]
    public void CurrentValuesMatchObjectiveFilters(ObjectiveType type,string expected)
    {
        var c=new ObjectiveClause{type=type,count=3,itemId="目标",itemIds=new List<string>{"目标"},itemLevel=2,tag=ItemTag.Medical,tags=new List<ItemTag>{ItemTag.Medical,ItemTag.Pistol},rarity=Rarity.Rare,minChestQuality=ChestQuality.Rare};
        var s=new QuestRunSnapshot{kills=7,eliteKills=2,level=2,elapsed=12.9f,chestsOpenedByQuality=new[]{9,8,1,6,5},items=new List<ItemRecord>{
            new ItemRecord{id="目标",tag=ItemTag.Medical,rarity=Rarity.Rare,level=2},
            new ItemRecord{id="目标",tag=ItemTag.Pistol,rarity=Rarity.Common,level=1},
            new ItemRecord{id="其他",tag=ItemTag.Armor,rarity=Rarity.Legendary,level=3}}};
        Assert.AreEqual(expected,ObjectiveText.CurrentProgress(c,s));
    }
    [Test] public void ValueBoundsShowCurrentValueAndRemovalCanRegressProgress()
    {
        var s=new QuestRunSnapshot{backpackValue=5200};
        var c=new ObjectiveClause{type=ObjectiveType.BackpackValueAtLeast,minValue=8000};
        Assert.AreEqual($"当前 {5200:N0} / {8000:N0}",ObjectiveText.CurrentProgress(c,s));
        s.backpackValue=2100;Assert.AreEqual($"当前 {2100:N0} / {8000:N0}",ObjectiveText.CurrentProgress(c,s));
        c.type=ObjectiveType.BackpackValueAtMost;c.maxValue=8000;
        Assert.AreEqual($"当前 {2100:N0} / 上限 {8000:N0}",ObjectiveText.CurrentProgress(c,s));
    }
}
