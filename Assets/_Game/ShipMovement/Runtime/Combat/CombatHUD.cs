using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class CombatHUD : MonoBehaviour
    {
        public PlayerShip player;public CombatFireControl fireControl;public CombatAimController aim;public Camera view;
        public PredictionRegion predictionRegion;
        [Range(16,100)] public float aimEdgeMargin=30;
        [Header("Actual barrel impact markers")]
        [Tooltip("Maximum separation of all main-battery impact points to merge, in metres.")]
        [Min(.1f)] public float mergeDistance=3;
        [Min(.1f)] public float splitDistance=4.5f;
        [Min(0)] public float mergeDelay=.15f;
        [Range(5,20)] public float mergedCircleRadius=9;
        public bool ImpactsMerged {get;private set;}
        public Vector3 FrontImpact {get;private set;}
        public Vector3 RearImpact {get;private set;}
        readonly System.Collections.Generic.List<Vector3> impacts=new System.Collections.Generic.List<Vector3>();
        Vector3 mergedImpact;
        bool frontValid,rearValid;float mergeAge;
        void Start()
        {
            // Older scenes may not serialize the optional overlay reference yet.
            if(predictionRegion)return;
            foreach(var region in FindObjectsByType<PredictionRegion>(FindObjectsSortMode.None))
                if(region.fireControl==fireControl){predictionRegion=region;break;}
            // Art test scenes deliberately disabled the water-occluded world mesh.
            if(predictionRegion&&!predictionRegion.enabled){predictionRegion.screenOverlay=true;predictionRegion.enabled=true;}
        }
        void LateUpdate()
        {
            frontValid=rearValid=false;
            if(!player||!player.IsAlive||!player.Weapon||player.Weapon.turrets.Length==0){ImpactsMerged=false;mergeAge=0;return;}
            var turrets=player.Weapon.turrets;
            bool all=true,frontOK=true,rearOK=true;int frontCount=0,rearCount=0;
            FrontImpact=RearImpact=mergedImpact=Vector3.zero;impacts.Clear();
            for(int i=0;i<turrets.Length;i++)
            {
                var t=turrets[i];if(!t||t.muzzles==null||t.muzzles.Length==0){all=false;continue;}
                bool rear=Mathf.Abs(Mathf.DeltaAngle(0,t.neutralYaw))>90;
                for(int j=0;j<t.muzzles.Length;j++)
                {
                    bool valid=t.TryGetImpact(j,out var point);all&=valid;
                    if(rear){rearOK&=valid;RearImpact+=point;rearCount++;}else{frontOK&=valid;FrontImpact+=point;frontCount++;}
                    impacts.Add(point);mergedImpact+=point;
                }
            }
            frontValid=frontOK&&frontCount>0;rearValid=rearOK&&rearCount>0;
            if(frontCount>0)FrontImpact/=frontCount;if(rearCount>0)RearImpact/=rearCount;
            if(impacts.Count>0)mergedImpact/=impacts.Count;else all=false;
            float separation=0;
            if(all)for(int i=0;i<impacts.Count;i++)for(int j=i+1;j<impacts.Count;j++)separation=Mathf.Max(separation,Vector3.Distance(impacts[i],impacts[j]));
            if(!all||separation>(ImpactsMerged?splitDistance:mergeDistance)){ImpactsMerged=false;mergeAge=0;}
            else {mergeAge+=Time.deltaTime;if(mergeAge>=mergeDelay)ImpactsMerged=true;}
        }
        void OnValidate(){mergeDistance=Mathf.Max(.1f,mergeDistance);splitDistance=Mathf.Max(mergeDistance,splitDistance);mergeDelay=Mathf.Max(0,mergeDelay);}
        void OnGUI()
        {
            if(!player)return;GUI.color=Color.white;GUI.Box(new Rect(12,12,440,235),"");
            var w=player.Weapon;string reload="";for(int i=0;i<w.turrets.Length;i++){var t=w.turrets[i];reload+=$"T{i+1}: {(t.ReloadRemaining>0?t.ReloadRemaining.ToString("F1")+"s":t.Aligned?"READY / ON TARGET":"READY / TRAVERSING")}  ";}
            GUI.Label(new Rect(24,24,420,220),$"PLAYER COMBAT V0.1    HP {player.Health.CurrentHealth}/{player.Health.maximumHealth}\nSpeed {player.Movement.ActualSpeed:F1}  Target {player.Movement.TargetSpeed:F1} m/s\nRudder {player.Movement.RudderAngle:F0} deg\n"+
                (fireControl.HasVisibleTarget?$"LOCK: {fireControl.Target.name}  HP {fireControl.Target.Health.CurrentHealth}/{fireControl.Target.Health.maximumHealth}  {Vector3.ProjectOnPlane(fireControl.Target.WorldPosition-player.WorldPosition,Vector3.up).magnitude:F0} UU\n":(fireControl.State==CombatFireControl.LockState.Occluded?"TARGET OCCLUDED\n":"NO TARGET\n"))+
                (fireControl.Valid?$"Observation {fireControl.SampleAge:F2}s / {fireControl.observationInterval:F1}s\n":"NO VALID PREDICTION / OUT OF RANGE\n")+reload+"\n"+w.Status+"\nWASD drive | RMB lock at centre reticle | Hold Shift aim\nMouse aims turrets / orbit | Wheel zoom | LMB salvo | Esc unlock\nR reset orbit | Yellow: prediction area | Gray: aim command | Blue: barrel impacts");
            DrawAim(aim.PlayerAimPoint,new Color(.8f,.8f,.8f));
            if(ImpactsMerged)DrawAim(mergedImpact,Color.cyan,true);
            else
            {
                if(frontValid)DrawAim(FrontImpact,Color.cyan,false,"F");
                if(rearValid)DrawAim(RearImpact,Color.cyan,false,"R");
            }
            if(fireControl.HasVisibleTarget)
            {
                var p=view.WorldToScreenPoint(fireControl.Target.WorldPosition+Vector3.up*4);
                if(p.z>0)
                {
                    float x=p.x,y=Screen.height-p.y;GUI.color=Color.green;
                    GUI.DrawTexture(new Rect(x-42,y-18,84,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x-42,y+16,84,2),Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x-42,y-18,2,36),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x+40,y-18,2,36),Texture2D.whiteTexture);
                    GUI.color=Color.black;GUI.DrawTexture(new Rect(x-40,y-32,80,7),Texture2D.whiteTexture);
                    GUI.color=Color.green;GUI.DrawTexture(new Rect(x-40,y-32,80f*fireControl.Target.Health.CurrentHealth/fireControl.Target.Health.maximumHealth,7),Texture2D.whiteTexture);
                    GUI.Label(new Rect(x-42,y-55,120,22),fireControl.Target.Health.CurrentHealth+" HP");
                }
            }
            DrawPrediction();GUI.color=Color.white;
        }
        public bool PredictionVisible => predictionRegion && predictionRegion.isActiveAndEnabled && predictionRegion.screenOverlay && predictionRegion.Visible && fireControl && fireControl.Valid && fireControl.HasVisibleTarget;
        void DrawPrediction()
        {
            if(!PredictionVisible || !view)return;
            Vector3 centre=predictionRegion.DisplayCentre;
            Vector3 forward=predictionRegion.DisplayForward;
            if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            GUI.color=new Color(1,.85f,.05f,1);
            // Tactical overlay is drawn after the water, independent of wave depth and transparency sorting.
            for(int i=0;i<64;i++)
            {
                if(i%4>=2)continue;
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                var p=view.WorldToScreenPoint(centre+right*Mathf.Cos(a)*predictionRegion.lateralRadius+forward*Mathf.Sin(a)*predictionRegion.longitudinalRadius);
                var q=view.WorldToScreenPoint(centre+right*Mathf.Cos(b)*predictionRegion.lateralRadius+forward*Mathf.Sin(b)*predictionRegion.longitudinalRadius);
                if(p.z>view.nearClipPlane&&q.z>view.nearClipPlane)DrawLine(new Vector2(p.x,Screen.height-p.y),new Vector2(q.x,Screen.height-q.y));
            }
        }
        static void DrawLine(Vector2 a,Vector2 b)
        {
            var previous=GUI.matrix;Vector2 delta=b-a;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
            GUI.DrawTexture(new Rect(a.x,a.y-1,delta.magnitude,2),Texture2D.whiteTexture);GUI.matrix=previous;
        }
        void DrawAim(Vector3 point,Color color,bool circle=false,string label="")
        {
            var p=view.WorldToScreenPoint(point);
            if(p.z<=0||p.x<0||p.x>Screen.width||p.y<0||p.y>Screen.height)
            {
                if(!aim.IsAiming)return;
                Vector2 centre=new Vector2(Screen.width,Screen.height)*.5f;
                Vector2 direction=new Vector2(p.x-centre.x,centre.y-p.y);
                if(p.z<=0)direction=-direction;
                if(direction.sqrMagnitude<.001f)direction=Vector2.down;
                direction.Normalize();
                Vector2 bounds=new Vector2(Mathf.Max(1,centre.x-aimEdgeMargin),Mathf.Max(1,centre.y-aimEdgeMargin));
                float distance=Mathf.Min(bounds.x/Mathf.Max(.0001f,Mathf.Abs(direction.x)),bounds.y/Mathf.Max(.0001f,Mathf.Abs(direction.y)));
                Vector2 edge=centre+direction*distance;
                Matrix4x4 previous=GUI.matrix;GUI.color=color;
                GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg,edge);
                GUI.Label(new Rect(edge.x-8,edge.y-12,24,24),">");
                GUI.matrix=previous;
                return;
            }
            GUI.color=color;float x=p.x,y=Screen.height-p.y;
            if(circle)
            {
                for(int i=0;i<32;i++)
                {
                    float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;
                    DrawLine(new Vector2(x,y)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*mergedCircleRadius,
                             new Vector2(x,y)+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*mergedCircleRadius);
                }
            }
            else
            {
                GUI.DrawTexture(new Rect(x-7,y-1,14,2),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x-1,y-7,2,14),Texture2D.whiteTexture);
                if(label.Length>0)GUI.Label(new Rect(x+10,y-10,24,20),label);
            }
        }
    }
}
