using System.Collections.Generic;

namespace BS.GamePlay.Upgrades
{
    public class LevelUpOptionGenerator
    {
        private readonly List<LevelUpOptionDefinition> definitions = new List<LevelUpOptionDefinition>();
        
        public LevelUpOptionGenerator()
        {

            definitions.Add(new LevelUpOptionDefinition(LevelUpOptionId.DamageUp,LevelUpOptionCategory.Attack,
                "火力强化","伤害 +20%",0.2f,100,1,-1));

            definitions.Add(new LevelUpOptionDefinition(LevelUpOptionId.MoveSpeedUp,LevelUpOptionCategory.Mobility,
                "轻装移动","移速 +10%",0.1f,80,1,-1));

            definitions.Add(new LevelUpOptionDefinition(LevelUpOptionId.FireRateUp,LevelUpOptionCategory.Attack,
                "快速射击","射速 +15%",0.15f,90,1,-1));
        }

        public List<LevelUpOption> Generate(int level, int count)
        {
            //拷贝，避免修改原始definitions
            List<LevelUpOptionDefinition> candidates = new List<LevelUpOptionDefinition>();
            foreach (var definition in definitions)//复制一份，避免改definitions
            {
                if (level < definition.MinLevel) continue;//过滤等级没达到要求的效果
                candidates.Add(definition);
            }
            //选择的升级效果
            List<LevelUpOption> result = new List<LevelUpOption>();

            while (result.Count < count && candidates.Count > 0)//count为3，随机到三个为止
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);//注意前闭后开
                LevelUpOptionDefinition select = candidates[index];

                result.Add(new LevelUpOption(select));
                candidates.Remove(select);
            }
            return result;
        }
    }
}