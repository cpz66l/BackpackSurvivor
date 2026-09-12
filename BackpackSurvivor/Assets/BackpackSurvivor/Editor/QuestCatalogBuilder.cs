using System.Collections.Generic;
using System.Linq;
using BS.Data;
using BS.GamePlay.Quest;
using BS.Inventory;
using BS.Quest;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestCatalogBuilder
    {
        public const string DatabasePath = "Assets/BackpackSurvivor/Data/Quest/QuestDatabase.asset";
        public static QuestDatabase Build()
        {
            const string folder = "Assets/BackpackSurvivor/Data/Quest";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/BackpackSurvivor/Data", "Quest");
            var database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(DatabasePath);
            if (database != null)
            {
                if(database.itemCatalog==null || database.itemCatalog.Count==0){SetItemCatalog(database);EditorUtility.SetDirty(database);AssetDatabase.SaveAssets();}
                return database; // Later content edits belong to the local assets.
            }
            database = ScriptableObject.CreateInstance<QuestDatabase>();
            SetItemCatalog(database);
            database.questOnlyPool = AssetDatabase.LoadAssetAtPath<LootTableData>("Assets/BackpackSurvivor/Data/EquipDrops/LegendaryBonusDrops.asset");
            var weaponTags = new List<ItemTag> { ItemTag.Pistol, ItemTag.Rifle, ItemTag.Shotgun };
            var sets = new[] {
                new[] { new ObjectiveClause { type=ObjectiveType.CarryTag, tag=ItemTag.Medical } },
                new[] { new ObjectiveClause { type=ObjectiveType.KillTotal, count=30 } },
                new[] { new ObjectiveClause { type=ObjectiveType.ReachLevel, count=3 } },
                new[] { new ObjectiveClause { type=ObjectiveType.CarryTagSet, tags=weaponTags, count=2 }, Value(8000) },
                new[] { new ObjectiveClause { type=ObjectiveType.CarryRarity, rarity=Rarity.Rare, count=2 }, Value(6000) },
                new[] { new ObjectiveClause { type=ObjectiveType.OpenChestAtLeast, minChestQuality=ChestQuality.Uncommon, count=2 }, new ObjectiveClause {type=ObjectiveType.KillTotal,count=60} },
                new[] { new ObjectiveClause { type=ObjectiveType.CarryItem, itemId="污染区研究样本" }, new ObjectiveClause {type=ObjectiveType.ExcludeTag,tag=ItemTag.Medical} },
                new[] { new ObjectiveClause { type=ObjectiveType.CarryRarity, rarity=Rarity.Epic }, Value(10000) },
                new[] { new ObjectiveClause { type=ObjectiveType.CarryAnyItemAtLevel,itemLevel=2,count=2 },new ObjectiveClause {type=ObjectiveType.KillTotal,count=150} },
                new[] { new ObjectiveClause { type=ObjectiveType.CarryAnyItemAtLevel,itemLevel=3 },new ObjectiveClause {type=ObjectiveType.KillElite,count=3} },
                new[] { new ObjectiveClause {type=ObjectiveType.CarryTagSet,tags=weaponTags,count=3},new ObjectiveClause {type=ObjectiveType.KillElite,count=5} },
                new[] { new ObjectiveClause {type=ObjectiveType.OpenChestAtLeast,minChestQuality=ChestQuality.Epic,count=2},Value(16000) },
                new[] { FinalItem(database), Value(30000) },
                new[] { FinalItem(database), Value(30000),new ObjectiveClause {type=ObjectiveType.KillElite,count=3} },
                new[] { FinalItem(database), Value(30000),new ObjectiveClause {type=ObjectiveType.CarryAnyItemAtLevel,itemLevel=3} }
            };
            string[] titles = { "基础回收", "清理路线", "适应战区", "军械整备", "品质回收", "箱源勘察", "样本封存", "精良物资", "整备升级", "无痕作业", "火力集结", "高阶开箱", "方舟密钥", "终局清理", "终局整备" };
            for (int i = 0; i < sets.Length; i++)
            {
                int tier = i / 3 + 1;
                var definition = ScriptableObject.CreateInstance<QuestEventDefinition>();
                definition.eventId = "prototype-t" + tier + "-" + (i % 3 + 1);
                definition.definitionVersion = "prototype-1";
                definition.tier = tier; definition.tag = i % 3; definition.isFinal = tier == 5;
                definition.codename = titles[i]; definition.objectives = new List<ObjectiveClause>(sets[i]);
                definition.offlineBriefing = "这次行动的目标已列在合同上。请核对装备，完成要求后存活带出。";
                definition.briefingSeed = "调度员，简洁务实，回收行动";
                definition.unlockAfter = new string[0];
                AssetDatabase.CreateAsset(definition, folder + "/" + definition.eventId + ".asset");
                database.events.Add(definition);
            }
            AssetDatabase.CreateAsset(database, DatabasePath);
            AssetDatabase.SaveAssets();
            return database;
        }
        static void SetItemCatalog(QuestDatabase database)
        {
            database.itemCatalog=AssetDatabase.FindAssets("t:LootTableData",new[]{"Assets/BackpackSurvivor/Data/EquipDrops"})
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).Select(AssetDatabase.LoadAssetAtPath<LootTableData>).ToList();
        }
        static ObjectiveClause Value(int value) => new ObjectiveClause { type=ObjectiveType.BackpackValueAtLeast,minValue=value };
        static ObjectiveClause FinalItem(QuestDatabase database) => new ObjectiveClause { type=ObjectiveType.CarryItemAnyOf,itemIds=database.AllQuestOnlyIds };
    }
}
