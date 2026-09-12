using System;
using System.IO;
using System.Linq;
using BS.Core;
using BS.Data;
using BS.GamePlay;
using BS.GamePlay.Combat;
using BS.GamePlay.Enemies;
using BS.GamePlay.Loot;
using BS.GamePlay.Player;
using BS.GamePlay.Run;
using BS.GamePlay.Save;
using BS.Inventory;
using BS.Presentation;
using BS.Quest;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackSurvivor.EditorTools
{
    // Controlled Play Mode integration audit. No gameplay assets/configs are saved.
    public static class QuestS3PlayAudit
    {
        static GameSession session;
        static InventorySystem inventory;
        static InventoryUIController ui;
        static Health player;
        static Item probe;
        static RunResult observedResult;
        static int observedKills, observedElite;
        static bool dragged;
        static float oldMaximumDelta;
        static string Evidence => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Evidence/S3"));

        [MenuItem("Tools/Backpack Survivor/Quest/S3 Begin Full Duration Play Audit")]
        public static void Begin()
        {
            QuestS3Audit.Require(EditorApplication.isPlaying, "Play Mode required");
            QuestS3Audit.Require(SaveService.Instance == null, "Use direct run scene without persistent SaveService");
            session = Object.FindAnyObjectByType<GameSession>();
            inventory = Object.FindAnyObjectByType<InventorySystem>();
            ui = Object.FindAnyObjectByType<InventoryUIController>();
            player = Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>();
            session.StartRun();
            observedResult = null;
            session.OnRunEnded += ObserveResult;
            observedKills = observedElite = 0;
            EnemyAI.OnEnemyDied += ObserveKill;
            try
            {
                string[] prefabs = { "NormalEnemy", "EliteEnemy", "RangedEnemyAI" };
                int beforeKills = session.KillCount;
                foreach (string name in prefabs)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BackpackSurvivor/Prefabs/Enemy/" + name + ".prefab");
                    var enemy = Object.Instantiate(prefab, new Vector3(1000, 1, 1000), Quaternion.identity);
                    enemy.GetComponent<IPoolable>().OnGetFromPool();
                    var health = enemy.GetComponent<Health>();
                    health.TakeDamage(new DamageInfo(100000, player.gameObject, health.Position, false, 0));
                    health.TakeDamage(new DamageInfo(100000, player.gameObject, health.Position, false, 0));
                    Object.Destroy(enemy);
                }
                QuestS3Audit.Require(session.KillCount - beforeKills == 3 && session.EliteKillCount == 1,
                    "real health deaths classify normal/elite/ranged once");
                QuestS3Audit.Record("PASS real Health death path: normal+elite+ranged=3, elite=1; repeated damage does not double count.");

                var chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RecoveryChestBuilder.PrefabPath);
                var chest = Object.Instantiate(chestPrefab, new Vector3(1000, 0, 1000), Quaternion.identity).GetComponent<LootChest>();
                string[] qualities = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
                for (int i = 0; i < qualities.Length; i++)
                {
                    chest.OnGetFromPool();
                    var bundle = AssetDatabase.LoadAssetAtPath<LootTableData>(
                        "Assets/BackpackSurvivor/Data/ChestDropLoot/" + qualities[i] + "ChestLoot.asset");
                    chest.Initialize("audit", Color.white, bundle);
                    QuestS3Audit.Require(chest.Quality == (ChestQuality)i && chest.Interact() && !chest.Interact(), "exact chest quality/double interact");
                    chest.OnReturnPool();
                }
                Object.Destroy(chest.gameObject);
                QuestS3Audit.Require(LootChest.RunOpenedCount == 5 && LootChest.CopyRunOpenedByQuality().All(v => v == 1), "five exact quality buckets");
                QuestS3Audit.Record("PASS chest reuse + duplicate interaction: opened=5, exact buckets=[1,1,1,1,1].");

                var entry = JsonUtility.FromJson<LootTableData.LootEntry>(JsonUtility.ToJson(
                    AssetDatabase.LoadAssetAtPath<LootTableData>("Assets/BackpackSurvivor/Data/EquipDrops/LegendaryBonusDrops.asset").entries[0]));
                entry.questOnly = true; // test instance only; real pool filtering remains S6.
                var manager = Object.FindAnyObjectByType<LootManager>();
                manager.SpawnEntry(entry, player.Position).GetComponent<DropItem>().Collect();
                probe = inventory.Grid.GetUniqueItems().First(i => i.Id == entry.id);
                QuestS3Audit.Require(probe.QuestOnly, "pickup flag");
                inventory.Grid.Remove(probe);
                inventory.DiscardToWorld(probe);
                var discarded = Object.FindObjectsByType<DropItem>(FindObjectsSortMode.None).First(d => d.GetPrompt().Contains(entry.id));
                discarded.Collect();
                probe = inventory.Grid.GetUniqueItems().First(i => i.Id == entry.id);
                QuestS3Audit.Require(probe.QuestOnly, "discard/pickup roundtrip flag");
                QuestS3Audit.Record("PASS questOnly real pickup-discard-pickup roundtrip.");

                player.SetMaxHpAndReset(100000000); // controlled survival, not a balance test
                oldMaximumDelta = Time.maximumDeltaTime;
                Time.maximumDeltaTime = 1f;
                Time.timeScale = 30f;
                dragged = false;
                EditorApplication.update += Tick;
                QuestS3Audit.Record("BEGIN full 900s configured timer at 30x, controlled player health; combat/spawners remain active.");
            }
            catch (Exception e) { Finish(); QuestS3Audit.Record("FAIL " + e); throw; }
        }

        static void ObserveKill(EnemyKind kind) { observedKills++; if (kind == EnemyKind.Elite) observedElite++; }
        static void ObserveResult(RunResult result) { observedResult = result; }

        static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying || !session) { Finish(); return; }
                if (session.State == GameState.LevelUpSelecting) session.CompleteLevelUpChoice();
                if (session.State == GameState.Running)
                {
                    Time.timeScale = 30;
                    if (!dragged && session.Remaining < 10)
                    {
                        ui.HandleOpenBag();
                        var view = Object.FindObjectsByType<ItemView>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(v => v.Item == probe);
                        ui.BeginDrag(probe, view);
                        probe.Rotate();
                        QuestS3Audit.Require(!inventory.Grid.Contains(probe), "item detached just before timer end");
                        dragged = true;
                    }
                    return;
                }
                if (session.State != GameState.Victory) return;
                QuestS3Audit.Require(dragged, "final drag ran");
                VerifySnapshot(RunOutcome.Survived);
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Path.Combine(Evidence, "victory.json"), JsonUtility.ToJson(session.LastQuestSnapshot, true));
                ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, "victory.png"));
                QuestS3Audit.Record("PASS full-duration timer settlement with active drag; snapshot/result/grid agree.");
                Finish();
            }
            catch (Exception e) { Finish(); QuestS3Audit.Record("FAIL " + e); Debug.LogException(e); }
        }

        [MenuItem("Tools/Backpack Survivor/Quest/S3 Check Death And Reset")]
        public static void CheckDeath()
        {
            QuestS3Audit.Require(EditorApplication.isPlaying && session != null, "Run victory audit first");
            var old = session.LastQuestSnapshot;
            session.StartRun();
            observedResult = null;
            session.OnRunEnded += ObserveResult;
            QuestS3Audit.Require(session.LastQuestSnapshot == null && session.KillCount == 0 && session.EliteKillCount == 0 &&
                LootChest.RunOpenedCount == 0 && LootChest.CopyRunOpenedByQuality().All(x => x == 0), "cross-run reset");
            observedKills = observedElite = 0;
            var view = Object.FindObjectsByType<ItemView>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(v => v.Item == probe);
            ui.BeginDrag(probe, view);
            probe.Rotate();
            player.TakeDamage(new DamageInfo(float.MaxValue, player.gameObject, player.Position, false, 0));
            session.OnRunEnded -= ObserveResult;
            VerifySnapshot(RunOutcome.Died);
            var frozen = session.LastQuestSnapshot;
            var copy = session.LastQuestSnapshot;
            copy.items.Clear(); copy.chestsOpenedByQuality[0] = 999;
            QuestS3Audit.Require(session.LastQuestSnapshot.items.Count == frozen.items.Count &&
                session.LastQuestSnapshot.chestsOpenedByQuality[0] == 0 && old.outcome == RunOutcome.Survived, "frozen copies survive reset");
            File.WriteAllText(Path.Combine(Evidence, "death.json"), JsonUtility.ToJson(frozen, true));
            ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, "death.png"));
            QuestS3Audit.Record("PASS death while dragging, reset counters, detached snapshots across runs.");
        }

        static void VerifySnapshot(RunOutcome outcome)
        {
            var s = session.LastQuestSnapshot;
            QuestS3Audit.Require(s != null && s.outcome == outcome && inventory.Grid.Contains(probe), "outcome and drag restoration");
            QuestS3Audit.Require(s.backpackValue == inventory.Grid.GetTotalScoreValue() &&
                s.items.Sum(i => i.scoreValue) == s.backpackValue &&
                s.items.Count == inventory.Grid.GetUniqueItems().Count, "unique item/value equality");
            QuestS3Audit.Require(s.kills == observedKills && s.eliteKills == observedElite &&
                s.level == session.Level && s.gold == session.TotalGold &&
                s.chestsOpened == LootChest.RunOpenedCount, "independent counters equality");
            QuestS3Audit.Require(s.items.Any(i => i.id == probe.Id && i.questOnly), "questOnly snapshot");
            QuestS3Audit.Require(observedResult != null && observedResult.BackpackValue == s.backpackValue &&
                observedResult.KillCount == s.kills && observedResult.Level == s.level &&
                observedResult.Elapsed == s.elapsed && observedResult.TotalGold == s.gold &&
                observedResult.LegendaryFoundCount == s.items.Count(i => i.rarity == Rarity.Legendary) &&
                observedResult.LegendaryCollectedValue == s.items.Where(i => i.rarity == Rarity.Legendary).Sum(i => i.scoreValue),
                "existing RunResult agrees with frozen facts");
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            EnemyAI.OnEnemyDied -= ObserveKill;
            if (session) session.OnRunEnded -= ObserveResult;
            Time.maximumDeltaTime = oldMaximumDelta > 0 ? oldMaximumDelta : .3333333f;
            Time.timeScale = session && session.State == GameState.Running ? 1 : 0;
        }
    }
}
