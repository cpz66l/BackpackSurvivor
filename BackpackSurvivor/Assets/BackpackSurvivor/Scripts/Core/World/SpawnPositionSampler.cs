using System;
using UnityEngine;

namespace BS.Core
{
    /// <summary>Bounded rejection sampling for the flat, circular gameplay map.</summary>
    public static class SpawnPositionSampler
    {
        // TagManager: Obstacle = 7. Ground, enemies, player, pickups and triggers do not block spawns.
        public const int ObstacleMask = 1 << 7;

        public static bool TryFindInRing(
            MapBounds map, Vector3 player, float minRadius, float maxRadius,
            float spawnY, Vector3 halfExtents, int maxAttempts, out Vector3 result)
        {
            minRadius = Mathf.Max(0f, minRadius);
            maxRadius = Mathf.Max(minRadius, maxRadius);
            return TryFind(map, player, minRadius, spawnY, halfExtents, maxAttempts, () =>
            {
                float radius = UnityEngine.Random.Range(minRadius, maxRadius);
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                return player + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }, out result);
        }

        public static bool TryFindInMap(
            MapBounds map, Vector3 player, float minDistance,
            float spawnY, Vector3 halfExtents, int maxAttempts, out Vector3 result)
        {
            result = Vector3.zero;
            if (map == null) return false;
            return TryFind(map, player, minDistance, spawnY, halfExtents, maxAttempts,
                map.GetRandomPoint, out result);
        }

        /// <summary>
        /// Sample at most maxAttempts candidates. Reject outside points instead of clamping them
        /// onto the map edge, which would concentrate spawns and shorten their player distance.
        /// </summary>
        public static bool TryFind(
            MapBounds map, Vector3 player, float minDistance, float spawnY,
            Vector3 halfExtents, int maxAttempts, Func<Vector3> nextCandidate, out Vector3 result)
        {
            result = Vector3.zero;
            if (map == null || nextCandidate == null || maxAttempts <= 0) return false;

            halfExtents = new Vector3(
                Mathf.Max(0.01f, halfExtents.x),
                Mathf.Max(0.01f, halfExtents.y),
                Mathf.Max(0.01f, halfExtents.z));
            float minDistanceSquared = Mathf.Max(0f, minDistance) * Mathf.Max(0f, minDistance);
            // Circumscribed radius keeps every corner of the occupancy box inside the map.
            float footprintRadius = new Vector2(halfExtents.x, halfExtents.z).magnitude;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 candidate = nextCandidate();
                candidate.y = spawnY;
                if (!map.IsInside(candidate, footprintRadius)) continue;
                Vector3 toPlayer = candidate - player;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude < minDistanceSquared) continue;
                if (Physics.CheckBox(candidate, halfExtents, Quaternion.identity,
                    ObstacleMask, QueryTriggerInteraction.Ignore)) continue;

                result = candidate;
                return true;
            }

            return false;
        }
    }
}
