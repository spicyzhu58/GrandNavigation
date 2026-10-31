using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class FireControlObservation
    {
        public Vector3 Position {get;private set;} public Vector3 Velocity {get;private set;}
        public double Timestamp {get;private set;} public bool Valid {get;private set;}
        public void Clear(){Valid=false;Position=Velocity=Vector3.zero;Timestamp=0;}
        public bool Sample(double now,float interval,System.Func<Vector3> position,System.Func<Vector3> velocity)
        {
            if(double.IsNaN(now)||double.IsInfinity(now))return false;
            if(Valid&&now>=Timestamp&&now-Timestamp<Mathf.Max(.05f,interval))return false;
            var p=position();var v=velocity();if(!CombatBallistics.Finite(p)||!CombatBallistics.Finite(v)){Clear();return false;}
            Position=p;Velocity=v;Timestamp=now;Valid=true;return true;
        }
        public Vector3 Estimate(double now)=>Position+Velocity*(float)System.Math.Max(0,now-Timestamp);
    }
}
