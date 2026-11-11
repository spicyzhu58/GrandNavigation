using System.Collections.Generic;
using UnityEngine;

namespace WaterInteraction
{
    /// <summary>
    /// 注册会扰动水面的物体。模拟器直接绘制这些 Renderer 的俯视轮廓，
    /// 不依赖主相机可见性、物体原材质或物理碰撞回调。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ShallowWaterInteractor : MonoBehaviour
    {
        internal static readonly List<ShallowWaterInteractor> Instances = new List<ShallowWaterInteractor>();

        [Tooltip("用于俯视投影的 MeshRenderer / SkinnedMeshRenderer，可指定简化代理网格。留空时在启用组件时获取子节点 Renderer。")]
        [SerializeField] Renderer[] targetRenderers;

        [Tooltip("轮廓变化产生的水位变化，单位米。正值：进入时抬高，离开时降低。重叠物体取最大值。")]
        [Min(0f)] public float displacement = 0.08f;

        [Tooltip("扰动轮廓的平滑半径（米）。越大，激发的波形越宽缓；0 保留原轮廓。平滑可能降低小物体的峰值强度，可配合 Displacement 调整。")]
        [Min(0f)] public float rippleWidth = 0f;

        [Tooltip("平均水面上下的接触过渡带，单位米。远离这个带的物体不会被捕获。")]
        [Min(0.001f)] public float contactBand = 0.15f;

        internal Renderer[] TargetRenderers => targetRenderers;

        void Reset() => targetRenderers = GetComponentsInChildren<Renderer>();

        void OnEnable()
        {
            if (!ShallowWaterSimulation.IsSceneInstance(gameObject)) return;
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>();
            if (!Instances.Contains(this)) Instances.Add(this);
        }

        void OnDisable() => Instances.Remove(this);

        internal float GetContactValue(Bounds bounds, float waterLevel)
        {
            float band = Mathf.Max(contactBand, 0.001f);
            // AABB 只负责判定是否接触平均水面；实际 XZ 轮廓仍由网格光栅化。
            // 两个过渡项分别避免入水、完全潜入时接触突然开关。
            float enter = Mathf.Clamp01((waterLevel + band - bounds.min.y) / (2f * band));
            float leave = Mathf.Clamp01((bounds.max.y - waterLevel + band) / (2f * band));
            return Mathf.Max(0f, displacement) * enter * leave;
        }
    }
}
