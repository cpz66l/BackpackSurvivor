using BS.GamePlay.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace BS.Presentation.EnvironmentEffects
{
    /// <summary>Small, camera-local weather volume. World-space particles never move with the player.</summary>
    [DisallowMultipleComponent]
    public sealed class NightAtmosphere : MonoBehaviour
    {
        public const int DustHardLimit = 120;
        public const int DrizzleHardLimit = 56;

        [Header("References")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private ParticleSystem windDust;
        [SerializeField] private ParticleSystem fineDrizzle;
        [SerializeField] private Material sharedParticleMaterial;

        [Header("Local weather volume (metres)")]
        [SerializeField, Min(8f)] private float volumeWidth = 26f;
        [SerializeField] private float groundHeight;
        [SerializeField, Range(2f, 6f)] private float volumeHeight = 4.8f;
        [SerializeField] private Vector2 windVelocity = new Vector2(1.15f, 0.36f);

        [Header("Hard-capped particle budget")]
        [SerializeField, Range(0, DustHardLimit)] private int dustLimit = DustHardLimit;
        [SerializeField, Range(0, DrizzleHardLimit)] private int drizzleLimit = DrizzleHardLimit;
        [SerializeField, Range(0f, 12f)] private float dustPerSecond = 10f;
        [SerializeField, Range(0f, 6f)] private float drizzlePerSecond = 6f;
        [SerializeField] private Color dustColor = new Color(0.60f, 0.72f, 0.82f, 0.28f);
        [SerializeField] private Color drizzleColor = new Color(0.56f, 0.70f, 0.83f, 0.24f);

        public int ParticleHardLimit => DustHardLimit + DrizzleHardLimit;
        public int LiveParticleCount => (windDust ? windDust.particleCount : 0) + (fineDrizzle ? fineDrizzle.particleCount : 0);
        public float EmissionPerSecond => dustPerSecond + drizzlePerSecond;

        // Called by the editor builder; no runtime material/texture copies are created.
        public void Configure(Transform target, ParticleSystem dust, ParticleSystem drizzle, Material material)
        {
            followTarget = target;
            windDust = dust;
            fineDrizzle = drizzle;
            sharedParticleMaterial = material;
            ApplyParticleSettings();
            FollowTarget();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            ResolveTarget();
            ApplyParticleSettings();
            FollowTarget();
            if (windDust) windDust.Play(false);
            if (fineDrizzle) fineDrizzle.Play(false);
        }

        private void OnDisable()
        {
            if (windDust) windDust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (fineDrizzle) fineDrizzle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnValidate()
        {
            dustLimit = Mathf.Clamp(dustLimit, 0, DustHardLimit);
            drizzleLimit = Mathf.Clamp(drizzleLimit, 0, DrizzleHardLimit);
            dustPerSecond = Mathf.Clamp(dustPerSecond, 0f, 12f);
            drizzlePerSecond = Mathf.Clamp(drizzlePerSecond, 0f, 6f);
            volumeWidth = Mathf.Clamp(volumeWidth, 8f, 36f);
            volumeHeight = Mathf.Clamp(volumeHeight, 2f, 6f);
            // Particle settings are reapplied on enable. Avoid changing duration on a playing system.
        }

        private void LateUpdate()
        {
            // Weather freezes alongside enemies while choosing an upgrade or pausing the run.
            if (Time.timeScale <= 0f) return;
            FollowTarget();
        }

        private void ResolveTarget()
        {
            if (followTarget) return;
            var player = FindAnyObjectByType<PlayerController>();
            if (player) followTarget = player.transform;
            else if (Camera.main) followTarget = Camera.main.transform;
        }

        private void FollowTarget()
        {
            if (!followTarget) return;
            Vector3 target = followTarget.position;
            // Player root is below the visible body in the legacy scene; use the floor datum.
            transform.position = new Vector3(target.x, groundHeight + 3.2f, target.z);
            transform.rotation = Quaternion.identity;
        }

        private void ApplyParticleSettings()
        {
            OnValidate();
            SetUpSystem(windDust, false, dustLimit, dustPerSecond, dustColor);
            SetUpSystem(fineDrizzle, true, drizzleLimit, drizzlePerSecond, drizzleColor);
        }

        private void SetUpSystem(ParticleSystem system, bool drizzle, int limit, float emissionRate, Color tint)
        {
            if (!system) return;
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.useAutoRandomSeed = false;
            system.randomSeed = drizzle ? 4817u : 8129u;

            var main = system.main;
            main.duration = 10f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.maxParticles = Mathf.Max(1, limit);
            main.startLifetime = drizzle ? new ParticleSystem.MinMaxCurve(2.2f, 3.2f) : new ParticleSystem.MinMaxCurve(7f, 10f);
            main.startSpeed = 0f;
            main.startSize = drizzle ? new ParticleSystem.MinMaxCurve(0.035f, 0.060f) : new ParticleSystem.MinMaxCurve(0.085f, 0.18f);
            main.startColor = tint;
            main.gravityModifier = 0f;

            var emission = system.emission;
            emission.enabled = limit > 0 && emissionRate > 0f;
            emission.rateOverTime = emissionRate;
            emission.rateOverDistance = 0f;
            emission.SetBursts(new ParticleSystem.Burst[0]);

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(volumeWidth, volumeHeight, volumeWidth);

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(windVelocity.x * 0.75f, windVelocity.x * 1.2f);
            velocity.z = new ParticleSystem.MinMaxCurve(windVelocity.y * 0.75f, windVelocity.y * 1.2f);
            velocity.y = drizzle ? new ParticleSystem.MinMaxCurve(-2.15f, -1.65f) : new ParticleSystem.MinMaxCurve(-0.09f, 0.14f);

            var color = system.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.16f), new GradientAlphaKey(0.8f, 0.72f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            var collision = system.collision; collision.enabled = false;
            var trigger = system.trigger; trigger.enabled = false;
            var noise = system.noise; noise.enabled = false;
            var trails = system.trails; trails.enabled = false;
            var lights = system.lights; lights.enabled = false;
            var subEmitters = system.subEmitters; subEmitters.enabled = false;

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = sharedParticleMaterial;
            renderer.renderMode = drizzle ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.lengthScale = drizzle ? 3f : 1f;
            renderer.velocityScale = drizzle ? 0.045f : 0f;
            renderer.cameraVelocityScale = 0f;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 0.018f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.sortMode = ParticleSystemSortMode.None;
            renderer.allowOcclusionWhenDynamic = false;
        }
    }
}
