using System.Collections.Generic;


namespace BS.GamePlay.Upgrades
{
    public class LevelUpOptionGenerator
    {
        private readonly List<LevelUpOptionDefinition> definitions = new List<LevelUpOptionDefinition>();
        private readonly Dictionary<LevelUpOptionId, int> pickCounts = new Dictionary<LevelUpOptionId, int>();//记录升级选项被选择了多少次

        public LevelUpOptionGenerator()
        {
            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.DamageUp,
                LevelUpOptionCategory.Attack,
                "火力强化",
                "伤害 +15%",
                0.15f,
                100,
                1,
                -1));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.FireRateUp,
                LevelUpOptionCategory.Attack,
                "快速射击",
                "射速 +15%",
                0.15f,
                90,
                1,
                -1));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.MoveSpeedUp,
                LevelUpOptionCategory.Mobility,
                "轻装移动",
                "移速 +10%",
                0.1f,
                75,
                3,
                8));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.MaxHpUp,
                LevelUpOptionCategory.Survival,
                "应急装甲",
                "最大生命值 +25",
                25f,
                70,
                5,
                8));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.PickupRangeUp,
                LevelUpOptionCategory.Loot,
                "磁吸背包",
                "拾取范围 +25%",
                0.25f,
                65,
                6,
                6));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.CritChanceUp,
                LevelUpOptionCategory.Attack,
                "精准校准",
                "暴击率 +10%",
                0.1f,
                55,
                2,
                10));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.ProjectileSpeedUp,
                LevelUpOptionCategory.Attack,
                "高速弹体",
                "子弹速度 +15%",
                0.15f,
                45,
                6,
                5));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.WeaponRangeUp,
                LevelUpOptionCategory.Attack,
                "扩展索敌",
                "武器射程 +15%",
                0.15f,
                45,
                6,
                5));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.CritDamageUp,
                LevelUpOptionCategory.Attack,
                "弱点打击",
                "暴击伤害 +25%",
                0.25f,
                40,
                5,
                6));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.DamageReductionUp,
                LevelUpOptionCategory.Survival,
                "战术护甲",
                "受到伤害 -10%",
                0.1f,
                35,
                6,
                5));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.XpGainUp,
                LevelUpOptionCategory.Loot,
                "战斗学习",
                "经验获取 +20%",
                0.2f,
                35,
                3,
                5));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.GoldGainUp,
                LevelUpOptionCategory.Loot,
                "淘金直觉",
                "金币获取 +20%",
                0.20f,
                30,
                8,
                5));

            definitions.Add(new LevelUpOptionDefinition(
                LevelUpOptionId.ActiveWeaponLimitUp,
                LevelUpOptionCategory.Build,
                "扩展武装槽",
                "激活武器上限 +1",
                1f,
                15,
                8,
                1));
        }

        public List<LevelUpOption> Generate(int level, int count)
        {
            //拷贝，避免修改原始definitions
            List<LevelUpOptionDefinition> candidates = new List<LevelUpOptionDefinition>();
            foreach (var definition in definitions)//复制一份可选的选项，避免改definitions
            {
                if(IsSelectable(definition,level))
                    candidates.Add(definition);
            }
            //选择的升级效果
            List<LevelUpOption> result = new List<LevelUpOption>();

            while (result.Count < count && candidates.Count > 0)//count为3，随机到三个为止
            {
                //按权重抽取技能
                LevelUpOptionDefinition select = RollWeighted(candidates);

                result.Add(new LevelUpOption(select));
                candidates.Remove(select);
            }
            return result;
        }


        private bool IsSelectable(LevelUpOptionDefinition definition, int level)
        {
            //判断等级是否达到要求
            if(definition.MinLevel > level) return false;
            //判断被选择是否超过上限，-1的无限选择。
            if (definition.MaxPickCount != -1)
            {
                if (pickCounts.ContainsKey(definition.Id))
                    if (pickCounts[definition.Id] >= definition.MaxPickCount) 
                        return false;
                //如果等级符合但还没被选择过，则可以加入备选
            }
            return true;
        }

        private LevelUpOptionDefinition RollWeighted(List<LevelUpOptionDefinition> candidates)
        {
            LevelUpOptionDefinition result = null;
            int total = 0;
            foreach(LevelUpOptionDefinition definition in candidates)
            {
                if(definition.Weight == 0) continue;
                total += definition.Weight;
            }

            if (total <= 0) return candidates[0];

            int randomNum = UnityEngine.Random.Range(0, total);
            foreach(LevelUpOptionDefinition definition in candidates)
            {
                total -= definition.Weight;
                if(randomNum >= total)
                {
                    result = definition;
                    break;
                }
            }

            return result;
        }

        //公开方法，玩家选择后调用
        public void RecordPick(LevelUpOption option)
        {
            if(option == null) return;
            if (pickCounts.ContainsKey(option.Id))
                pickCounts[option.Id]++;
            else
                pickCounts[option.Id] = 1;
        }

        public void ResetRuntimeState()
        {
            pickCounts.Clear();
        }
    }
}