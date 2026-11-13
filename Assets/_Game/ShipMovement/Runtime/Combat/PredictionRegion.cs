using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class PredictionRegion : MonoBehaviour
    {
        public CombatFireControl fireControl;public Material fillMaterial,edgeMaterial;
        [Min(.1f)] public float longitudinalRadius=8,lateralRadius=4;
        [Min(.001f)] public float smoothTime=.12f;
        [Min(.02f)] public float borderWidth=.15f;
        [Tooltip("Draw the smoothed region through CombatHUD instead of the world mesh.")]
        public bool screenOverlay;
        public bool Visible {get;private set;}
        public Vector3 DisplayCentre=>centre;
        public Vector3 DisplayForward=>Quaternion.Euler(0,heading,0)*Vector3.forward;
        void OnDisable(){Visible=false;displayed=null;if(surface)surface.enabled=false;}
        Mesh mesh;MeshRenderer surface;Vector3 velocity,centre;float heading;Ship displayed;
        const int Segments=64;
        void Awake()
        {
            mesh=new Mesh{name="Prediction ellipse"};gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            surface=gameObject.AddComponent<MeshRenderer>();surface.sharedMaterials=new[]{fillMaterial,edgeMaterial};
            surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;surface.receiveShadows=false;
        }
        void LateUpdate()
        {
            bool visible=fireControl.Valid&&fireControl.HasVisibleTarget;Visible=visible;surface.enabled=visible&&!screenOverlay;if(!visible){displayed=null;return;}
            float desired=Mathf.Atan2(fireControl.ObservedHeading.x,fireControl.ObservedHeading.z)*Mathf.Rad2Deg;
            if(displayed!=fireControl.Target){displayed=fireControl.Target;centre=fireControl.PredictedIntercept;velocity=Vector3.zero;heading=desired;}
            else {centre=Vector3.SmoothDamp(centre,fireControl.PredictedIntercept,ref velocity,smoothTime);heading=Mathf.LerpAngle(heading,desired,1-Mathf.Exp(-Time.deltaTime/smoothTime));}
            if(screenOverlay)return;
            Vector3 forward=Quaternion.Euler(0,heading,0)*Vector3.forward,right=Vector3.Cross(Vector3.up,forward);
            var vertices=new Vector3[Segments*2+1];var fill=new int[Segments*3];var edge=new System.Collections.Generic.List<int>();vertices[0]=centre+Vector3.up*.08f;
            for(int i=0;i<Segments;i++)
            {
                int next=(i+1)%Segments;float a=i*Mathf.PI*2/Segments;
                vertices[i+1]=vertices[0]+right*(Mathf.Cos(a)*lateralRadius)+forward*(Mathf.Sin(a)*longitudinalRadius);
                vertices[i+1+Segments]=vertices[0]+right*(Mathf.Cos(a)*Mathf.Max(.1f,lateralRadius-borderWidth))+forward*(Mathf.Sin(a)*Mathf.Max(.1f,longitudinalRadius-borderWidth));
                fill[i*3]=0;fill[i*3+1]=next+1;fill[i*3+2]=i+1;
                if(i%4<2)edge.AddRange(new[]{1+i,1+next,1+Segments+i,1+next,1+Segments+next,1+Segments+i});
            }
            mesh.Clear();mesh.vertices=vertices;mesh.subMeshCount=2;mesh.SetTriangles(fill,0);mesh.SetTriangles(edge,1);mesh.RecalculateBounds();
        }
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
