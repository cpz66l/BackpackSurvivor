using System;
using System.IO;
using BS.Data;
using BS.Inventory;
using BS.Quest;
using BS.GamePlay.Enemies;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestS3Audit
    {
        [MenuItem("Tools/Backpack Survivor/Quest/S3 Configure Fact Identities")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode.");
            string[] names = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
            for (int i = 0; i < names.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LootTableData>(
                    "Assets/BackpackSurvivor/Data/ChestDropLoot/" + names[i] + "ChestLoot.asset");
                if (!asset) throw new InvalidOperationException("Missing chest bundle " + names[i]);
                asset.chestQuality = (ChestQuality)i;
                EditorUtility.SetDirty(asset);
            }
            var elite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BackpackSurvivor/Prefabs/Enemy/EliteEnemy.prefab");
            var serialized = new SerializedObject(elite.GetComponent<EnemyAI>());
            serialized.FindProperty("enemyKind").intValue = (int)EnemyKind.Elite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[Quest S3] identity configuration saved; weights untouched.");
        }

        [MenuItem("Tools/Backpack Survivor/Quest/S3 Check Pure Data")]
        public static void CheckPureData()
        {
            var grid = new InventoryGrid(2, 2);
            var item = new Item("probe", Rarity.Legendary, 2, 1, ItemTag.None, ConnectableSides.None, 100, 0, 2, true);
            Require(grid.Place(0, 0, item), "initial place");
            Require(grid.TryReserveDragOrigin(item), "reserve");
            grid.Remove(item);
            var other = new Item("other", Rarity.Common, 1, 1, ItemTag.None, ConnectableSides.None, 1, 0, 1);
            Require(!grid.Place(0, 0, other), "pickup cannot consume drag origin");
            Require(grid.Place(0, 1, other), "unreserved pickup remains possible");
            item.Rotate();
            while (item.RotationState != Rotation.None) item.Rotate();
            Require(grid.Place(0, 0, item), "rollback after rotation");
            grid.ReleaseDragOrigin(item);
            Require(grid.GetTotalScoreValue() == 201, "no item/value loss");
            Require(item.QuestOnly, "questOnly retained");
            var snapshot = new QuestRunSnapshot();
            snapshot.items.Add(new ItemRecord { id = item.Id, scoreValue = item.ScoreValue, questOnly = item.QuestOnly });
            snapshot.chestsOpenedByQuality[4] = 1;
            var copy = snapshot.Copy();
            copy.items.Clear(); copy.chestsOpenedByQuality[4] = 0;
            Require(snapshot.items.Count == 1 && snapshot.chestsOpenedByQuality[4] == 1, "snapshot copies detached");
            Record("PASS pure data: reserved rollback, rotation, concurrent pickup, questOnly, detached snapshot.");
        }

        public static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[Quest S3] FAIL " + message);
        }

        public static void Record(string message)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Evidence/S3"));
            Directory.CreateDirectory(path);
            File.AppendAllText(Path.Combine(path, "verification.txt"), DateTime.UtcNow.ToString("O") + " " + message + Environment.NewLine);
            Debug.Log("[Quest S3] " + message);
        }
    }
}
