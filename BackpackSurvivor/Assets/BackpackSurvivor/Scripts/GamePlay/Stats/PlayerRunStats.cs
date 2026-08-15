using BS.GamePlay.Upgrades;
using System;
using UnityEngine;

namespace BS.GamePlay.Stats
{
    public class PlayerRunStats : MonoBehaviour
    {
        public event Action OnStatsChanged;
        //攻击类
        private float damageMultiplier = 1f; //攻击伤害
        private float fireRateMultiplier = 1f; //攻击速度
        private float critChance = 0f; //暴击率
        private float critDamageMultiplier = 1.5f; //暴击伤害
        private float projectileSpeedMultiplier = 1f; //子弹速度
        private float autoWeaponRangeMultiplier = 1f; //武器攻击范围

        //生存类
        private float maxHpBonus = 0f; //最大生命值加成
        private float damageReduction = 0f;//免伤

        //机动类
        private float moveSpeedMultiplier = 1f;//移动速度

        //搜刮类
        private float pickupRangeMultiplier = 1f;//磁吸范围
        private float xpGainMultiplier = 1f; //经验成长加成
        private float goldGainMultiplier = 1f; //金币加成

        //构筑类
        private int activeWeaponLimitBonus = 0;//激活武器限制加成

        //攻击类
        public float DamageMultiplier => damageMultiplier;
        public float FireRateMultiplier => fireRateMultiplier;
        public float CritChance => critChance;
        public float CritDamageMultiplier => critDamageMultiplier;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public float AutoWeaponRangeMultiplier => autoWeaponRangeMultiplier;
        //生存类
        public float MaxHpBonus => maxHpBonus; //最大生命值加成
        public float DamageReduction => damageReduction;//免伤
        //机动类
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        //搜刮类
        public float PickupRangeMultiplier => pickupRangeMultiplier;
        public float GoldGainMultiplier => goldGainMultiplier;
        public float XpGainMultiplier => xpGainMultiplier;
        //构筑类
        public int ActiveWeaponLimitBonus => activeWeaponLimitBonus;

        public void Apply(LevelUpOption option)
        {
            if (option == null) return;

            switch (option.Id)
            {
                case LevelUpOptionId.DamageUp:
                    damageMultiplier += option.Value;
                    break;

                case LevelUpOptionId.FireRateUp:
                    fireRateMultiplier += option.Value;
                    break;

                case LevelUpOptionId.MoveSpeedUp:
                    moveSpeedMultiplier += option.Value;
                    break;

                case LevelUpOptionId.CritChanceUp:
                    critChance = Mathf.Clamp01(critChance + option.Value);
                    break;

                case LevelUpOptionId.CritDamageUp:
                    critDamageMultiplier += option.Value;
                    break;

                case LevelUpOptionId.ProjectileSpeedUp:
                    projectileSpeedMultiplier += option.Value;
                    break;

                case LevelUpOptionId.WeaponRangeUp:
                    autoWeaponRangeMultiplier += option.Value;
                    break;

                case LevelUpOptionId.MaxHpUp:
                    maxHpBonus += option.Value;
                    break;

                case LevelUpOptionId.DamageReductionUp:
                    damageReduction = Mathf.Clamp(damageReduction + option.Value, 0f, 0.75f);
                    break;

                case LevelUpOptionId.PickupRangeUp:
                    pickupRangeMultiplier += option.Value;
                    break;

                case LevelUpOptionId.XpGainUp:
                    xpGainMultiplier += option.Value;
                    break;

                case LevelUpOptionId.GoldGainUp:
                    goldGainMultiplier += option.Value;
                    break;

                case LevelUpOptionId.ActiveWeaponLimitUp:
                    activeWeaponLimitBonus += Mathf.RoundToInt(option.Value);
                    break;
            }
            OnStatsChanged?.Invoke();
        }

        public void ResetToDefault()
        {
            damageMultiplier = 1f;
            fireRateMultiplier = 1f;
            moveSpeedMultiplier = 1f;
            critChance = 0f;
            critDamageMultiplier = 1.5f;
            projectileSpeedMultiplier = 1f;
            autoWeaponRangeMultiplier = 1f;
            maxHpBonus = 0f;
            damageReduction = 0f;
            pickupRangeMultiplier = 1f;
            xpGainMultiplier = 1f;
            goldGainMultiplier = 1f;
            activeWeaponLimitBonus = 0;
            OnStatsChanged?.Invoke();
        }
    }
}
