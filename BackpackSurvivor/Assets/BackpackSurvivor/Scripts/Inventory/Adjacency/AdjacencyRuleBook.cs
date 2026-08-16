using System.Collections.Generic;

namespace BS.Inventory
{
    public static class AdjacencyRuleBook
    {
        public static IReadOnlyList<AdjacencyRule> Rules => rules;

        private static readonly List<AdjacencyRule> rules = new List<AdjacencyRule>
        {
            //双持手枪效果
            new AdjacencyRule(
            ItemTag.Pistol,
            ConnectableSides.Right,
            ItemTag.Pistol,
            ConnectableSides.Left,
            AdjacencyEffectId.DualWield),
            new AdjacencyRule(
            ItemTag.Pistol,
            ConnectableSides.Left,
            ItemTag.Pistol,
            ConnectableSides.Right,
            AdjacencyEffectId.DualWield),
            //步枪的弹匣邻接规则，增加射速
            new AdjacencyRule(
            ItemTag.Rifle,
            ConnectableSides.Down,
            ItemTag.Magazine,
            ConnectableSides.Up,
            AdjacencyEffectId.FireRateBoost),
            //霰弹枪的弹匣邻接规则，增加射速
            new AdjacencyRule(
            ItemTag.Shotgun,
            ConnectableSides.Down,
            ItemTag.Magazine,
            ConnectableSides.Up,
            AdjacencyEffectId.FireRateBoost),
            //手枪的弹匣邻接规则，增加射速
            new AdjacencyRule(
            ItemTag.Pistol,
            ConnectableSides.Left,
            ItemTag.Magazine,
            ConnectableSides.Up,
            AdjacencyEffectId.FireRateBoost),
            //手枪攻击芯片的邻接规则，增加伤害
            new AdjacencyRule(
            ItemTag.Pistol,
            ConnectableSides.Left,
            ItemTag.AttackDamageChip,
            ConnectableSides.Down,
            AdjacencyEffectId.DamageBoost),
            new AdjacencyRule(
            ItemTag.Pistol,
            ConnectableSides.Right,
            ItemTag.AttackDamageChip,
            ConnectableSides.Down,
            AdjacencyEffectId.DamageBoost),
            //步枪攻击芯片的邻接规则，增加伤害
            new AdjacencyRule(
            ItemTag.Rifle,
            ConnectableSides.Up,
            ItemTag.AttackDamageChip,
            ConnectableSides.Down,
            AdjacencyEffectId.DamageBoost),
            //霰弹枪攻击芯片的邻接规则，增加伤害
            new AdjacencyRule(
            ItemTag.Shotgun,
            ConnectableSides.Up,
            ItemTag.AttackDamageChip,
            ConnectableSides.Down,
            AdjacencyEffectId.DamageBoost),
            // 瞄准镜邻接规则：手枪上边接瞄准镜下边，增加暴击率
            new AdjacencyRule(
            ItemTag.Pistol,
            ConnectableSides.Up,
            ItemTag.Scope,
            ConnectableSides.Down,
            AdjacencyEffectId.CritBoost),
            // 瞄准镜邻接规则：步枪上边接瞄准镜下边，增加暴击率
            new AdjacencyRule(
            ItemTag.Rifle,
            ConnectableSides.Up,
            ItemTag.Scope,
            ConnectableSides.Down,
            AdjacencyEffectId.CritBoost),
            // 瞄准镜邻接规则：霰弹枪上边接瞄准镜下边，增加暴击率
            new AdjacencyRule(
            ItemTag.Shotgun,
            ConnectableSides.Up,
            ItemTag.Scope,
            ConnectableSides.Down,
            AdjacencyEffectId.CritBoost),
        };
    }
}
