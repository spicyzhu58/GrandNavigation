using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WaterInteraction
{
    /// <summary>
    /// 一个浅水域共用的浮点高度查询。只回读每个浮点的一个 float，最多一批在途请求。
    /// 不同步等待 GPU；资源重建时旧请求自行完成并释放，不把旧结果写回物体。
    /// </summary>
    internal sealed class ShallowWaterHeightQueries : IDisposable
    {
        static readonly int PositionsId = Shader.PropertyToID("_HeightQueryPositions");
        static readonly int ResultsId = Shader.PropertyToID("_HeightQueryResults");
        static readonly int CountId = Shader.PropertyToID("_HeightQueryCount");
        static readonly int FadeId = Shader.PropertyToID("_HeightQueryEdgeFade");
        static readonly int StateId = Shader.PropertyToID("_StateRead");

        struct Receiver
        {
            public ShallowWaterBuoyancy body;
            public int index;
            public int version;
        }

        readonly Action<AsyncGPUReadbackRequest> onComplete;
        ComputeBuffer positionBuffer, resultBuffer;
        Vector2[] positions;
        Receiver[] receivers;
        ShallowWaterSimulation source;
        int count, capacity, generation;
        float sampleTime;
        bool pending, disposed, reportedUnsupported;

        internal ShallowWaterHeightQueries() => onComplete = Complete;

        internal void Record(CommandBuffer cmd, ComputeShader solver, int kernel, RenderTexture state,
            ShallowWaterSimulation simulation, List<ShallowWaterBuoyancy> bodies)
        {
            if (disposed || pending) return;
            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                if (!reportedUnsupported)
                    Debug.LogWarning("设备不支持异步 GPU 高度读回，浮力将继续跟随平均水面和 Gerstner 波。", simulation);
                reportedUnsupported = true;
                return;
            }
            int required = 0;
            foreach (ShallowWaterBuoyancy body in bodies)
                if (body != null) required += body.QueryPointCount;
            if (required == 0) return;
            EnsureCapacity(required);
            count = 0;
            foreach (ShallowWaterBuoyancy body in bodies)
            {
                if (body == null) continue;
                for (int point = 0; point < body.QueryPointCount; point++)
                {
                    Vector3 world = body.GetFloatPoint(point);
                    positions[count] = new Vector2(world.x, world.z);
                    receivers[count] = new Receiver { body = body, index = point, version = body.QueryVersion };
                    count++;
                }
            }
            source = simulation;
            generation = simulation.HeightQueryGeneration;
            sampleTime = WaterWaveSampling.SampleTime;
            cmd.SetBufferData(positionBuffer, positions, 0, 0, count);
            cmd.SetComputeBufferParam(solver, kernel, PositionsId, positionBuffer);
            cmd.SetComputeBufferParam(solver, kernel, ResultsId, resultBuffer);
            cmd.SetComputeTextureParam(solver, kernel, StateId, state);
            cmd.SetComputeIntParam(solver, CountId, count);
            cmd.SetComputeFloatParam(solver, FadeId, Mathf.Max(0.001f, simulation.edgeFade));
            cmd.DispatchCompute(solver, kernel, (count + 63) / 64, 1, 1);
            pending = true;
            cmd.RequestAsyncReadback(resultBuffer, count * sizeof(float), 0, onComplete);
        }

        void EnsureCapacity(int required)
        {
            if (capacity >= required) return;
            ReleaseBuffers();
            capacity = Mathf.NextPowerOfTwo(required);
            positionBuffer = new ComputeBuffer(capacity, sizeof(float) * 2);
            resultBuffer = new ComputeBuffer(capacity, sizeof(float));
            positions = new Vector2[capacity];
            receivers = new Receiver[capacity];
        }

        void Complete(AsyncGPUReadbackRequest request)
        {
            pending = false;
            if (disposed) { ReleaseBuffers(); return; }
            try
            {
                if (request.hasError || source == null || generation != source.HeightQueryGeneration) return;
                var values = request.GetData<float>();
                for (int i = 0; i < count; i++)
                {
                    Receiver receiver = receivers[i];
                    if (receiver.body != null)
                        receiver.body.ReceiveHeight(source, receiver.version, generation, receiver.index,
                            positions[i], values[i], sampleTime);
                }
            }
            finally
            {
                Array.Clear(receivers, 0, count);
                source = null;
            }
        }

        public void Dispose()
        {
            disposed = true;
            // 在途的 readback 持有 GPU 缓冲；完成回调负责最后释放，避免阻塞主线程。
            if (!pending) ReleaseBuffers();
        }

        void ReleaseBuffers()
        {
            positionBuffer?.Release();
            resultBuffer?.Release();
            positionBuffer = null;
            resultBuffer = null;
            capacity = 0;
            positions = null;
            receivers = null;
        }
    }
}
