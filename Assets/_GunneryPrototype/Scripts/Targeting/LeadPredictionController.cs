using UnityEngine;
namespace GrandNavigation.Gunnery
{
    [DefaultExecutionOrder(-20)]
    public class LeadPredictionController : MonoBehaviour
    {
        public TargetSelectionController selection;
        public Transform fireControl;
        [Min(1)] public float muzzleSpeed=65;
        [Min(.01f)] public float gravity=9.81f;
        [Min(1)] public float maxRange=250;
        public Vector3 PredictedIntercept { get; private set; }
        public float FlightTime { get; private set; }
        public bool Valid { get; private set; }
        public string Status { get; private set; }
        void Update() => Refresh();
        public void Refresh()
        {
            Valid=false;
            if(!selection || !selection.HasTarget || !fireControl) { Status="NO TARGET - click the orange ship"; return; }
            var motion=selection.LockedTarget.GetComponent<TargetMotion>();
            if(BallisticSolver.TryIntercept(fireControl.position,selection.TargetPosition,motion ? motion.Velocity : Vector3.zero,
                muzzleSpeed,gravity,maxRange,out var point,out var t))
            { PredictedIntercept=point; FlightTime=t; Valid=true; Status="LEAD SOLUTION"; }
            else Status="NO INTERCEPT / OUT OF RANGE";
        }
    }
}