using BS.GamePlay.Run;
using System;
using UnityEngine;
using System.IO;
using BS.Quest;
namespace BS.GamePlay.Save
{
    public class SaveService : MonoBehaviour
    {
        //持久化配置，先用常量保存文件名与路劲，防止拼写错误
        private const string SaveFileName = "save_data.json";
        private string SavePath
        {
            get
            {
#if UNITY_EDITOR
                string auditPath = UnityEditor.SessionState.GetString("BS.Quest.AuditSavePath", "");
                if (!string.IsNullOrEmpty(auditPath)) return auditPath;
#endif
                return Path.Combine(Application.persistentDataPath, SaveFileName);
            }
        }

        public static SaveService Instance { get; private set; } //实现单例
        public SaveData CurrentData { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);//加载场景时不能销
            LoadOrCreate();
        }
        public void LoadOrCreate()
        {
            
            try
            {
                if (!File.Exists(SavePath))
                {
                    CurrentData = SaveData.CreateDefault();
                    Save();
                    return;
                }

                string json = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                //把json字符串反序列化为C#对象，
                //而且这个C#对象的类在定义外要加[System.Serralizable],
                //否则jsonUtility无法序列化

                if (data == null)
                {
                    CurrentData = SaveData.CreateDefault();
                    Save();
                    return;
                }

                CurrentData = data;
                if (CurrentData.campaign == null) CurrentData.campaign = new CampaignSave();
                CurrentData.lastPlayedVersion = "v0.3.10";
                Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"读取存档失败，已创建默认存档：{e.Message}");
                CurrentData = SaveData.CreateDefault();
                Save();
            }
        }

        public void Save()
        {
            try
            {
                if (CurrentData == null)
                    CurrentData = SaveData.CreateDefault();

                //将C#对象打成Json字符
                string json = JsonUtility.ToJson(CurrentData, true);
                //再把josn字符写入存档Json文件中
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"保存存档失败：{e.Message}");
            }
        }

        public void RecordRunStarted()//记录游戏开局
        {
            if (CurrentData == null)
                CurrentData = SaveData.CreateDefault();

            CurrentData.totalRuns++;
            Save();
        }

        public void ApplyVictoryResult(RunResult result)//胜利结算后才写入剩余数据
        {
            if (result == null) return;
            if (CurrentData == null)
                CurrentData = SaveData.CreateDefault();

            CurrentData.totalWins++;
            CurrentData.totalGold += result.TotalGold;
            CurrentData.bestBackpackValue = Math.Max(CurrentData.bestBackpackValue, result.BackpackValue);
            CurrentData.legendaryFoundCount += result.LegendaryFoundCount;
            CurrentData.legendaryCollectedValue += result.LegendaryCollectedValue;
            CurrentData.lastPlayedVersion = "v0.3.10";

            Save();
        }
        public bool TrySetPendingQuest(QuestInstance quest, out string error)
        {
            error = null;
            if (quest == null || string.IsNullOrWhiteSpace(quest.eventId) || quest.objectives == null || quest.objectives.Count == 0)
            { error = "合同数据不完整，无法接受。"; return false; }
            var next = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(CurrentData ?? SaveData.CreateDefault()));
            if (next.campaign == null) next.campaign = new CampaignSave();
            if (next.campaign.finalCompleted) { error = "当前战役已通关。"; return false; }
            next.campaign.pendingQuest = quest.Copy();
            next.campaign.drawCount++;
            next.campaign.lastTag = quest.tag;
            if (next.campaign.recentEventIds == null) next.campaign.recentEventIds = new System.Collections.Generic.List<string>();
            next.campaign.recentEventIds.Add(quest.eventId);
            while (next.campaign.recentEventIds.Count > 5) next.campaign.recentEventIds.RemoveAt(0);
            if (!CampaignFile.TryWrite(SavePath, JsonUtility.ToJson(next, true), out error)) return false;
            CurrentData = next;
            return true;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool TryCompleteQuest(QuestInstance quest, bool final, out string error)
        {
            error = null;
            if (quest == null || string.IsNullOrWhiteSpace(quest.eventId)) { error="合同无效。"; return false; }
            var next = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(CurrentData ?? SaveData.CreateDefault()));
            if (next.campaign == null) next.campaign = new CampaignSave();
            if (next.campaign.completedEventIds == null) next.campaign.completedEventIds = new System.Collections.Generic.List<string>();
            if (next.campaign.completedEventIds.Contains(quest.eventId)) return true;
            next.campaign.completedEventIds.Add(quest.eventId);
            next.campaign.pendingQuest = null;
            if (!final) next.campaign.tier = Math.Max(next.campaign.tier, quest.tier + 1);
            next.campaign.finalCompleted |= final;
            if (!CampaignFile.TryWrite(SavePath, JsonUtility.ToJson(next, true), out error)) return false;
            CurrentData = next;
            return true;
        }

        public void CompleteQuest(QuestInstance quest, bool final)
        {
            TryCompleteQuest(quest, final, out string error);
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning("合同结算写档失败：" + error);
        }

    }
}
