using BS.GamePlay.Combat;
using UnityEngine;
namespace BS.Presentation
{
    public class PlayerHitFeedbackView : MonoBehaviour
    {
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private Health health;
        [SerializeField] private CameraShakePlayer cameraShakePlayer;
        [SerializeField] private float duration;
        [SerializeField] private float amplitude;
        [SerializeField] private float frequency;

        private void Awake()
        {
            if(sfx == null)
                sfx = FindAnyObjectByType<SfxPlayer>();
            if(health == null)
                health = GetComponent<Health>();
            if(cameraShakePlayer == null)
                cameraShakePlayer = FindAnyObjectByType<CameraShakePlayer>();
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.OnDamaged += HandleHitFeedBack;
        }
        private void OnDisable()
        {
            if (health != null)
                health.OnDamaged -= HandleHitFeedBack;
        }

        private void HandleHitFeedBack(DamageInfo info)
        {
            sfx?.PlayHurt();

            if (cameraShakePlayer != null)
                cameraShakePlayer.Shake(duration, amplitude, frequency);
        }
    }
}
