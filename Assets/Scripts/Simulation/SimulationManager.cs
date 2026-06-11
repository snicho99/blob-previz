using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Single entry point for the simulation. Place on an empty GameObject in the root of the scene.
    /// Holds the SimulationConfig reference so all other scripts can find it via FindFirstObjectByType.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public SimulationConfig Config;

        void Awake()
        {
            if (Config == null)
                Debug.LogError("[SimulationManager] No SimulationConfig assigned.");
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (Config == null) return;
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Gizmos.DrawCube(Config.captureVolume.center, Config.captureVolume.size);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(Config.captureVolume.center, Config.captureVolume.size);
        }
#endif
    }
}
