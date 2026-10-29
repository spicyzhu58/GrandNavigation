using System;
using UnityEngine;
namespace GrandNavigation.Gunnery
{
    /// <summary>One target's observation history. No target/path knowledge between observations.</summary>
    public sealed class FireControlTrack
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 Heading { get; private set; } = Vector3.forward;
        public double Timestamp { get; private set; }
        public float ObservedTurnDegrees { get; private set; }
        public int ObservationCount { get; private set; }
        public bool Valid { get; private set; }
        public static bool FiniteTime(double t) => !double.IsNaN(t) && !double.IsInfinity(t);
        public float Age(double now) => Valid && FiniteTime(now) ? (float)Math.Max(0,now-Timestamp) : 0;
        public bool UpdateObservation(double now,float interval,Func<Vector3> readPosition,Func<Vector3> readVelocity)
        {
            if(!FiniteTime(now)) return false;
            interval=BallisticSolver.Finite(interval) ? Mathf.Max(.05f,interval) : 2.5f;
            if(Valid && now>=Timestamp && now-Timestamp<interval) return true;
            // The only place callers' live target getters are invoked.
            Vector3 position=readPosition(),velocity=readVelocity();
            if(!BallisticSolver.Finite(position) || !BallisticSolver.Finite(velocity)) { Valid=false; return false; }
            Vector3 flat=new Vector3(velocity.x,0,velocity.z);
            ObservedTurnDegrees=Valid && Velocity.sqrMagnitude>.01f && flat.sqrMagnitude>.01f ?
                Vector3.Angle(new Vector3(Velocity.x,0,Velocity.z),flat) : 0;
            if(flat.sqrMagnitude>.01f) Heading=flat.normalized;
            Position=position; Velocity=velocity; Timestamp=now; Valid=true; ObservationCount++; return true;
        }
        public Vector3 EstimatePosition(double now) => Position+Velocity*Age(now);
        public bool Predict(double now,Vector3 origin,float speed,float gravity,float range,out Vector3 point,out float time)
        {
            point=Vector3.zero; time=0;
            return Valid && FiniteTime(now) &&
                BallisticSolver.TryIntercept(origin,EstimatePosition(now),Velocity,speed,gravity,range,out point,out time);
        }
    }
}