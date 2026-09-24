using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Core;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Manages VFX spawning for combat and breakthrough events.
    /// Uses ObjectPool for performance. Assigns colors by element.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Prefabs (assign in Inspector)")]
        public GameObject hitVFXPrefab;
        public GameObject qiTrailPrefab;
        public GameObject breakthroughVFXPrefab;
        public GameObject qiDeviationVFXPrefab;
        public GameObject shockwavePrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (ObjectPool.Instance != null)
            {
                if (hitVFXPrefab != null) ObjectPool.Instance.RegisterPrefab("hit_vfx", hitVFXPrefab, 5);
                if (qiTrailPrefab != null) ObjectPool.Instance.RegisterPrefab("qi_trail", qiTrailPrefab, 3);
                if (breakthroughVFXPrefab != null) ObjectPool.Instance.RegisterPrefab("breakthrough_vfx", breakthroughVFXPrefab, 1);
                if (qiDeviationVFXPrefab != null) ObjectPool.Instance.RegisterPrefab("qi_deviation", qiDeviationVFXPrefab, 1);
                if (shockwavePrefab != null) ObjectPool.Instance.RegisterPrefab("shockwave", shockwavePrefab, 2);
            }
        }

        public static Color GetElementColor(Element element)
        {
            return element switch
            {
                Element.Fire => new Color(1f, 0.3f, 0.1f),
                Element.Water => new Color(0.2f, 0.5f, 1f),
                Element.Wood => new Color(0.3f, 0.8f, 0.2f),
                Element.Metal => new Color(0.8f, 0.8f, 0.9f),
                Element.Earth => new Color(0.7f, 0.5f, 0.2f),
                Element.Lightning => new Color(0.9f, 0.9f, 0.3f),
                Element.Darkness => new Color(0.3f, 0.1f, 0.4f),
                Element.Light => new Color(1f, 1f, 0.8f),
                _ => Color.white
            };
        }

        public void SpawnHitVFX(Vector3 position, Element element)
        {
            var obj = SpawnFromPool("hit_vfx", position);
            if (obj != null) TintParticles(obj, GetElementColor(element));
        }

        public void SpawnQiTrail(Vector3 from, Vector3 to, Element element)
        {
            var obj = SpawnFromPool("qi_trail", from);
            if (obj != null)
            {
                TintParticles(obj, GetElementColor(element));
                // Trail moves to target over time
                var mover = obj.GetComponent<VFXMover>();
                if (mover != null) mover.SetTarget(to, 0.5f);
            }
        }

        public void SpawnBreakthroughVFX(Vector3 position, bool success)
        {
            string key = success ? "breakthrough_vfx" : "qi_deviation";
            var obj = SpawnFromPool(key, position);
            if (obj != null && !success)
                TintParticles(obj, new Color(0.5f, 0f, 0f));
        }

        public void SpawnShockwave(Vector3 position)
        {
            SpawnFromPool("shockwave", position);
        }

        private GameObject SpawnFromPool(string key, Vector3 position)
        {
            if (ObjectPool.Instance == null) return null;
            var obj = ObjectPool.Instance.Get(key, position, Quaternion.identity);
            if (obj != null)
                ObjectPool.Instance.Return(key, obj, 2f);
            return obj;
        }

        private void TintParticles(GameObject obj, Color color)
        {
            var ps = obj.GetComponent<ParticleSystem>();
            if (ps == null) return;

            var main = ps.main;
            main.startColor = color;
        }
    }

    /// <summary>
    /// Simple mover for VFX objects (Qi trails).
    /// </summary>
    public class VFXMover : MonoBehaviour
    {
        private Vector3 _target;
        private float _duration;
        private float _elapsed;
        private Vector3 _start;
        private bool _moving;

        public void SetTarget(Vector3 target, float duration)
        {
            _start = transform.position;
            _target = target;
            _duration = duration;
            _elapsed = 0f;
            _moving = true;
        }

        private void Update()
        {
            if (!_moving) return;
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            transform.position = Vector3.Lerp(_start, _target, t);
            if (t >= 1f) _moving = false;
        }
    }
}
