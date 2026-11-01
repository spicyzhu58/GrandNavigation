using UnityEngine;
namespace GrandNavigation.Gameplay
{
    [DefaultExecutionOrder(-20)]
    public sealed class CombatFireControl : MonoBehaviour
    {
        public PlayerShip player;public Transform reference;
        [Min(.05f)] public float observationInterval=.5f;
        public Ship Target {get;private set;}
        public bool Valid {get;private set;}
        public Vector3 PredictedIntercept {get;private set;}
        public Vector3 ObservedHeading {get;private set;}=Vector3.forward;
        public float FlightTime {get;private set;}
        public float SampleAge=>(float)System.Math.Max(0,Time.timeAsDouble-observation.Timestamp);
        public readonly FireControlObservation observation=new FireControlObservation();
        public void Lock(Ship target)
        {
            if(target==Target)return;
            Target=target&&target.IsAlive&&target.team!=player.team?target:null;observation.Clear();Valid=false;
            if(Target)ObservedHeading=Target.WorldForward;
            Refresh();
        }
        void Update()=>Refresh();
        public void Refresh()
        {
            Valid=false;
            if(!player||!player.IsAlive||!Target||!Target.IsAlive||!Target.isActiveAndEnabled){Target=null;observation.Clear();return;}
            observation.Sample(Time.timeAsDouble,observationInterval,()=>Target.WorldPosition,()=>Target.WorldVelocity);
            if(observation.Velocity.sqrMagnitude>.01f)ObservedHeading=observation.Velocity.normalized;
            var w=player.Weapon;Vector3 origin=reference?reference.position:player.WorldPosition+Vector3.up*3;
            Vector3 point=Vector3.zero;float time=0;
            Valid=observation.Valid&&CombatBallistics.Intercept(origin,observation.Estimate(Time.timeAsDouble),observation.Velocity,player.WorldVelocity,w.muzzleSpeed,w.gravity,w.maximumRange,out point,out time,w.horizontalRange);
            if(Valid){PredictedIntercept=point;FlightTime=time;}
        }
    }
}
