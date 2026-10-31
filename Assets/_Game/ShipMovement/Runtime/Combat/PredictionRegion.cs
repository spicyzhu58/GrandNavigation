using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class PredictionRegion : MonoBehaviour
    {
        public CombatFireControl fireControl;public Material fillMaterial,edgeMaterial;
        [Min(.1f)] public float longitudinalRadius=8,lateralRadius=4;
        [Min(.001f)] public float smoothTime=.12f;
        Mesh mesh;MeshRenderer surface;LineRenderer edge;Vector3 velocity,centre;float heading;Ship displayed;
        const int Segments=64;
        void Awake()
        {
            mesh=new Mesh{name="Prediction ellipse"};var filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            surface=gameObject.AddComponent<MeshRenderer>();surface.sharedMaterial=fillMaterial;
            edge=gameObject.AddComponent<LineRenderer>();edge.sharedMaterial=edgeMaterial;edge.widthMultiplier=.12f;edge.positionCount=Segments;edge.loop=true;edge.useWorldSpace=true;
        }
        void LateUpdate()
        {
            bool visible=fireControl.Valid&&fireControl.Target;surface.enabled=edge.enabled=visible;if(!visible){displayed=null;return;}
            float desired=Mathf.Atan2(fireControl.ObservedHeading.x,fireControl.ObservedHeading.z)*Mathf.Rad2Deg;
            if(displayed!=fireControl.Target){displayed=fireControl.Target;centre=fireControl.PredictedIntercept;velocity=Vector3.zero;heading=desired;}
            else {centre=Vector3.SmoothDamp(centre,fireControl.PredictedIntercept,ref velocity,smoothTime);heading=Mathf.LerpAngle(heading,desired,1-Mathf.Exp(-Time.deltaTime/smoothTime));}
            Vector3 forward=Quaternion.Euler(0,heading,0)*Vector3.forward,right=Vector3.Cross(Vector3.up,forward);
            var vertices=new Vector3[Segments+1];var indices=new int[Segments*3];vertices[0]=centre+Vector3.up*.08f;
            for(int i=0;i<Segments;i++)
            {
                float a=i*Mathf.PI*2/Segments;vertices[i+1]=vertices[0]+right*(Mathf.Cos(a)*lateralRadius)+forward*(Mathf.Sin(a)*longitudinalRadius);
                edge.SetPosition(i,vertices[i+1]);indices[i*3]=0;indices[i*3+1]=i+1;indices[i*3+2]=(i+1)%Segments+1;
            }
            mesh.Clear();mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateBounds();
        }
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
