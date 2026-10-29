using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class TurretController : MonoBehaviour
    {
        public Transform yawBase, elevationPivot;
        public Transform[] muzzles=new Transform[2];
        public float traverseDegrees=65, elevationDegrees=45, alignmentTolerance=1.5f;
        public float reloadSeconds=5;
        public ManualAimController aim;
        public LeadPredictionController prediction;
        public float ReloadRemaining => Mathf.Max(0,readyAt-Time.time);
        public bool Aligned { get; private set; }
        public bool HasSolution { get; private set; }
        float readyAt;
        public virtual bool WithinFiringArc(Vector3 point) => true;
        public string State => ReloadRemaining>0 ? "RELOAD "+ReloadRemaining.ToString("F1")+"s" :
            !aim || !aim.IsAiming ? "STANDBY" : !HasSolution ? "NO SOLUTION" : !Aligned ? "TRAVERSING" : "READY";
        public bool CanFire => ReloadRemaining<=0 && Aligned && HasSolution;
        public void ResetReload() => readyAt=0;
        public void MarkFired() => readyAt=Time.time+Mathf.Max(.1f,reloadSeconds);
        void Update() => Track(Time.deltaTime);
        public void Track(float dt)
        {
            Aligned=false; HasSolution=false;
            if(!aim || !aim.IsAiming || !aim.HasBallisticSolution() || !prediction || muzzles.Length!=2) return;
            Vector3 point=aim.PlayerAimPoint;
            if(!WithinFiringArc(point) || !muzzles[0] || !muzzles[1]) return;
            Vector3 delta=point-yawBase.position; delta.y=0;
            if(delta.sqrMagnitude<.001f) return;
            yawBase.rotation=Quaternion.RotateTowards(yawBase.rotation,Quaternion.LookRotation(delta),traverseDegrees*dt);
            if(!BallisticSolver.TrySolve(muzzles[0].position,point,prediction.muzzleSpeed,prediction.gravity,out var velocity,out _)) return;
            float pitch=-Mathf.Atan2(velocity.y,new Vector2(velocity.x,velocity.z).magnitude)*Mathf.Rad2Deg;
            elevationPivot.localRotation=Quaternion.RotateTowards(elevationPivot.localRotation,Quaternion.Euler(pitch,0,0),elevationDegrees*dt);
            HasSolution=true; Aligned=true;
            foreach(var muzzle in muzzles)
                if(!BallisticSolver.TrySolve(muzzle.position,point,prediction.muzzleSpeed,prediction.gravity,out var v,out _) ||
                    Vector3.Angle(muzzle.forward,v)>alignmentTolerance) Aligned=false;
        }
    }
}