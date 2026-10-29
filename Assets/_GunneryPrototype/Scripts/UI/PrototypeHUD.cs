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
            screen=new Vector2(p.x,Screen.height-p.y); return p.z>0 && p.x>=0 && p.x<=Screen.width && p.y>=0 && p.y<=Screen.height;
        }
        void Marker(Vector3 world,Color color,bool cross)
        {
            if(!Project(world,out var p)) return;
            if(cross) { Line(new Rect(p.x-13,p.y-1,26,2),color); Line(new Rect(p.x-1,p.y-13,2,26),color); }
            else { Line(new Rect(p.x-9,p.y-9,18,2),color); Line(new Rect(p.x-9,p.y+7,18,2),color); Line(new Rect(p.x-9,p.y-9,2,18),color); Line(new Rect(p.x+7,p.y-9,2,18),color); }
        }
        [Header("Prediction marker (visual only)")]
        [Min(.001f)] public float predictionSmoothTime=.12f;
        public Vector3 DisplayedPrediction { get; private set; }
        public bool PredictionVisible { get; private set; }
        Vector3 markerVelocity;
        Health markerTarget;
        void LateUpdate() => UpdatePredictionDisplay(Time.deltaTime);
        public void UpdatePredictionDisplay(float delta)
        {
            var prediction=session ? session.prediction : null;
            PredictionVisible=prediction && prediction.Valid && session.selection.HasTarget;
            if(!PredictionVisible) { markerTarget=null; markerVelocity=Vector3.zero; return; }
            var target=session.selection.LockedTarget;
            if(markerTarget!=target)
            {
                markerTarget=target; markerVelocity=Vector3.zero;
                DisplayedPrediction=prediction.PredictedIntercept; return;
            }
            if(delta<=0 || !BallisticSolver.Finite(delta)) return;
            float smooth=BallisticSolver.Finite(predictionSmoothTime)?Mathf.Max(.001f,predictionSmoothTime):.12f;
            DisplayedPrediction=Vector3.SmoothDamp(DisplayedPrediction,prediction.PredictedIntercept,
                ref markerVelocity,smooth,Mathf.Infinity,delta);
        }
        void OnDisable() { PredictionVisible=false; markerTarget=null; markerVelocity=Vector3.zero; }
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
                "LMB: lock / salvo   RMB: aim / overview   Wheel: zoom   Esc: unlock   R: reset\n1: stop   2: lateral 8   3: diagonal 12   4: maneuver 10 m/s   Yellow: predicted intercept   Blue: YOUR impact point",label);
            // Smoothing affects only the HUD; fire control and manual aim retain their original world data.
            if(PredictionVisible) Marker(DisplayedPrediction+Vector3.up*.2f,Color.yellow,false);
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
