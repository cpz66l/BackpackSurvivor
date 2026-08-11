using UnityEngine;

namespace BS.Presentation
{
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shootClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip pickupXpClip;
        [SerializeField] private AudioClip levelUpClip;
        [SerializeField] private AudioClip hurtClip;
        [SerializeField] private AudioClip chestOpenClip;


        private void Awake()
        {
            if (audioSource == null) 
                audioSource = GetComponent<AudioSource>();
        }
        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.PlayOneShot(clip);
        }
        public void PlayHit()
        {
            PlayOneShot(hitClip);
        }

        public void PlayShoot()
        {
            PlayOneShot(shootClip);
        }
        public void PlayPickupXp()
        {
            PlayOneShot(pickupXpClip);
        }
        public void PlayLevelUp()
        {
            PlayOneShot(levelUpClip);
        }
        public void PlayHurt()
        {
            PlayOneShot(hurtClip);
        }
        public void PlayChestOpen()
        {
            PlayOneShot(chestOpenClip);
        }
    }
}
