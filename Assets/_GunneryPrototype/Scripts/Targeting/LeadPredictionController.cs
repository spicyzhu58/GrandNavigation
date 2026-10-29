using System.Collections.Generic;
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
        [Min(.05f)] public float fireControlUpdateInterval=2.5f;
        [Header("Uncertainty region (metres; independent of gun dispersion)")]
        [Min(.1f)] public float baseLongitudinalRadius=8, baseLateralRadius=5;
        [Min(.1f)] public float minRadius=3, maxRadius=18;
        [Min(0)] public float ageGrowthPerSecond=.8f, distanceGrowthPerMetre=.015f, speedGrowth=.1f, turnGrowthPerDegree=.06f;
        [Min(.1f)] public float regionResponse=4;
        public Vector2 RegionRadii { get; private set; } = new Vector2(5,8);
        public Vector3 RegionHeading { get; private set; } = Vector3.forward;
        public Vector3 RegionCenter { get; private set; }
        Health displayedTarget;
        public Vector3 PredictedIntercept { get; private set; }
        public float FlightTime { get; private set; }
        public bool Valid { get; private set; }
        public string Status { get; private set; }
        public Vector3 SampledTargetPosition => active?.Position ?? Vector3.zero;
        public Vector3 SampledTargetVelocity => active?.Velocity ?? Vector3.zero;
        public double SampleTimestamp => active?.Timestamp ?? 0;
        public float SampleAge { get; private set; }
        public Vector3 SampledHeading => active?.Heading ?? Vector3.forward;
        public float ObservedTurnDegrees => active?.ObservedTurnDegrees ?? 0;
        public int SampleCount => active?.ObservationCount ?? 0;
        public int CachedTargetCount => tracks.Count;
        readonly Dictionary<Health,FireControlTrack> tracks=new Dictionary<Health,FireControlTrack>();
        readonly List<Health> invalidTargets=new List<Health>();
        FireControlTrack active;
        double lastTime=double.NegativeInfinity;
        void OnEnable() { if(selection) selection.SelectionChanged+=Refresh; }
        void Update() { Refresh(); UpdateRegion(Time.deltaTime); }
        public Vector2 ComputeRegionRadii(float distance,float speed,float age,float turn)
        {
            float lo=BallisticSolver.Finite(minRadius)?Mathf.Max(.1f,minRadius):3;
            float hi=BallisticSolver.Finite(maxRadius)?Mathf.Max(lo,maxRadius):18;
            float Safe(float v,float fallback) => BallisticSolver.Finite(v)?Mathf.Max(0,v):fallback;
            float growth=Mathf.Max(0,Safe(distance,100)-100)*Safe(distanceGrowthPerMetre,.015f)+
                Safe(speed,0)*Safe(speedGrowth,.1f)+Safe(age,0)*Safe(ageGrowthPerSecond,.8f)+
                Safe(turn,0)*Safe(turnGrowthPerDegree,.06f);
            return new Vector2(Mathf.Clamp(Safe(baseLateralRadius,5)+growth*.55f,lo,hi),
                Mathf.Clamp(Safe(baseLongitudinalRadius,8)+growth,lo,hi));
        }
        public void UpdateRegion(float delta)
        {
            if(!Valid) { displayedTarget=null; return; }
            Vector2 desired=ComputeRegionRadii(Vector3.Distance(fireControl.position,PredictedIntercept),
                SampledTargetVelocity.magnitude,SampleAge,ObservedTurnDegrees);
            if(displayedTarget!=selection.LockedTarget)
            {
                displayedTarget=selection.LockedTarget; RegionRadii=desired; RegionHeading=SampledHeading; RegionCenter=PredictedIntercept;
                return;
            }
            float blend=1-Mathf.Exp(-Mathf.Max(.1f,regionResponse)*Mathf.Max(0,delta));
            RegionRadii=Vector2.Lerp(RegionRadii,desired,blend);
            float yaw=Mathf.LerpAngle(Mathf.Atan2(RegionHeading.x,RegionHeading.z)*Mathf.Rad2Deg,
                Mathf.Atan2(SampledHeading.x,SampledHeading.z)*Mathf.Rad2Deg,blend)*Mathf.Deg2Rad;
            RegionHeading=new Vector3(Mathf.Sin(yaw),0,Mathf.Cos(yaw));
            RegionCenter=Vector3.Lerp(RegionCenter,PredictedIntercept,blend);
        }
        public Vector3 InitialAimNearRegion()
        {
            // An off-centre, deterministic initial point. This does not offset any projectile or observation.
            return PredictedIntercept+Vector3.Cross(Vector3.up,SampledHeading)*
                Mathf.Clamp(baseLateralRadius*.45f,1,4);
        }
        public void Refresh() => RefreshAt(Time.timeAsDouble);
        // Explicit clock also makes pause, slow motion and observation boundaries testable.
        public void RefreshAt(double now)
        {
            Valid=false; FlightTime=0; SampleAge=0; active=null; PredictedIntercept=Vector3.zero;
            if(!FireControlTrack.FiniteTime(now)) { Status="INVALID CLOCK"; return; }
            if(now<lastTime) tracks.Clear();
            lastTime=now;
            invalidTargets.Clear();
            foreach(var pair in tracks) if(!pair.Key || !pair.Key.IsAlive) invalidTargets.Add(pair.Key);
            foreach(var key in invalidTargets) tracks.Remove(key);
            if(!selection || !selection.HasTarget || !fireControl) { Status="NO TARGET - click the orange ship"; return; }
            Health target=selection.LockedTarget;
            if(!tracks.TryGetValue(target,out active)) { active=new FireControlTrack(); tracks.Add(target,active); }
            if(!active.UpdateObservation(now,fireControlUpdateInterval,()=>target.transform.position,()=>
            {
                var motion=target.GetComponent<TargetMotion>();
                return motion ? motion.Velocity : Vector3.zero;
            })) { Status="INVALID OBSERVATION"; return; }
            SampleAge=active.Age(now);
            if(active.Predict(now,fireControl.position,muzzleSpeed,gravity,maxRange,out var point,out var t))
            { PredictedIntercept=point; FlightTime=t; Valid=true; Status="FIRE CONTROL ESTIMATE"; }
            else Status="NO INTERCEPT / OUT OF RANGE";
        }
        // Debug battle reset starts a new engagement; ordinary unlock/relock never clears live-target history.
        public void ClearSamples()
        {
            tracks.Clear(); active=null; displayedTarget=null; Valid=false; SampleAge=0; FlightTime=0; PredictedIntercept=Vector3.zero;
            lastTime=double.NegativeInfinity;
        }
        void OnDisable() { if(selection) selection.SelectionChanged-=Refresh; ClearSamples(); }
    }
}