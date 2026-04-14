using UnityEngine;
using MirrorChronicles.Data;

namespace MirrorChronicles.Core
{
    /// <summary>
    /// Core entry point of the game. Manages the high-level GameState.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            // Standard Singleton Pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // For Phase 1 testing, we jump straight into the Playing state.
            ChangeState(GameState.Playing);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"[GameManager] State changed to: {newState}");

            if (newState == GameState.Playing)
            {
                // Initialize the first year when entering Playing state
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.StartGameLoop();
                }
                else
                {
                    Debug.LogError("[GameManager] TimeManager is missing from the scene!");
                }
            }
        }
    }
}
