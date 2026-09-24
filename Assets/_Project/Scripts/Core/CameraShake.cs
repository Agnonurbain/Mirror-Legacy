using System.Collections;
using UnityEngine;

namespace MirrorChronicles.Core
{
    /// <summary>
    /// Simple camera shake for impact effects (breakthrough, combat hits).
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 _originalPosition;
        private bool _shaking;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Shake(float duration = 0.3f, float magnitude = 0.2f)
        {
            if (_shaking) return;
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            _shaking = true;
            _originalPosition = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                transform.localPosition = _originalPosition + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                magnitude *= 0.95f;
                yield return null;
            }

            transform.localPosition = _originalPosition;
            _shaking = false;
        }
    }
}
