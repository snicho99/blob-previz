using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Place on the same GameObject as the Animator (the character mesh child).
    /// Absorbs animation events fired by Starter Assets walk/run clips so Unity
    /// doesn't spam "has no receiver" warnings.
    ///
    /// Add real behaviour here later if needed (footstep audio, VFX, etc.).
    /// </summary>
    public class NpcAnimationEvents : MonoBehaviour
    {
        void OnFootstep(AnimationEvent e) { }
        void OnLand(AnimationEvent e)     { }
    }
}
