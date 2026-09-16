using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BS.Core.LLM;
using BS.GamePlay.Npc;
using BS.GamePlay.Quest;
using BS.Quest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class SettlementExperienceAudit
    {
        public static bool Running { get; private set; }
        public static string LastResult { get; private set; }

        [MenuItem("Tools/Backpack Survivor/LLM/Verify Settlement Experience")]
        public static void Run() => RunLabel("latest");

        public static async void RunLabel(string label)
        {
            if (Running) throw new InvalidOperationException("Settlement audit is running");
            Running = true; LastResult = "running";
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Evidence/NpcExperience/S20/Settlement"));
            Directory.CreateDirectory(folder);
            var rows = new JArray();
            try
            {
                var settings = LlmConfigService.Resolve();
                if (!settings.Settings.npcEnabled || string.IsNullOrWhiteSpace(settings.ApiKey))
                    throw new InvalidOperationException("Online audit needs enabled NPC and configured key; configuration unchanged");
                var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestCatalogBuilder.DatabasePath);
                var q = QuestDrawer.Accept(db.events.Single(e => e.eventId == "prototype-t5-1").ToCandidate(), 216, db.AllQuestOnlyIds);
                var names = new[] { "victory_complete", "victory_partial", "death_conditions_met", "death_partial" };
                for (int i = 0; i < names.Length; i++)
                {
                    var s = new QuestRunSnapshot { outcome = i < 2 ? RunOutcome.Survived : RunOutcome.Died,
                        elapsed = i < 2 ? 900 : 780, level = 51, kills = 1996, eliteKills = i % 2 == 0 ? 556 : 180,
                        backpackValue = 105750,
                        items = new List<ItemRecord> { new ItemRecord { id = db.AllQuestOnlyIds[0], level = 3, scoreValue = 54000, questOnly = true } } };
                    var service = new NpcDialogueService();
                    service.CompletedEvents = () => 0;
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90)))
                    {
                        string reply = await service.RequestDebriefAsync(q, s, timeout.Token);
                        rows.Add(new JObject {
                            ["case"] = names[i], ["outcome"] = s.outcome.ToString(),
                            ["completed"] = QuestEvaluator.Evaluate(q, s).Completed,
                            ["text"] = reply, ["failure"] = service.LastFailure,
                            ["fallback"] = service.UsedFallback, ["tools"] = service.ToolCount,
                            ["audit"] = service.Audit
                        });
                        File.WriteAllText(Path.Combine(folder, label + ".json"), rows.ToString(Formatting.Indented));
                        LastResult = "completed " + rows.Count + "/4";
                    }
                }
                LastResult = "Completed 4 live samples; online=" + rows.Count(x => !(bool)x["fallback"] && !string.IsNullOrWhiteSpace((string)x["text"])) + "/4; inspect all outputs";
            }
            catch (Exception e)
            {
                LastResult = "FAIL " + e.GetType().Name;
                rows.Add(new JObject { ["errorType"] = e.GetType().Name });
                File.WriteAllText(Path.Combine(folder, label + ".json"), rows.ToString(Formatting.Indented));
            }
            finally { Running = false; }
        }
    }
}
