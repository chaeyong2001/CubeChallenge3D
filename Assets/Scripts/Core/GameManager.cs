using UnityEngine;

namespace CubeChallenge3D.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public AppState CurrentState { get; private set; } = AppState.Boot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(AppState state)
        {
            CurrentState = state;
        }
    }
}
