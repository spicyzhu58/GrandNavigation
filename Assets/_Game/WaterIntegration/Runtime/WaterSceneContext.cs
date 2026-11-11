using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WaterInteraction;

namespace GrandNavigation.Gameplay.Water
{
    [ExecuteAlways,DefaultExecutionOrder(-300)]
    public sealed class WaterSceneContext:MonoBehaviour
    {
        public UniversalRenderPipelineAsset pipeline;
        public Camera view;
        public Transform waterGrid;
        public Material waterMaterial,proxyMaterial;
        public NavalFFT fft;
        public ShallowWaterSimulation simulation;
        public Vector4[] islands=new Vector4[3];
        public int WaterImpactCount {get;private set;}
        public int AcceptedImpulses {get;private set;}
        public Vector3 LastImpact {get;private set;}
        public int ShipCount=>ships.Count;
        RenderPipelineAsset previous;bool ownsPipeline;Mesh mesh;float scanAt;
        readonly List<Ship> ships=new List<Ship>();
        void OnEnable()
        {
            if(!pipeline||!Application.isPlaying)return;
            previous=QualitySettings.renderPipeline;QualitySettings.renderPipeline=pipeline;ownsPipeline=true;
            if(Application.isPlaying)
            {
                CombatEffects.WaterImpact+=Impact;CombatEffects.WaterVisualPosition=VisualPosition;
                if(waterGrid){mesh=Grid();waterGrid.GetComponent<MeshFilter>().sharedMesh=mesh;}
            }
        }
        void Update()
        {
            if(!view||!waterGrid)return;
            Vector3 p=view.transform.position;p.y=0;
            // Fixed world sampling keeps waves continuous as the mesh follows the camera.
            waterGrid.position=new Vector3(Mathf.Round(p.x/8)*8,0,Mathf.Round(p.z/8)*8);
            Shader.SetGlobalVector("_NavalWaveCenter",p);Shader.SetGlobalVectorArray("_NavalIslands",islands);
            if(!Application.isPlaying)return;
            if(simulation&&Vector3.Distance(p,simulation.transform.position)>32)
            {
                float cell=simulation.size.x/simulation.resolution;
                simulation.transform.position=new Vector3(Mathf.Round(p.x/cell)*cell,0,Mathf.Round(p.z/cell)*cell);
            }
            if(Time.unscaledTime>=scanAt){scanAt=Time.unscaledTime+.5f;AttachShips();}
        }
        void AttachShips()
        {
            ships.RemoveAll(s=>!s);
            foreach(var ship in FindObjectsByType<Ship>(FindObjectsSortMode.None))
            {
                if(ships.Contains(ship))continue;ships.Add(ship);
                var proxy=GameObject.CreatePrimitive(PrimitiveType.Cube);proxy.name="WaterContactProxy";Destroy(proxy.GetComponent<Collider>());
                proxy.transform.SetParent(ship.transform,false);
                var box=ship.GetComponent<BoxCollider>();Vector3 size=box?box.size:new Vector3(4,3,20);
                proxy.transform.localScale=new Vector3(size.x*.8f,.2f,size.z*.9f);proxy.transform.localPosition=Vector3.up*(-ship.transform.position.y);
                proxy.GetComponent<Renderer>().sharedMaterial=proxyMaterial;
                var interactor=proxy.AddComponent<ShallowWaterInteractor>();interactor.displacement=.12f;interactor.rippleWidth=.7f;interactor.contactBand=.4f;
                // The proxy shader has no visible pass; it is drawn only with the capture override material.
            }
        }
        public Vector3 VisualPosition(Vector3 p)
        {
            float weight=WaveWeight(p);
            p.y=fft?fft.Height(p)*weight:0;return p;
        }
        public float WaveWeight(Vector3 p)
        {
            Vector3 c=view?view.transform.position:Vector3.zero;c.y=0;p.y=0;
            float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(600,1800,Vector3.Distance(p,c)));
            foreach(var island in islands)
            {
                float x=(p.x-island.x)/Mathf.Max(1,island.z),z=(p.z-island.y)/Mathf.Max(1,island.w);
                weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f,1.35f,Mathf.Sqrt(x*x+z*z)));
            }
            return weight;
        }
        void Impact(Vector3 p)
        {
            WaterImpactCount++;LastImpact=p;
            if(simulation&&simulation.AddImpulse(p,3,.14f))AcceptedImpulses++;
            // Impact rings also remain visible beyond the high-resolution local simulation window.
            var go=new GameObject("Water impact ring");var ring=go.AddComponent<WaterImpactRing>();ring.Initialize(p,this);
        }
        void OnDisable()
        {
            CombatEffects.WaterImpact-=Impact;if(CombatEffects.WaterVisualPosition==VisualPosition)CombatEffects.WaterVisualPosition=null;
            if(ownsPipeline&&QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=previous;ownsPipeline=false;
            Shader.SetGlobalFloat("_ShallowWaterEnabled",0);Shader.SetGlobalFloat("_WaterSSREnabled",0);Shader.SetGlobalFloat("_NavalFFTEnabled",0);
            if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
        }
        public static Mesh Grid()
        {
            var positive=new List<float>{0};float n=0;
            void Band(int count,float step){for(int i=0;i<count;i++){n+=step;positive.Add(n);}}
            Band(64,2);Band(32,8);Band(16,32);Band(8,128);Band(8,512);
            var axis=new List<float>();for(int i=positive.Count-1;i>0;i--)axis.Add(-positive[i]);axis.AddRange(positive);
            int count=axis.Count;var v=new Vector3[count*count];var normals=new Vector3[v.Length];var tangents=new Vector4[v.Length];var uv=new Vector2[v.Length];
            for(int z=0;z<count;z++)for(int x=0;x<count;x++){int i=z*count+x;v[i]=new Vector3(axis[x],0,axis[z]);normals[i]=Vector3.up;tangents[i]=new Vector4(1,0,0,-1);uv[i]=new Vector2(axis[x],axis[z])/1000;}
            var triangles=new int[(count-1)*(count-1)*6];int t=0;
            for(int z=0;z<count-1;z++)for(int x=0;x<count-1;x++){int a=z*count+x;triangles[t++]=a;triangles[t++]=a+count;triangles[t++]=a+1;triangles[t++]=a+1;triangles[t++]=a+count;triangles[t++]=a+count+1;}
            var m=new Mesh{name="Camera centred ocean grid",indexFormat=IndexFormat.UInt32};m.vertices=v;m.normals=normals;m.tangents=tangents;m.uv=uv;m.triangles=triangles;m.bounds=new Bounds(Vector3.zero,new Vector3(n*2+20,40,n*2+20));return m;
        }
    }
    public sealed class WaterImpactRing:MonoBehaviour
    {
        LineRenderer line;WaterSceneContext water;Vector3 centre;float age;
        public void Initialize(Vector3 p,WaterSceneContext context)
        {
            centre=p;water=context;line=gameObject.AddComponent<LineRenderer>();line.loop=true;line.positionCount=48;line.useWorldSpace=true;line.widthMultiplier=.15f;
            line.sharedMaterial=context.GetComponent<WaterRingMaterial>().material;line.startColor=line.endColor=new Color(.8f,.96f,1,.8f);
        }
        void Update()
        {
            age+=Time.deltaTime;if(age>3||!water){Destroy(gameObject);return;}
            float radius=.6f+age*3;line.startColor=line.endColor=new Color(.8f,.96f,1,(1-age/3)*.7f);line.widthMultiplier=.12f+age*.06f;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;var p=centre+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);line.SetPosition(i,water.VisualPosition(p)+Vector3.up*.07f);}
        }
    }
}
