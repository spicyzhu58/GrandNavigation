using UnityEngine;

namespace GrandNavigation.Gameplay.Water.Tests
{
    // Batchmode has no Game view repaint; explicitly exercise the real URP render path.
    public sealed class BatchWaterCamera : MonoBehaviour
    {
        public Camera view;
        void LateUpdate(){if(view && view.targetTexture)view.Render();}
    }
}
