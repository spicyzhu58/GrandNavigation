using UnityEngine;
namespace GrandNavigation.Gameplay
{
    [DefaultExecutionOrder(-20)]
    public sealed class CombatFireControl : MonoBehaviour
    {
        public PlayerShip player;public Transform reference;
        [Min(.05f)] public float observationInterval=.5f;
        [Header("Tactical target visibility")]
        [Min(.01f)] public float visibilityCheckInterval=.1f;
        [Min(0)] public float lockLossDelay=1.5f;
        public bool requireLineOfSight=true, partialVisibilityAllowed=true;
        [Tooltip("Ship bridge observer. When absent, use player position + observerHeight.")] public Transform observationPoint;
        [Min(0)] public float observerHeight=6;
        [Min(1)] public float maximumLockDistance=1200;
        public LayerMask obstructionMask=~((1<<2)|(1<<4)|(1<<5));
        public bool debugVisibility;
        public enum LockState { Unlocked, Locked, Occluded }
        public LockState State {get;private set;}
        public bool HasVisibleTarget=>State==LockState.Locked && Target && Target.IsAlive && Target.isActiveAndEnabled;
        public Vector3 ObserverPosition=>observationPoint?observationPoint.position:player.WorldPosition+Vector3.up*observerHeight;
        public Vector3 LastVisiblePosition {get;private set;}
        public float OcclusionAge=>State==LockState.Occluded?(float)(Time.timeAsDouble-occludedSince):0;
        double nextVisibilityCheck,occludedSince;
        public Ship Target {get;private set;}
        public bool Valid {get;private set;}
        public Vector3 PredictedIntercept {get;private set;}
        public Vector3 ObservedHeading {get;private set;}=Vector3.forward;
        public float FlightTime {get;private set;}
        public float SampleAge=>(float)System.Math.Max(0,Time.timeAsDouble-observation.Timestamp);
        public readonly FireControlObservation observation=new FireControlObservation();
        public bool CanObserve(Ship target)=>!requireLineOfSight||TargetSelectionHelper.ShipVisible(ObserverPosition,target,player,obstructionMask,partialVisibilityAllowed);
        void ClearLock()
        {
            if(Target && Target.Health)Target.Health.Died-=OnTargetDied;
            Target=null;State=LockState.Unlocked;observation.Clear();Valid=false;
            PredictedIntercept=LastVisiblePosition=Vector3.zero;FlightTime=0;ObservedHeading=Vector3.forward;
            nextVisibilityCheck=occludedSince=0;
        }
        void OnTargetDied()=>ClearLock();
        void OnDisable()=>ClearLock();
        public void Lock(Ship target)
        {
            if(!target){ClearLock();return;}
            if(!player||!player.IsAlive||!TargetSelectionHelper.Eligible(target,player,maximumLockDistance,true)||!CanObserve(target))return;
            if(target==Target)
            {
                // A visible re-selection can restore sight immediately; hidden re-clicks were rejected above.
                if(State==LockState.Occluded){nextVisibilityCheck=Time.timeAsDouble;Refresh();}
                return;
            }
            ClearLock();Target=target;Target.Health.Died+=OnTargetDied;
            State=LockState.Locked;ObservedHeading=target.WorldForward;
            nextVisibilityCheck=Time.timeAsDouble+visibilityCheckInterval;Refresh();
        }
        void OnValidate(){visibilityCheckInterval=Mathf.Max(.01f,visibilityCheckInterval);lockLossDelay=Mathf.Max(0,lockLossDelay);maximumLockDistance=Mathf.Max(1,maximumLockDistance);}
        void OnDrawGizmosSelected(){if(debugVisibility&&player&&State!=LockState.Unlocked){Gizmos.color=HasVisibleTarget?Color.green:Color.yellow;Gizmos.DrawLine(ObserverPosition,LastVisiblePosition);}}
        void Update()=>Refresh();
        public void Refresh()
        {
            Valid=false;
            if(!player||!player.IsAlive||!Target||!Target.IsAlive||!Target.isActiveAndEnabled){ClearLock();return;}
            double now=Time.timeAsDouble;
            if(now>=nextVisibilityCheck)
            {
                nextVisibilityCheck=now+Mathf.Max(.01f,visibilityCheckInterval);
                if(!TargetSelectionHelper.Eligible(Target,player,maximumLockDistance,true)){ClearLock();return;}
                if(CanObserve(Target))
                {
                    if(State==LockState.Occluded)observation.Clear();
                    State=LockState.Locked;occludedSince=0;
                }
                else if(State!=LockState.Occluded){State=LockState.Occluded;occludedSince=now;observation.Clear();}
            }
            if(State==LockState.Occluded)
            {
                if(now-occludedSince>=Mathf.Max(0,lockLossDelay))ClearLock();
                return;
            }
            LastVisiblePosition=Target.WorldPosition;
            observation.Sample(Time.timeAsDouble,observationInterval,()=>Target.WorldPosition,()=>Target.WorldVelocity);
            if(observation.Velocity.sqrMagnitude>.01f)ObservedHeading=observation.Velocity.normalized;
            var w=player.Weapon;Vector3 origin=reference?reference.position:player.WorldPosition+Vector3.up*3;
            Vector3 point=Vector3.zero;float time=0;
            Valid=observation.Valid&&CombatBallistics.Intercept(origin,observation.Estimate(Time.timeAsDouble),observation.Velocity,player.WorldVelocity,w.muzzleSpeed,w.gravity,w.maximumRange,out point,out time,w.horizontalRange);
            if(Valid){PredictedIntercept=point;FlightTime=time;}
        }
    }
}
