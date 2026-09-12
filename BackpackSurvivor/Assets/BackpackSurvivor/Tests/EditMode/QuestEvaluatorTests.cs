using NUnit.Framework;
using System.Collections.Generic;
using BS.Inventory;
using BS.Quest;

public class QuestEvaluatorTests
{
    static QuestRunSnapshot Snapshot(RunOutcome outcome = RunOutcome.Survived)
    {
        return new QuestRunSnapshot { outcome = outcome, level = 3, kills = 10, eliteKills = 2, elapsed = 120, backpackValue = 500,
            items = new List<ItemRecord> { new ItemRecord { id = "核心", tag = ItemTag.Rifle, rarity = Rarity.Epic, level = 3, scoreValue = 300 }, new ItemRecord { id = "药", tag = ItemTag.Medical, rarity = Rarity.Common, level = 1, scoreValue = 200 } },
            chestsOpenedByQuality = new[] { 0, 1, 2, 0, 0 } };
    }
    static QuestInstance Q(ObjectiveClause c) => new QuestInstance { objectives = new List<ObjectiveClause> { c } };

    [Test] public void EveryObjectiveTypeBoundaries()
    {
        var s = Snapshot();
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryTag, tag=ItemTag.Rifle, count=1 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryTagSet, tags=new List<ItemTag>{ItemTag.Rifle,ItemTag.Pistol}, count=1 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryRarity, rarity=Rarity.Rare, count=1 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryItem, itemId="核心" }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryItemAtLevel, itemId="核心", itemLevel=3 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryAnyItemAtLevel, itemLevel=3 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryItemAnyOf, itemIds=new List<string>{"核心","x"} }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.BackpackValueAtLeast, minValue=500 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.BackpackValueAtMost, maxValue=500 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.ExcludeTag, tag=ItemTag.Pistol }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.ExcludeRarity, rarity=Rarity.Legendary }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.KillTotal, count=10 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.KillElite, count=2 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.OpenChestAtLeast, minChestQuality=ChestQuality.Rare, count=2 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.ReachLevel, count=3 }), s).Completed);
        Assert.IsTrue(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.SurviveToSecond, count=120 }), s).Completed);
    }
    [Test] public void DeathNeverCompletesButRecordsPriorConditions()
    {
        var result = QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.KillElite, count=2 }), Snapshot(RunOutcome.Died));
        Assert.IsFalse(result.Completed); Assert.IsTrue(result.ConditionsSatisfiedBeforeDeath); Assert.AreEqual(1f, result.Progress01);
    }
    [Test] public void SettlementMatrixRequiresVictoryAndCompletedObjectives()
    {
        var q=Q(new ObjectiveClause { type=ObjectiveType.KillTotal, count=10 });
        var victory=QuestEvaluator.Evaluate(q, Snapshot(RunOutcome.Survived));
        var victoryIncomplete=QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.KillTotal, count=11 }), Snapshot(RunOutcome.Survived));
        var deathCompleteConditions=QuestEvaluator.Evaluate(q, Snapshot(RunOutcome.Died));
        var deathIncomplete=QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.KillTotal, count=11 }), Snapshot(RunOutcome.Died));
        Assert.IsTrue(victory.Completed, "victory + objectives complete");
        Assert.IsFalse(victoryIncomplete.Completed, "victory + objectives incomplete");
        Assert.IsFalse(deathCompleteConditions.Completed, "death invalidates otherwise complete conditions");
        Assert.IsFalse(deathIncomplete.Completed, "death + objectives incomplete");
    }
    [Test] public void AndOptionalUnknownAndEmptyBoundaries()
    {
        var s=Snapshot(); var q=new QuestInstance { objectives=new List<ObjectiveClause>{new ObjectiveClause{type=ObjectiveType.KillTotal,count=11},new ObjectiveClause{type=ObjectiveType.KillElite,count=99,optional=true}}};
        var r=QuestEvaluator.Evaluate(q,s); Assert.IsFalse(r.Completed); Assert.AreEqual(0f,r.Progress01);
        Assert.IsFalse(QuestEvaluator.Evaluate(new QuestInstance(),s).Completed);
        Assert.IsFalse(QuestEvaluator.Evaluate(new QuestInstance{objectives=new List<ObjectiveClause>{new ObjectiveClause{type=(ObjectiveType)999}}},s).Completed);
    }
    [Test] public void InvalidRequiredFieldsNeverPass()
    {
        var s=Snapshot();
        Assert.IsFalse(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.KillTotal, count=0 }),s).Completed);
        Assert.IsFalse(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryItem, itemId="", count=1 }),s).Completed);
        Assert.IsFalse(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryTagSet, count=1, tags=new List<ItemTag>() }),s).Completed);
        Assert.IsFalse(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.OpenChestAtLeast, minChestQuality=ChestQuality.Unknown, count=1 }),s).Completed);
        Assert.IsFalse(QuestEvaluator.Evaluate(Q(new ObjectiveClause { type=ObjectiveType.CarryAnyItemAtLevel, itemLevel=0, count=1 }),s).Completed);
    }
    [Test] public void EveryObjectiveTypeHasARealFailureBoundary()
    {
        var s=Snapshot();
        var failures=new List<ObjectiveClause>{
            new ObjectiveClause{type=ObjectiveType.CarryTag,tag=ItemTag.SniperRifle,count=1},
            new ObjectiveClause{type=ObjectiveType.CarryTagSet,tags=new List<ItemTag>{ItemTag.Pistol},count=1},
            new ObjectiveClause{type=ObjectiveType.CarryRarity,rarity=Rarity.Legendary,count=1},
            new ObjectiveClause{type=ObjectiveType.CarryItem,itemId="不存在",count=1},
            new ObjectiveClause{type=ObjectiveType.CarryItemAtLevel,itemId="核心",itemLevel=4,count=1},
            new ObjectiveClause{type=ObjectiveType.CarryAnyItemAtLevel,itemLevel=4,count=1},
            new ObjectiveClause{type=ObjectiveType.CarryItemAnyOf,itemIds=new List<string>{"不存在"},count=1},
            new ObjectiveClause{type=ObjectiveType.BackpackValueAtLeast,minValue=501},
            new ObjectiveClause{type=ObjectiveType.BackpackValueAtMost,maxValue=499},
            new ObjectiveClause{type=ObjectiveType.ExcludeTag,tag=ItemTag.Medical},
            new ObjectiveClause{type=ObjectiveType.ExcludeRarity,rarity=Rarity.Common},
            new ObjectiveClause{type=ObjectiveType.KillTotal,count=11},
            new ObjectiveClause{type=ObjectiveType.KillElite,count=3},
            new ObjectiveClause{type=ObjectiveType.OpenChestAtLeast,minChestQuality=ChestQuality.Rare,count=3},
            new ObjectiveClause{type=ObjectiveType.ReachLevel,count=4},
            new ObjectiveClause{type=ObjectiveType.SurviveToSecond,count=121}
        };
        foreach(var clause in failures) Assert.IsFalse(QuestEvaluator.Evaluate(Q(clause),s).Completed,clause.type.ToString());
        Assert.IsFalse(QuestEvaluator.Evaluate(Q(failures[0]),null).Completed);
    }
    [Test] public void DrawerFiltersTierCompletedAndUnlocksDeterministically()
    {
        var candidates = new List<QuestCandidate> {
            new QuestCandidate { eventId="a", tier=1, tag=1, baseWeight=1 },
            new QuestCandidate { eventId="done", tier=1, baseWeight=99 },
            new QuestCandidate { eventId="locked", tier=1, unlockAfter=new[]{"missing"} }
        };
        var picked = QuestDrawer.Pick(candidates, 1, new HashSet<string>{"done"}, new HashSet<int>(), 7);
        Assert.AreEqual("a", picked.eventId);
        Assert.IsNull(QuestDrawer.Pick(candidates, 2, new HashSet<string>(), new HashSet<int>(), 7));
        var instance = QuestDrawer.Accept(picked, 42, new List<string>{"quest-item"});
        Assert.AreEqual(42, instance.seed); Assert.AreEqual("quest-item", instance.activeQuestOnlyItemIds[0]);
    }
}
