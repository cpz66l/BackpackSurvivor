using System.Collections.Generic;

namespace BS.GamePlay.Upgrades
{
    public class LevelUpOptionGenerator
    {
        private readonly List<LevelUpOption> optionPool = new List<LevelUpOption>();

        public LevelUpOptionGenerator()
        {
            LevelUpOption option1 = new LevelUpOption(LevelUpOptionId.DamageUp,"火力强化","伤害 + 20 %",0.2f);
            optionPool.Add(option1);
            LevelUpOption option2 = new LevelUpOption(LevelUpOptionId.MoveSpeedUp,"轻装移动", "移速 +10%", 0.1f);
            optionPool.Add(option2);
            LevelUpOption option3 = new LevelUpOption(LevelUpOptionId.FireRateUp,"快速射击", "射速 +15%", 0.15f);
            optionPool.Add(option3);
        }

        public List<LevelUpOption> Generate(int level, int count)
        {
            List<LevelUpOption> result = new List<LevelUpOption>();
            List<LevelUpOption> candidates = new List<LevelUpOption>();
            foreach(var option in optionPool)//复制一份，避免改optionPool
            {
                candidates.Add(option);
            }
            while (result.Count < count && candidates.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);//注意前闭后开
                LevelUpOption option = candidates[index];
                result.Add(option);
                candidates.Remove(option);
            }
            return result;
        }
    }
}