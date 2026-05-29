using System.Collections.Generic;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Container for NPC wander targets. Add this to an empty GameObject,
    /// then create child empty GameObjects as the actual waypoint positions.
    /// NPCs find this automatically at startup if no reference is set.
    /// </summary>
    public class WaypointGroup : MonoBehaviour
    {
        /// <summary>
        /// Returns a random child waypoint, optionally excluding the one just visited.
        /// </summary>
        public Transform GetRandom(Transform exclude = null)
        {
            var candidates = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child != exclude)
                    candidates.Add(child);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[WaypointGroup] No usable waypoints found.");
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
            foreach (Transform child in transform)
            {
                Gizmos.DrawSphere(child.position, 0.12f);
                Gizmos.DrawLine(transform.position, child.position);
            }
        }
#endif
    }
}
