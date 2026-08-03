using System.Collections.Generic;

namespace BS.Inventory
{
    public static class AdjacencyRuleBook
    {
        public static IReadOnlyList<AdjacencyRule> Rules => rules;

        private static readonly List<AdjacencyRule> rules = new List<AdjacencyRule>
        {
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

            new AdjacencyRule(
            ItemTag.Rifle,
            ConnectableSides.Down,
            ItemTag.Magazine,
            ConnectableSides.Up,
            AdjacencyEffectId.FireRateBoost),

            new AdjacencyRule(
            ItemTag.Shotgun,
            ConnectableSides.Down,
            ItemTag.Magazine,
            ConnectableSides.Up,
            AdjacencyEffectId.FireRateBoost),

            new AdjacencyRule(
            ItemTag.Pistol,
            ConnectableSides.Left,
            ItemTag.Magazine,
            ConnectableSides.Up,
            AdjacencyEffectId.FireRateBoost)
        };
    }
}
