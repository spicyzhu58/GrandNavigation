#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WaterInteraction
{
    /// <summary>
    /// 编辑模式的可恢复姿态预览。不调用 Physics.Simulate，不修改其他刚体。
    /// 水面高度与运行时共享采样，姿态通过浮点所采水面的最小二乘平面得到。
    /// </summary>
    internal sealed class ShallowWaterBuoyancyPreview : IDisposable
    {
        readonly ShallowWaterBuoyancy owner;
        Vector3 originalLocalPosition, lastLocalPosition;
        Quaternion originalLocalRotation, lastLocalRotation;
        Transform originalParent;
        double lastUpdate = -1.0;
        bool poseStored, disposed;

        internal bool IsRunning => poseStored && !disposed;

        internal ShallowWaterBuoyancyPreview(ShallowWaterBuoyancy owner)
        {
            this.owner = owner;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            AssemblyReloadEvents.beforeAssemblyReload += Restore;
            EditorApplication.quitting += Restore;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void Update()
        {
            if (disposed || owner == null) return;
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!owner.isActiveAndEnabled || !owner.previewInEditMode || SceneView.sceneViews.Count == 0
                || !ShallowWaterSimulation.IsSceneInstance(owner.gameObject)
                || owner.floatPoints == null || owner.floatPoints.Length == 0 || owner.buoyancyStrength <= 1f)
            {
                Restore();
                return;
            }
            double now = EditorApplication.timeSinceStartup;
            if (lastUpdate >= 0.0 && now - lastUpdate < 1.0 / 30.0) return;
            float delta = lastUpdate < 0.0 ? 1f / 30f : (float)Math.Min(now - lastUpdate, 0.1);
            lastUpdate = now;
            CaptureExternalEdits();
            // 拖动对象时让操作手柄优先，松开后继续跟随。
            if (GUIUtility.hotControl != 0 && Selection.Contains(owner.gameObject)) return;
            owner.PrepareHeightSampling();
            ApplyPose(delta);
            // 同时驱动 SceneView 的水 Shader 时钟和后续交互 RT / 高度查询。
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        void CaptureExternalEdits()
        {
            Transform target = owner.transform;
            if (!poseStored || originalParent != target.parent)
            {
                originalParent = target.parent;
                originalLocalPosition = target.localPosition;
                originalLocalRotation = target.localRotation;
                poseStored = true;
            }
            else
            {
                // 预览过程中拖动物体时，把手动编辑增量保留到原始姿态，避免关闭后丢失布局修改。
                originalLocalPosition += target.localPosition - lastLocalPosition;
                if (Quaternion.Angle(target.localRotation, lastLocalRotation) > 0.001f)
                    originalLocalRotation = target.localRotation * Quaternion.Inverse(lastLocalRotation) * originalLocalRotation;
            }
            RememberDisplayedPose();
        }

        void ApplyPose(float delta)
        {
            ShallowWaterSimulation source = owner.simulation != null ? owner.simulation : ShallowWaterSimulation.Active;
            WaterWaveSampling waves = WaterWaveSampling.FromMaterial(owner.waterSurface != null ? owner.waterSurface.sharedMaterial : null);
            float baseHeight = owner.BaseWaterHeight(source);
            float time = WaterWaveSampling.SampleTime;
            Vector3 center = Vector3.zero;
            int count = 0;
            for (int i = 0; i < owner.floatPoints.Length; i++)
            {
                Vector3 point = owner.GetFloatPoint(i);
                if (!owner.ContainsWater(point, source)) continue;
                center += new Vector3(point.x, owner.SurfaceHeight(i, point, source, waves, baseHeight, time), point.z);
                count++;
            }
            if (count == 0) { Restore(); return; }
            center /= count;
            float xx = 0f, zz = 0f, xz = 0f, xy = 0f, zy = 0f;
            for (int i = 0; i < owner.floatPoints.Length; i++)
            {
                Vector3 point = owner.GetFloatPoint(i);
                if (!owner.ContainsWater(point, source)) continue;
                float x = point.x - center.x, z = point.z - center.z;
                float y = owner.SurfaceHeight(i, point, source, waves, baseHeight, time) - center.y;
                xx += x * x; zz += z * z; xz += x * z; xy += x * y; zy += z * y;
            }
            float determinant = xx * zz - xz * xz;
            Vector3 normal = Vector3.up;
            if (determinant > 1e-8f)
                normal = new Vector3(-(xy * zz - zy * xz) / determinant, 1f,
                    -(zy * xx - xy * xz) / determinant).normalized;
            normal = Vector3.RotateTowards(Vector3.up, normal, Mathf.Clamp(owner.previewMaxTilt, 0f, 80f) * Mathf.Deg2Rad, 0f);
            Quaternion authoredRotation = originalParent != null
                ? originalParent.rotation * originalLocalRotation : originalLocalRotation;
            Quaternion targetRotation = Quaternion.FromToRotation(authoredRotation * Vector3.up, normal) * authoredRotation;
            float blend = 1f - Mathf.Exp(-Mathf.Max(0f, owner.previewFollowSpeed) * delta);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, blend);

            // 新旋转会改变浮点世界位置，再计算高度偏差，保留 XZ 布局和作者的朝向。
            float draft = Mathf.Max(0.001f, owner.submergeDepth) / owner.buoyancyStrength;
            float offset = 0f;
            count = 0;
            for (int i = 0; i < owner.floatPoints.Length; i++)
            {
                Vector3 point = owner.GetFloatPoint(i);
                if (!owner.ContainsWater(point, source)) continue;
                offset += owner.SurfaceHeight(i, point, source, waves, baseHeight, time) - draft - point.y;
                count++;
            }
            if (count > 0) owner.transform.position += Vector3.up * (offset / count * blend);
            RememberDisplayedPose();
        }

        void RememberDisplayedPose()
        {
            lastLocalPosition = owner.transform.localPosition;
            lastLocalRotation = owner.transform.localRotation;
        }

        void OnUndoRedo()
        {
            if (!poseStored || owner == null) return;
            Transform target = owner.transform;
            // Undo 产生的是绝对编辑位置；只在本物体确实变化时采用，其他物体的 Undo 不影响快照。
            if ((target.localPosition - lastLocalPosition).sqrMagnitude <= 1e-12f
                && Quaternion.Angle(target.localRotation, lastLocalRotation) <= 0.001f) return;
            originalParent = target.parent;
            originalLocalPosition = target.localPosition;
            originalLocalRotation = target.localRotation;
            RememberDisplayedPose();
        }

        void OnSceneSaving(Scene scene, string path)
        {
            if (owner != null && owner.gameObject.scene == scene) Restore();
        }

        void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) Restore();
        }

        void Restore()
        {
            if (owner == null) return;
            if (poseStored)
            {
                CaptureExternalEdits();
                owner.transform.SetLocalPositionAndRotation(originalLocalPosition, originalLocalRotation);
                poseStored = false;
            }
            if (!Application.IsPlaying(owner.gameObject)) owner.StopHeightSampling();
            lastUpdate = -1.0;
        }

        public void Dispose()
        {
            if (disposed) return;
            Restore();
            disposed = true;
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            AssemblyReloadEvents.beforeAssemblyReload -= Restore;
            EditorApplication.quitting -= Restore;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }
    }
}
#endif
