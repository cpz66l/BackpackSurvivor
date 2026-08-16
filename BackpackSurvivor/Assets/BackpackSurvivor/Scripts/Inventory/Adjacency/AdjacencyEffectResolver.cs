using System.Collections.Generic;

namespace BS.Inventory
{
    //有效效果解析器:用于从候选效果抽出真实效果
    public static class AdjacencyEffectResolver
    {
        public static List<AdjacencyEffect> ResolveValidEffects(List<AdjacencyEffect> candidateEffects)
        {
            List<AdjacencyEffect> validEffects = new List<AdjacencyEffect>();

            if (candidateEffects == null) return validEffects;

            AddValidDualWieldEffects(candidateEffects,validEffects);//处理候选双持效果
            AddValidStackableEffects(candidateEffects, validEffects);//处理可堆叠数值效果
            return validEffects;
        }

        //用于单独对DualWieldEffect（双持效果）进行处理
        private static void AddValidDualWieldEffects(List<AdjacencyEffect> candidateEffects, List<AdjacencyEffect> validEffects)
        {
            HashSet<Item> usedItems = new HashSet<Item>();

            foreach (AdjacencyEffect effect in candidateEffects)
            {
                if (effect == null) continue;
                if (effect.EffectId != AdjacencyEffectId.DualWield) continue;
                //筛掉已经参与效果的item
                if (usedItems.Contains(effect.ItemA)) continue;
                if (usedItems.Contains(effect.ItemB)) continue;

                validEffects.Add(effect);
                usedItems.Add(effect.ItemA);
                usedItems.Add(effect.ItemB);
            }
        }

        private static void AddValidStackableEffects(List<AdjacencyEffect> candidateEffects, List<AdjacencyEffect> validEffects)
        {
            foreach (AdjacencyEffect effect in candidateEffects)
            {
                if (effect == null) continue;
                if (!IsStackableEffect(effect.EffectId)) continue;//筛选可堆叠效果

                validEffects.Add(effect);
            }
        }

        private static bool IsStackableEffect(AdjacencyEffectId effectId)
        {
            return effectId == AdjacencyEffectId.FireRateBoost
                || effectId == AdjacencyEffectId.DamageBoost
                || effectId == AdjacencyEffectId.CritBoost;
        }
    }
}
