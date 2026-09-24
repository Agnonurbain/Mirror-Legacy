using System.Collections.Generic;
using UnityEngine;

namespace MirrorChronicles.Core
{
    /// <summary>
    /// Generic object pool for reusing GameObjects (VFX, projectiles, UI elements).
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        private readonly Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterPrefab(string key, GameObject prefab, int prewarmCount = 0)
        {
            if (_prefabs.ContainsKey(key)) return;

            _prefabs[key] = prefab;
            _pools[key] = new Queue<GameObject>();

            for (int i = 0; i < prewarmCount; i++)
            {
                var obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                _pools[key].Enqueue(obj);
            }
        }

        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogWarning($"[ObjectPool] Pool '{key}' not registered.");
                return null;
            }

            GameObject obj;
            if (_pools[key].Count > 0)
            {
                obj = _pools[key].Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(_prefabs[key], position, rotation);
            }

            return obj;
        }

        public void Return(string key, GameObject obj)
        {
            if (!_pools.ContainsKey(key))
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            _pools[key].Enqueue(obj);
        }

        public void Return(string key, GameObject obj, float delay)
        {
            StartCoroutine(ReturnAfterDelay(key, obj, delay));
        }

        private System.Collections.IEnumerator ReturnAfterDelay(string key, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(key, obj);
        }
    }
}
