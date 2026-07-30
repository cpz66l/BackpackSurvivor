using BS.GamePlay.Combat;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace BS.Presentation
{
    public class CameraShakePlayer : MonoBehaviour
    {
        [SerializeField] private CinemachineBasicMultiChannelPerlin noise;
        [SerializeField] private float defaultAmplitude;
        [SerializeField] private float defaultFrequency;

        private Coroutine shakeRoutine;

        private void Awake()
        {
            if (noise == null)
                noise = GetComponent<CinemachineBasicMultiChannelPerlin>();

            ResetNoise();
        }
        public void Shake(float duration, float amplitude, float frequency)
        {
            if (noise == null)
                return;
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine(duration, amplitude, frequency));
        }
        private IEnumerator ShakeRoutine(float duration, float amplitude, float frequency)
        {
            if (noise == null)
                yield break;

            duration = Mathf.Max(0.01f, duration);

            float timer = 0f;

            noise.AmplitudeGain = amplitude;
            noise.FrequencyGain = frequency;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;

                float ratio = Mathf.Clamp01(timer / duration);
                float currentAmplitude = Mathf.Lerp(amplitude, 0f, ratio);

                noise.AmplitudeGain = currentAmplitude;
                noise.FrequencyGain = frequency;

                yield return null;
            }

            ResetNoise();
            shakeRoutine = null;
        }
        private void ResetNoise()
        {
            if (noise == null)
                return;
            noise.AmplitudeGain = 0;
            noise.FrequencyGain = 0;
        }
        private void OnDisable()
        {
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);
            ResetNoise();
        }
    }
}
