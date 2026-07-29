using BS.GamePlay.Upgrades;
using UnityEngine;

namespace BS.GamePlay.Stats
{
    public class PlayerRunStats : MonoBehaviour
    {
        private float damageMultiplier = 1f;
        private float fireRateMultiplier = 1f;
        private float moveSpeedMultiplier = 1f;

        public float DamageMultiplier => damageMultiplier;
        public float FireRateMultiplier => fireRateMultiplier;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;

        public void Apply(LevelUpOption option)
        {
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
            }
        }

        public void ResetToDefault()
        {
            damageMultiplier = 1f;
            fireRateMultiplier = 1f;
            moveSpeedMultiplier = 1f;
        }
    }
}
