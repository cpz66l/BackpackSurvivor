using UnityEngine;

namespace BS.GamePlay.Combat
{
    public struct DamageInfo//伤害数据包
    {
        public readonly float damage ;//伤害值
        public readonly GameObject attacker;//攻击者
        public readonly Vector3 hitPoint;//击中世界坐标用于特效
        public readonly bool isCritical;//是否暴击
        public readonly float knockbackForce;//击退力

        public DamageInfo(float damage,
            GameObject attacker,
            Vector3 hitPoint,
            bool isCritical,
            float knockbackForce)
        {
            this.damage = damage;
            this.attacker = attacker;
            this.hitPoint = hitPoint;
            this.isCritical = isCritical;
            this.knockbackForce = knockbackForce;
        }
    }

    public enum Faction//阵营枚举
    {
        Player,
        Enemy
    }
    public interface IDamageable
    {
        ///<summary>
        ///接收伤害
        ///</summary>
        ///<param name="info">伤害数据包</param>
        void TakeDamage(DamageInfo info);

        ///<summary>
        ///查询位置
        ///</summary>
        Vector3 Position { get; }

        ///<summary>
        ///死亡状态
        ///</summary>
        bool IsDead { get; }

        ///<summary>
        ///阵营
        ///</summary>
        Faction Faction { get; }
    }
}
