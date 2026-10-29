using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class ManualAimController : MonoBehaviour
    {
        public LeadPredictionController prediction;
        public float metresPerPixel=.12f;
        public Vector3 PlayerAimPoint { get; private set; }
        public bool IsAiming { get; private set; }
        Vector3 forward,right;
        Health aimTarget; bool initialized;
        public bool Begin()
        {
            prediction.Refresh();
            if(!prediction.Valid) return false;
            if(!initialized || aimTarget!=prediction.selection.LockedTarget)
            {
                PlayerAimPoint=prediction.PredictedIntercept; PlayerAimPoint=new Vector3(PlayerAimPoint.x,0,PlayerAimPoint.z);
                aimTarget=prediction.selection.LockedTarget; initialized=true;
            }
            forward=PlayerAimPoint-prediction.fireControl.position; forward.y=0; forward.Normalize();
            right=Vector3.Cross(Vector3.up,forward);
            IsAiming=true; return true;
        }
        public void End() => IsAiming=false;
        public void ResetAim() { End(); aimTarget=null; initialized=false; }
        public void Adjust(Vector2 delta)
        {
            if(!IsAiming || !BallisticSolver.Finite(delta.x) || !BallisticSolver.Finite(delta.y)) return;
            PlayerAimPoint+=(right*delta.x+forward*delta.y)*metresPerPixel;
            PlayerAimPoint=new Vector3(Mathf.Clamp(PlayerAimPoint.x,-195,195),0,Mathf.Clamp(PlayerAimPoint.z,-125,265));
        }
        public bool HasBallisticSolution()
        {
            Vector3 flat=PlayerAimPoint-prediction.fireControl.position; flat.y=0;
            return IsAiming && flat.magnitude<=prediction.maxRange &&
                BallisticSolver.TrySolve(prediction.fireControl.position,PlayerAimPoint,prediction.muzzleSpeed,prediction.gravity,out _,out _);
        }
    }
}