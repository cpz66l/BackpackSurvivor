using System;
using System.IO;
using UnityEngine;

namespace BS.Core.LLM
{
    public enum LlmKeySource
    {
        None,
        Environment,
        File
    }

    public readonly struct ResolvedLlmConfig
    {
        public readonly LlmModelConfig Settings;
        public readonly string ApiKey;
        public readonly LlmKeySource KeySource;

        public ResolvedLlmConfig(LlmModelConfig settings, string apiKey, LlmKeySource keySource)
        {
            Settings = settings;
            ApiKey = apiKey ?? string.Empty;
            KeySource = keySource;
        }
    }

    public static class LlmConfigService
    {
        public const string ApiKeyEnvironmentName = "DEEPSEEK_API_KEY";
        public const string ConfigFileName = "llm_model_config.json";

        public static string ConfigPath => Path.Combine(Application.persistentDataPath, ConfigFileName);

        public static LlmModelConfig LoadFile()
        {
            LlmModelConfig config = LlmModelConfig.CreateDefault();
            try
            {
                if (!File.Exists(ConfigPath)) return config;
                string json = File.ReadAllText(ConfigPath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    LlmModelConfig parsed = JsonUtility.FromJson<LlmModelConfig>(json);
                    if (parsed != null) config = parsed;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[LLM Config] 配置文件读取失败，已使用默认值：" + exception.Message);
            }

            config.Normalize();
            return config;
        }

        public static bool SaveFile(LlmModelConfig config, out string error)
        {
            error = string.Empty;
            if (config == null)
            {
                error = "配置为空。";
                return false;
            }

            config.Normalize();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(ConfigPath, JsonUtility.ToJson(config, true));
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Debug.LogError("[LLM Config] 配置文件保存失败：" + error);
                return false;
            }
        }

        public static ResolvedLlmConfig Resolve()
        {
            LlmModelConfig fileConfig = LoadFile();
            string environmentKey = ReadEnvironmentKey();
            if (!string.IsNullOrWhiteSpace(environmentKey))
                return new ResolvedLlmConfig(fileConfig, environmentKey, LlmKeySource.Environment);
            if (!string.IsNullOrWhiteSpace(fileConfig.apiKey))
                return new ResolvedLlmConfig(fileConfig, fileConfig.apiKey, LlmKeySource.File);
            return new ResolvedLlmConfig(fileConfig, string.Empty, LlmKeySource.None);
        }

        public static string ReadEnvironmentKey()
        {
            string value = Environment.GetEnvironmentVariable(ApiKeyEnvironmentName);
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                value = Environment.GetEnvironmentVariable(ApiKeyEnvironmentName, EnvironmentVariableTarget.User);
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            catch (PlatformNotSupportedException) { }
            catch (System.Security.SecurityException) { }
#endif
            return string.Empty;
        }

        public static string GetKeySourceLabel(LlmKeySource source)
        {
            switch (source)
            {
                case LlmKeySource.Environment: return "环境变量优先";
                case LlmKeySource.File: return "本地配置文件";
                default: return "未配置";
            }
        }

        public static string MaskKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "未配置";
            string value = key.Trim();
            if (value.Length <= 4) return "****";
            return "****" + value.Substring(value.Length - 4);
        }
    }
}
