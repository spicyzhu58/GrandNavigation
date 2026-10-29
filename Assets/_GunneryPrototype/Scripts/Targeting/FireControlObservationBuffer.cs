using System;
using System.Collections.Generic;
using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public sealed class FireControlObservationBuffer
    {
        struct Packet { public Vector3 position,velocity; public double captured,ready; }
        readonly Queue<Packet> pending=new Queue<Packet>();
        public FireControlTrack Track { get; }=new FireControlTrack();
        double lastCapture=double.NegativeInfinity;
        Vector3 previousReading; bool hasPrevious;
        public int PendingCount => pending.Count;
        public static Vector3 QuantizePosition(Vector3 origin,Vector3 position,float rangeStep,float bearingStep)
        {
            Vector3 d=position-origin; d.y=0;
            float range=d.magnitude, angle=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
            if(BallisticSolver.Finite(rangeStep) && rangeStep>.001f) range=Mathf.Round(range/rangeStep)*rangeStep;
            if(BallisticSolver.Finite(bearingStep) && bearingStep>.001f) angle=Mathf.Round(angle/bearingStep)*bearingStep;
            return new Vector3(origin.x+Mathf.Sin(angle*Mathf.Deg2Rad)*range,0,origin.z+Mathf.Cos(angle*Mathf.Deg2Rad)*range);
        }
        public bool Update(double now,float interval,float latency,bool optical,float rangeStep,float bearingStep,
            Vector3 observer,Func<Vector3> readPosition,Func<Vector3> readVelocity)
        {
            if(!FireControlTrack.FiniteTime(now)) return false;
            interval=BallisticSolver.Finite(interval)?Mathf.Max(.05f,interval):4.5f;
            latency=BallisticSolver.Finite(latency)?Mathf.Clamp(latency,0,10):2;
            if(now-lastCapture>=interval)
            {
                Vector3 p=readPosition();
                if(!BallisticSolver.Finite(p)) return false;
                if(optical) p=QuantizePosition(observer,p,rangeStep,bearingStep);
                // Optical mode never invokes the live-velocity getter.
                Vector3 velocity=optical ? (hasPrevious ? (p-previousReading)/(float)(now-lastCapture) : Vector3.zero) : readVelocity();
                if(!BallisticSolver.Finite(velocity)) return false;
                if(optical) velocity=Vector3.ClampMagnitude(velocity,30);
                if(pending.Count>=64) pending.Dequeue();
                pending.Enqueue(new Packet{position=p,velocity=velocity,captured=now,ready=now+latency});
                previousReading=p; hasPrevious=true; lastCapture=now;
            }
            while(pending.Count>0 && pending.Peek().ready<=now)
            {
                var packet=pending.Dequeue();
                Track.UpdateObservation(packet.captured,.001f,()=>packet.position,()=>packet.velocity);
            }
            return Track.Valid;
        }
    }
}
