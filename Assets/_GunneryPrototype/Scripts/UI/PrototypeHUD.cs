using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class PrototypeHUD : MonoBehaviour
    {
        public PrototypeSession session;
        GUIStyle label,big; Texture2D pixel;
        void Initialize()
        {
            if(label!=null) return;
            label=new GUIStyle(GUI.skin.label) { fontSize=16,wordWrap=true };
            label.normal.textColor=Color.white;
            big=new GUIStyle(label) { fontSize=20,fontStyle=FontStyle.Bold };
            pixel=Texture2D.whiteTexture;
        }
        void Line(Rect r,Color color) { GUI.color=color; GUI.DrawTexture(r,pixel); GUI.color=Color.white; }
        bool Project(Vector3 world,out Vector2 screen)
        {
            Vector3 p=session.cameraRig.view.WorldToScreenPoint(world);
            screen=new Vector2(p.x,Screen.height-p.y); return p.z>0;
        }
        void Marker(Vector3 world,Color color,bool cross)
        {
            if(!Project(world,out var p)) return;
            if(cross) { Line(new Rect(p.x-13,p.y-1,26,2),color); Line(new Rect(p.x-1,p.y-13,2,26),color); }
            else { Line(new Rect(p.x-9,p.y-9,18,2),color); Line(new Rect(p.x-9,p.y+7,18,2),color); Line(new Rect(p.x-9,p.y-9,2,18),color); Line(new Rect(p.x+7,p.y-9,2,18),color); }
        }
        [Header("Fire-control region appearance")]
        [Range(0,.3f)] public float regionFillOpacity=.08f;
        [Range(0,1)] public float regionBorderOpacity=.5f;
        [Min(.02f)] public float regionBorderWidth=.15f;
        GameObject regionObject; Mesh regionMesh; Material fillMaterial,borderMaterial;
        const int Segments=64;
        Material RegionMaterial(float alpha)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface",1); material.SetFloat("_Blend",0);
            material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite",0); material.SetFloat("_Cull",0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue=3000;
            material.SetColor("_BaseColor",new Color(1,.85f,.05f,alpha)); return material;
        }
        void CreateRegion()
        {
            regionObject=new GameObject("Observed fire-control area (no centre marker)");
            regionObject.transform.SetParent(transform,false);
            regionObject.AddComponent<MeshFilter>().sharedMesh=regionMesh=new Mesh { name="FireControlEllipse" };
            var renderer=regionObject.AddComponent<MeshRenderer>();
            fillMaterial=RegionMaterial(regionFillOpacity); borderMaterial=RegionMaterial(regionBorderOpacity);
            renderer.sharedMaterials=new[]{fillMaterial,borderMaterial};
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
        }
        void LateUpdate()
        {
            if(!session || !session.prediction) return;
            if(!regionObject) CreateRegion();
            var prediction=session.prediction;
            bool visible=prediction.Valid && session.selection.HasTarget;
            regionObject.SetActive(visible);
            if(!visible) return;
            var vertices=new Vector3[1+Segments*2]; var fill=new int[Segments*3];
            var edge=new System.Collections.Generic.List<int>(Segments*3);
            Vector2 radius=prediction.RegionRadii;
            for(int i=0;i<Segments;i++)
            {
                float a=i*Mathf.PI*2/Segments; int next=(i+1)%Segments;
                vertices[1+i]=new Vector3(Mathf.Cos(a)*radius.x,0,Mathf.Sin(a)*radius.y);
                vertices[1+Segments+i]=new Vector3(Mathf.Cos(a)*Mathf.Max(.1f,radius.x-regionBorderWidth),0,
                    Mathf.Sin(a)*Mathf.Max(.1f,radius.y-regionBorderWidth));
                fill[i*3]=0; fill[i*3+1]=1+next; fill[i*3+2]=1+i;
                if(i%4<2) edge.AddRange(new[]{1+i,1+next,1+Segments+i,1+next,1+Segments+next,1+Segments+i});
            }
            regionMesh.Clear(); regionMesh.vertices=vertices; regionMesh.subMeshCount=2;
            regionMesh.SetTriangles(fill,0); regionMesh.SetTriangles(edge,1); regionMesh.RecalculateBounds();
            regionObject.transform.SetPositionAndRotation(prediction.RegionCenter+Vector3.up*.08f,
                Quaternion.LookRotation(prediction.RegionHeading,Vector3.up));
            fillMaterial.SetColor("_BaseColor",new Color(1,.85f,.05f,regionFillOpacity));
            borderMaterial.SetColor("_BaseColor",new Color(1,.85f,.05f,regionBorderOpacity));
            // Real world geometry uses the camera's perspective, clipping and depth test:
            // near-plane/frustum clipping is automatic; opaque target hulls occlude the fill.
        }
        void OnDisable() { if(regionObject) regionObject.SetActive(false); }
        void OnDestroy()
        {
            if(regionMesh) Destroy(regionMesh);
            if(fillMaterial) Destroy(fillMaterial);
            if(borderMaterial) Destroy(borderMaterial);
            if(regionObject) Destroy(regionObject);
        }
        void OnGUI()
        {
            Initialize(); var select=session.selection; var prediction=session.prediction; var aim=session.aim;
            GUI.Box(new Rect(12,12,355,208),"");
            GUI.Label(new Rect(24,20,335,30),"GRAND NAVIGATION | V0.2",big);
            string target=select.HasTarget ? "TargetShip  |  "+Vector3.Distance(prediction.fireControl.position,select.TargetPosition).ToString("F0")+" m" : "NO LOCK";
            GUI.Label(new Rect(24,54,330,28),target,label);
            GUI.Label(new Rect(24,82,330,48),prediction.Status+(prediction.Valid ? "\nObservation age "+prediction.SampleAge.ToString("F1")+" / "+prediction.fireControlUpdateInterval.ToString("F1")+" s" : ""),label);
            GUI.Label(new Rect(24,130,330,50),session.target.mode+" | "+session.target.Velocity.magnitude.ToString("F1")+" m/s\n"+(aim.IsAiming ? "Observe splashes, then correct cyan aim." : session.Notice),label);
            if(!aim.IsAiming && GUI.Button(new Rect(24,184,130,26),"Reset [R]")) session.ResetBattle();
            GUI.Label(new Rect(170,184,180,26),aim.IsAiming?"AIM VIEW":"OVERVIEW",label);
            GUI.Box(new Rect(12,Screen.height-76,Screen.width-24,64),"");
            GUI.Label(new Rect(24,Screen.height-70,Screen.width-48,56),
                "LMB: lock / salvo   RMB: aim / overview   Wheel: zoom   Esc: unlock   R: reset\n1: stop   2: lateral 8   3: diagonal 12   4: maneuver 10 m/s   Yellow: uncertain area   Blue: YOUR impact point",label);
            // Prediction is rendered only as an area: no precise centre marker.
            if(aim.IsAiming) Marker(aim.PlayerAimPoint+Vector3.up*.2f,Color.cyan,true);
            if(select.HasTarget && Project(select.TargetPosition+Vector3.up*4,out var p))
            {
                Line(new Rect(p.x-42,p.y-18,84,2),Color.green); Line(new Rect(p.x-42,p.y-18,2,36),Color.green);
                Line(new Rect(p.x+40,p.y-18,2,36),Color.green); Line(new Rect(p.x-42,p.y+16,84,2),Color.green);
                var hp=select.LockedTarget;
                Line(new Rect(p.x-40,p.y-32,80,7),Color.black); Line(new Rect(p.x-40,p.y-32,80*hp.CurrentHP/hp.maxHP,7),Color.green);
                GUI.Label(new Rect(p.x-45,p.y-57,140,25),hp.CurrentHP.ToString("F0")+" HP",label);
                if(Time.time-hp.LastHitTime<1.1f) GUI.Label(new Rect(p.x+46,p.y-25-(Time.time-hp.LastHitTime)*25,100,30),"-"+hp.LastDamage.ToString("F0"),big);
            }
            if(aim.IsAiming && !aim.HasBallisticSolution()) GUI.Label(new Rect(Screen.width/2-150,Screen.height/2+60,320,40),"AIM POINT OUT OF RANGE",big);
        }
    }
}