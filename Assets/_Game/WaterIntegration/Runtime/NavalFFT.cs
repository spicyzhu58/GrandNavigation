using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace GrandNavigation.Gameplay.Water
{
    // Simulation adapted from the supplied FFTOcean.cs. Rendering is owned by the user's water shader.
    [DefaultExecutionOrder(-200)]
    public sealed class NavalFFT : MonoBehaviour
    {
        public ComputeShader spectrum;
        public bool useFFT=true;
        public float patchSize=250,windSpeed=12,amplitude=.04f,choppiness=.6f;
        public Vector2 windDirection=new Vector2(1,.6f);
        public float foamThreshold=.6f,foamScale=2,foamDecay=.5f;
        public int seed=1;
        public RenderTexture Displacement {get;private set;}
        public RenderTexture NormalFoam {get;private set;}
        public bool Ready=>Displacement&&Displacement.IsCreated();
        public bool HasHeightSamples=>heights!=null;
        public int FrameCount {get;private set;}
        RenderTexture h0,spec1,spec2;ComputeShader compute;
        int init,time,horizontal,vertical,assemble,generation;
        bool dirty=true,pending;Vector4[] heights;float readAt;
        const int N=256;
        void OnEnable()
        {
            generation++;dirty=true;
            if(!spectrum||!SystemInfo.supportsComputeShaders){enabled=false;return;}
            compute=Instantiate(spectrum);
            init=compute.FindKernel("InitSpectrum");time=compute.FindKernel("TimeSpectrum");horizontal=compute.FindKernel("FFTHorizontal");vertical=compute.FindKernel("FFTVertical");assemble=compute.FindKernel("Assemble");
            h0=Texture(RenderTextureFormat.ARGBFloat);spec1=Texture(RenderTextureFormat.ARGBFloat);spec2=Texture(RenderTextureFormat.ARGBFloat);
            Displacement=Texture(RenderTextureFormat.ARGBHalf);NormalFoam=Texture(RenderTextureFormat.ARGBHalf);
        }
        static RenderTexture Texture(RenderTextureFormat format)
        {
            var rt=new RenderTexture(N,N,0,format,RenderTextureReadWrite.Linear){enableRandomWrite=true,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};rt.Create();
            var previous=RenderTexture.active;RenderTexture.active=rt;GL.Clear(false,true,Color.clear);RenderTexture.active=previous;return rt;
        }
        void OnValidate(){patchSize=Mathf.Max(1,patchSize);amplitude=Mathf.Max(0,amplitude);dirty=true;}
        void Update()=>Simulate(Time.time,Time.deltaTime);
        public void Simulate(float seconds,float delta)
        {
            if(!Ready)return;
            if(dirty)
            {
                compute.SetFloat("_PatchSize",patchSize);compute.SetFloat("_WindSpeed",windSpeed);compute.SetVector("_WindDir",windDirection.sqrMagnitude>0?windDirection.normalized:Vector2.right);
                compute.SetFloat("_Amplitude",amplitude*1e-3f);compute.SetInt("_Seed",seed);compute.SetTexture(init,"_H0",h0);compute.Dispatch(init,N/8,N/8,1);dirty=false;
            }
            compute.SetFloat("_OceanTime",seconds);compute.SetFloat("_DeltaTime",Mathf.Clamp(delta,0,.1f));compute.SetFloat("_PatchSize",patchSize);
            compute.SetFloat("_Choppiness",choppiness);compute.SetFloat("_FoamThreshold",foamThreshold);compute.SetFloat("_FoamScale",foamScale);compute.SetFloat("_FoamDecay",foamDecay);
            compute.SetTexture(time,"_H0",h0);compute.SetTexture(time,"_Spec1",spec1);compute.SetTexture(time,"_Spec2",spec2);compute.Dispatch(time,32,32,1);
            TransformSpectrum(spec1);TransformSpectrum(spec2);
            compute.SetTexture(assemble,"_Spec1",spec1);compute.SetTexture(assemble,"_Spec2",spec2);compute.SetTexture(assemble,"_Displacement",Displacement);compute.SetTexture(assemble,"_Normal",NormalFoam);compute.Dispatch(assemble,32,32,1);
            Shader.SetGlobalTexture("_NavalFFTDisplacement",Displacement);Shader.SetGlobalTexture("_NavalFFTNormal",NormalFoam);
            Shader.SetGlobalFloat("_NavalPatchSize",patchSize);Shader.SetGlobalFloat("_NavalFFTEnabled",useFFT?1:0);FrameCount++;
            // Only needed by visual splash placement. Gameplay keeps the existing mean-sea-plane ballistics.
            if(Application.isPlaying&&!pending&&SystemInfo.supportsAsyncGPUReadback&&Time.unscaledTime>=readAt)
            {
                pending=true;readAt=Time.unscaledTime+.1f;int version=generation;
                AsyncGPUReadback.Request(Displacement,0,TextureFormat.RGBAFloat,request=>
                {if(!this||version!=generation)return;pending=false;if(!request.hasError){heights??=new Vector4[N*N];request.GetData<Vector4>().CopyTo(heights);}});
            }
        }
        void TransformSpectrum(RenderTexture texture)
        {
            compute.SetTexture(horizontal,"_Target",texture);compute.Dispatch(horizontal,1,N,1);
            compute.SetTexture(vertical,"_Target",texture);compute.Dispatch(vertical,1,N,1);
        }
        public float Height(Vector3 position)
        {
            if(!useFFT||heights==null)return 0;
            Vector2 rest=new Vector2(position.x,position.z);Vector4 value=Vector4.zero;
            for(int i=0;i<4;i++){value=Sample(rest);rest=new Vector2(position.x-value.x,position.z-value.z);}
            return value.y;
        }
        Vector4 Sample(Vector2 p)
        {
            float x=Mathf.Repeat(p.x/patchSize,1)*N-.5f,z=Mathf.Repeat(p.y/patchSize,1)*N-.5f;int ix=Mathf.FloorToInt(x),iz=Mathf.FloorToInt(z);
            Vector4 At(int a,int b)=>heights[((b%N+N)%N)*N+(a%N+N)%N];
            return Vector4.Lerp(Vector4.Lerp(At(ix,iz),At(ix+1,iz),x-ix),Vector4.Lerp(At(ix,iz+1),At(ix+1,iz+1),x-ix),z-iz);
        }
        void OnDisable()
        {
            generation++;pending=false;heights=null;Shader.SetGlobalFloat("_NavalFFTEnabled",0);
            foreach(var rt in new[]{h0,spec1,spec2,Displacement,NormalFoam})if(rt){rt.Release();Destroy(rt);}
            if(compute)Destroy(compute);Displacement=NormalFoam=null;
        }
    }
}
