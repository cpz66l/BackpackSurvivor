using BS.GamePlay.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace BS.GamePlay.Combat
{
    public static class TargetRegistry
    {
        private static readonly List<IDamageable> allTargets = new List<IDamageable>();
        public static int Count => allTargets.Count;

        public static IDamageable GetNearestTarget(Vector3 fromPos, float maxRange, Faction targetFaction)
        {
            float nearestDistance = float.MaxValue;
            IDamageable nearestTarget = null;
            for (int i = allTargets.Count - 1; i >= 0; i--)
            {
                if (allTargets[i] == null || allTargets[i].IsDead || allTargets[i].Faction != targetFaction)
                {
                    Unregister(allTargets[i]);
                    continue;
                }
                float sqrDistance = (allTargets[i].Position - fromPos).sqrMagnitude;
                bool isOutOfRange = sqrDistance > maxRange * maxRange;
                if (isOutOfRange) continue;
                if (nearestDistance > sqrDistance)
                {
                    nearestDistance = sqrDistance;
                    nearestTarget = allTargets[i];
                }
            }
            return nearestTarget;
        }


        public static void Register(IDamageable t)
        {
            if (t == null || allTargets.Contains(t)) return;
            allTargets.Add(t);
            Debug.Log($"注册");
        }
        public static void Unregister(IDamageable t)
        {
            if (!allTargets.Contains(t)) return;
            allTargets.Remove(t);
            Debug.Log("注销");
        }

    }
}

