using BS.GamePlay.Run;
using System;
using UnityEngine;
using System.IO;
namespace BS.GamePlay.Save
{
    public class SaveService : MonoBehaviour
    {
        //持久化配置，先用常量保存文件名与路劲，防止拼写错误
        private const string SaveFileName = "save_data.json";
        private string SavePath => Path.Combine(UnityEngine.Application.persistentDataPath, SaveFileName);

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
    }
}
