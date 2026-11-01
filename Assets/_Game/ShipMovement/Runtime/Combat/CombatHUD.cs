using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class CombatHUD : MonoBehaviour
    {
        public PlayerShip player;public CombatFireControl fireControl;public CombatAimController aim;public Camera view;
        [Range(16,100)] public float aimEdgeMargin=30;
        [Header("Actual barrel impact markers")]
        [Tooltip("Maximum separation of ALL six impact points to merge, in metres.")]
        [Min(.1f)] public float mergeDistance=3;
        [Min(.1f)] public float splitDistance=4.5f;
        [Min(0)] public float mergeDelay=.15f;
        [Range(5,20)] public float mergedCircleRadius=9;
        public bool ImpactsMerged {get;private set;}
        public Vector3 FrontImpact {get;private set;}
        public Vector3 RearImpact {get;private set;}
        readonly Vector3[] impacts=new Vector3[6];
        bool frontValid,rearValid;float mergeAge;
        void LateUpdate()
        {
            frontValid=rearValid=false;
            if(!player||!player.IsAlive||!player.Weapon||player.Weapon.turrets.Length<3){ImpactsMerged=false;mergeAge=0;return;}
            var turrets=player.Weapon.turrets;
            bool all=true;
            for(int i=0;i<3;i++)
            {
                bool valid=turrets[i]&&turrets[i].TryGetImpact(0,out impacts[i*2]);
                valid=turrets[i]&&turrets[i].TryGetImpact(1,out impacts[i*2+1])&&valid;
                if(i==0)frontValid=valid;else if(i==1)frontValid&=valid;else rearValid=valid;
                all&=valid;
            }
            FrontImpact=(impacts[0]+impacts[1]+impacts[2]+impacts[3])*.25f;
            RearImpact=(impacts[4]+impacts[5])*.5f;
            float separation=0;
            if(all)for(int i=0;i<6;i++)for(int j=i+1;j<6;j++)separation=Mathf.Max(separation,Vector3.Distance(impacts[i],impacts[j]));
            if(!all||separation>(ImpactsMerged?splitDistance:mergeDistance)){ImpactsMerged=false;mergeAge=0;}
            else {mergeAge+=Time.deltaTime;if(mergeAge>=mergeDelay)ImpactsMerged=true;}
        }
        void OnValidate(){mergeDistance=Mathf.Max(.1f,mergeDistance);splitDistance=Mathf.Max(mergeDistance,splitDistance);mergeDelay=Mathf.Max(0,mergeDelay);}
        void OnGUI()
        {
            if(!player)return;GUI.color=Color.white;GUI.Box(new Rect(12,12,440,235),"");
            var w=player.Weapon;string reload="";for(int i=0;i<w.turrets.Length;i++){var t=w.turrets[i];reload+=$"T{i+1}: {(t.ReloadRemaining>0?t.ReloadRemaining.ToString("F1")+"s":t.Aligned?"READY / ON TARGET":"READY / TRAVERSING")}  ";}
            GUI.Label(new Rect(24,24,420,220),$"PLAYER COMBAT V0.1    HP {player.Health.CurrentHealth}/{player.Health.maximumHealth}\nSpeed {player.Movement.ActualSpeed:F1}  Target {player.Movement.TargetSpeed:F1} m/s\nRudder {player.Movement.RudderAngle:F0} deg\n"+
                (fireControl.Target?$"LOCK: {fireControl.Target.name}  HP {fireControl.Target.Health.CurrentHealth}/{fireControl.Target.Health.maximumHealth}  {Vector3.ProjectOnPlane(fireControl.Target.WorldPosition-player.WorldPosition,Vector3.up).magnitude:F0} UU\n":"NO TARGET\n")+
                (fireControl.Valid?$"Observation {fireControl.SampleAge:F2}s / {fireControl.observationInterval:F1}s\n":"NO VALID PREDICTION / OUT OF RANGE\n")+reload+"\n"+w.Status+"\nWASD drive | RMB lock at centre reticle | Hold Shift aim\nMouse aims turrets / orbit | Wheel zoom | LMB salvo | Esc unlock\nR reset orbit | Yellow: prediction area | Gray: aim command | Blue: barrel impacts");
            DrawAim(aim.PlayerAimPoint,new Color(.8f,.8f,.8f));
            if(ImpactsMerged)DrawAim((FrontImpact*2+RearImpact)/3,Color.cyan,true);
            else
            {
                if(frontValid)DrawAim(FrontImpact,Color.cyan,false,"F");
                if(rearValid)DrawAim(RearImpact,Color.cyan,false,"R");
            }
            if(fireControl.Target)
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
            GUI.color=Color.white;
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
