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
        readonly System.Collections.Generic.Dictionary<Health,Vector3> savedPoints=new System.Collections.Generic.Dictionary<Health,Vector3>();
        public bool Begin()
        {
            prediction.Refresh();
            if(!prediction.Valid) return false;
            if(!initialized || aimTarget!=prediction.selection.LockedTarget)
            {
                PlayerAimPoint=savedPoints.TryGetValue(prediction.selection.LockedTarget,out var saved) ? saved : prediction.InitialAimNearRegion(); PlayerAimPoint=new Vector3(PlayerAimPoint.x,0,PlayerAimPoint.z);
                aimTarget=prediction.selection.LockedTarget; initialized=true; savedPoints[aimTarget]=PlayerAimPoint;
            }
            forward=PlayerAimPoint-prediction.fireControl.position; forward.y=0; forward.Normalize();
            right=Vector3.Cross(Vector3.up,forward);
            IsAiming=true; return true;
        }
        public void End() => IsAiming=false;
        public void ResetAim() { End(); aimTarget=null; initialized=false; savedPoints.Clear(); }
        public void Adjust(Vector2 delta)
        {
            if(!IsAiming || !BallisticSolver.Finite(delta.x) || !BallisticSolver.Finite(delta.y)) return;
            PlayerAimPoint+=(right*delta.x+forward*delta.y)*metresPerPixel;
            PlayerAimPoint=new Vector3(Mathf.Clamp(PlayerAimPoint.x,-195,195),0,Mathf.Clamp(PlayerAimPoint.z,-125,265));
            if(aimTarget) savedPoints[aimTarget]=PlayerAimPoint;
        }
        public bool HasBallisticSolution()
        {
            Vector3 flat=PlayerAimPoint-prediction.fireControl.position; flat.y=0;
            return IsAiming && flat.magnitude<=prediction.maxRange &&
                BallisticSolver.TrySolve(prediction.fireControl.position,PlayerAimPoint,prediction.muzzleSpeed,prediction.gravity,out _,out _);
        }
    }
}