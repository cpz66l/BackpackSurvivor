using System;

namespace BS.Core.LLM
{
    [Serializable]
    public sealed class LlmModelConfig
    {
        public string apiKey = string.Empty;
        public int maxSessionTurns = 20;
        public int maxTotalTokens = 40000;
        public int maxResponseCharacters = 200;
        public int maxPulseCharacters = 60;

        public static LlmModelConfig CreateDefault()
        {
            return new LlmModelConfig();
        }

        public void Normalize()
        {
            apiKey = apiKey == null ? string.Empty : apiKey.Trim();
            maxSessionTurns = Clamp(maxSessionTurns, 1, 1000, 20);
            maxTotalTokens = Clamp(maxTotalTokens, 1000, 1000000, 40000);
            maxResponseCharacters = Clamp(maxResponseCharacters, 1, 10000, 200);
            maxPulseCharacters = Clamp(maxPulseCharacters, 1, 5000, 60);
        }

        private static int Clamp(int value, int min, int max, int fallback)
        {
            if (value <= 0) return fallback;
            return value < min ? min : value > max ? max : value;
        }
    }
}
