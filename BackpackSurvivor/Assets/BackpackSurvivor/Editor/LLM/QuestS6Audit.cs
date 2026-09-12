using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BS.Data;
using BS.GamePlay.Loot;
using BS.Inventory;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestS6Audit
    {
        const string LeafPath = "Assets/BackpackSurvivor/Data/EquipDrops/LegendaryBonusDrops.asset";
        static readonly string[] QuestIds = { "方舟计划核心", "星火反应炉" };
        const int SampleCount = 10000;
        const int Seed = 20260912;
        static readonly List<string> evidence = new List<string>();
        static readonly List<LootTableData> temporary = new List<LootTableData>();

        [MenuItem("Tools/Backpack Survivor/Quest/S6 Configure QuestOnly Assets")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var table = AssetDatabase.LoadAssetAtPath<LootTableData>(LeafPath);
            Require(table != null && table.entries.Length == 5, "legendary table shape");
            Require(table.entries.Select(e => e.weight).SequenceEqual(new[] { 35, 25, 20, 10, 10 }), "existing weights must be unchanged");
            Require(QuestIds.All(id => table.entries.Any(e => e.id == id)), "both quest ids exist");
            foreach (var e in table.entries) e.questOnly = QuestIds.Contains(e.id);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log("[Quest S6] two questOnly flags configured; five weights unchanged.");
        }

        [MenuItem("Tools/Backpack Survivor/Quest/S6 Check QuestOnly Filter")]
        public static void Check()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run isolated audit outside Play Mode.");
            evidence.Clear();
            var randomState = Random.state;
            try
            {
                var leaf = AssetDatabase.LoadAssetAtPath<LootTableData>(LeafPath);
                Require(leaf.entries.Where(e => e.questOnly).Select(e => e.id).OrderBy(x => x).SequenceEqual(QuestIds.OrderBy(x => x)), "actual asset questOnly set");
                Require(leaf.entries.Select(e => e.weight).SequenceEqual(new[] { 35, 25, 20, 10, 10 }), "actual asset weights retained");
                var normalCounts = SampleLeaf(leaf, LootContext.Normal);
                var contractCounts = SampleLeaf(leaf, new LootContext(true, QuestIds));
                Require(normalCounts.Where(k => QuestIds.Contains(k.Key)).Sum(k => k.Value) == 0, "normal leaf excludes questOnly");
                Require(contractCounts.Where(k => QuestIds.Contains(k.Key)).Sum(k => k.Value) > 0, "contract leaf includes questOnly");
                foreach (var entry in leaf.entries.Where(e => !e.questOnly))
                {
                    double expected = entry.weight / 65.0;
                    Require(Math.Abs(normalCounts[entry.id] / (double)SampleCount - expected) < .025, "relative normal weight distribution " + entry.id);
                }
                Note("LEAF normal " + Counts(normalCounts));
                Note("LEAF contract " + Counts(contractCounts));

                foreach (string quality in new[] { "Epic", "Legendary" })
                {
                    var bundle = AssetDatabase.LoadAssetAtPath<LootTableData>("Assets/BackpackSurvivor/Data/ChestDropLoot/" + quality + "ChestLoot.asset");
                    CompareBundle(bundle, CloneBundle(bundle, false), LootContext.Normal, quality + " normal");
                    CompareBundle(bundle, CloneBundle(bundle, true), new LootContext(true, QuestIds), quality + " contract");
                }
                CheckPityAndMissingContext();
                Note("PASS all assertions; Random.state restored; no scene, save or gameplay state changed.");
                WriteEvidence();
            }
            catch (Exception e)
            {
                Note("FAIL " + e);
                WriteEvidence();
                throw;
            }
            finally
            {
                Random.state = randomState;
                foreach (var asset in temporary) if (asset) Object.DestroyImmediate(asset);
                temporary.Clear();
            }
        }

        static Dictionary<string,int> SampleLeaf(LootTableData table, LootContext context)
        {
            var counts = table.entries.ToDictionary(e => e.id, e => 0);
            var roller = new LootRoller(10);
            Random.InitState(Seed);
            for (int i = 0; i < SampleCount; i++) counts[roller.Roll(table, context).id]++;
            return counts;
        }

        // A reference asset tree with only allowed entries, preserving all weights and channel probabilities.
        static LootTableData CloneBundle(LootTableData source, bool includeQuest)
        {
            var clone = ScriptableObject.CreateInstance<LootTableData>(); temporary.Add(clone);
            if (source.entries != null)
                clone.entries = source.entries.Where(e => e != null && (includeQuest || !e.questOnly)).Select(e =>
                {
                    var copy = JsonUtility.FromJson<LootTableData.LootEntry>(JsonUtility.ToJson(e));
                    copy.questOnly = false;
                    return copy;
                }).ToArray();
            if (source.channels != null)
                clone.channels = source.channels.Select(c => new LootTableData.DropChannel
                { probability = c.probability, subTable = CloneBundle(c.subTable, includeQuest) }).ToArray();
            return clone;
        }

        static void CompareBundle(LootTableData actual, LootTableData reference, LootContext context, string label)
        {
            var expected = new string[SampleCount];
            var baseline = new LootRoller(10);
            Random.InitState(Seed);
            for (int i = 0; i < SampleCount; i++) expected[i] = string.Join("|", baseline.RollBundle(reference).Select(e => e.id));
            var roller = new LootRoller(10);
            Random.InitState(Seed);
            int questDrops = 0, normalDrops = 0;
            for (int i = 0; i < SampleCount; i++)
            {
                var entries = roller.RollBundle(actual, context);
                Require(string.Join("|", entries.Select(e => e.id)) == expected[i], label + " reference sequence at " + i);
                questDrops += entries.Count(e => e.questOnly);
                normalDrops += entries.Count(e => !e.questOnly && e.rarity == Rarity.Legendary);
            }
            Require(context.AllowQuestOnly ? questDrops > 0 : questDrops == 0, label + " quest count");
            Note(label + ": boxes=" + SampleCount + ", questDrops=" + questDrops + ", regularLegendary=" + normalDrops + ", exact reference matches=" + SampleCount);
        }

        static void CheckPityAndMissingContext()
        {
            var common = ScriptableObject.CreateInstance<LootTableData>(); temporary.Add(common);
            common.entries = new[] { new LootTableData.LootEntry { id = "common", weight = 1, rarity = Rarity.Common } };
            var rare = ScriptableObject.CreateInstance<LootTableData>(); temporary.Add(rare);
            rare.entries = new[] {
                new LootTableData.LootEntry { id = "ordinary-rare", rarity = Rarity.Rare, weight = 1 },
                new LootTableData.LootEntry { id = "quest-rare", rarity = Rarity.Rare, weight = 99, questOnly = true }
            };
            int allowedHits = 0;
            for (int i = 0; i < 1000; i++)
            {
                var roller = new LootRoller(1);
                roller.Roll(common); // Arms the real pity branch (ordinary equipment miss).
                Require(roller.Roll(rare, LootContext.Normal).id == "ordinary-rare", "pity cannot leak");
                roller.Roll(common);
                if (roller.Roll(rare, new LootContext(true, new[] { "quest-rare" })).questOnly) allowedHits++;
            }
            Require(allowedHits > 0, "pity contract candidate available");
            var ids = new List<string> { "quest-rare" };
            var captured = new LootContext(true, ids); ids.Clear();
            Require(captured.Allows("quest-rare", true), "caller cannot mutate captured context");
            rare.entries = new[] { rare.entries[1] };
            foreach (int threshold in new[] { 0, 10 })
            {
                var roller = new LootRoller(threshold);
                Require(roller.Roll(rare, null) == null, "null context denies");
                Require(roller.Roll(rare, new LootContext(true)) == null, "missing list denies");
                Require(roller.Roll(rare, new LootContext(true, Array.Empty<string>())) == null, "empty list denies");
                Require(roller.Roll(rare, new LootContext(true, new[] { "different" })) == null, "unknown id denies");
                Require(roller.Roll(rare, captured)?.id == "quest-rare", "captured allowed id remains usable");
            }
            Note("PITY: normal quest hits=0/1000; contract quest hits=" + allowedHits + "/1000; null/empty/restricted/all-filtered/immutable context checks PASS on both paths.");
        }

        static string Counts(Dictionary<string,int> counts) => string.Join(", ", counts.Select(k => k.Key + "=" + k.Value));
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        static void Note(string message) { evidence.Add(message); Debug.Log("[Quest S6] " + message); }
        static void WriteEvidence()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Evidence/S6"));
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "verification.txt"), new[] { DateTime.UtcNow.ToString("O"), "Seed=" + Seed }.Concat(evidence));
        }
    }
}
