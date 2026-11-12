using UnityEngine;
using GrandNavigation.Gameplay;
namespace GrandNavigation.Battle2D
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ShipMovement2D : MonoBehaviour
    {
        public MovementSettings settings=new MovementSettings {maxForwardSpeed=15,maxReverseSpeed=4,forwardAcceleration=3,brakingDeceleration=4,reverseAcceleration=2,maxTurnRate=25,fullTurnSpeed=10};
        public float TargetSpeed {get;private set;}
        public float Rudder {get;private set;}
        public float Speed=>Vector2.Dot(Body.linearVelocity,transform.up);
        public Rigidbody2D Body {get;private set;}
        float throttle,steering,turn;
        void Awake(){Body=GetComponent<Rigidbody2D>();Body.gravityScale=0;Body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;Body.interpolation=RigidbodyInterpolation2D.Interpolate;}
        public void Input(float acceleration,float rudder){throttle=Mathf.Clamp(acceleration,-1,1);steering=Mathf.Clamp(rudder,-1,1);}
        public void Drive(float speed,float rudder){TargetSpeed=Mathf.Clamp(speed,-settings.maxReverseSpeed,settings.maxForwardSpeed);Input(0,rudder);}
        public void Stop(){TargetSpeed=throttle=steering=turn=0;Body.linearVelocity=Vector2.zero;Body.angularVelocity=0;}
        void FixedUpdate()
        {
            float dt=Time.fixedDeltaTime;
            TargetSpeed=ShipMovementMath.AdjustTarget(TargetSpeed,throttle,dt,settings);
            Rudder=Mathf.MoveTowards(Rudder,steering*settings.maxRudderAngle,settings.rudderRate*dt);
            turn=Mathf.MoveTowards(turn,ShipMovementMath.DesiredTurnRate(Speed,Rudder,settings),settings.turnAcceleration*dt);
            Vector2 forward=transform.up;Vector2 lateral=Body.linearVelocity-forward*Speed;
            Body.linearVelocity=forward*ShipMovementMath.StepSpeed(Speed,TargetSpeed,dt,settings)+lateral*Mathf.Exp(-settings.lateralDamping*dt);
            Body.angularVelocity=-turn;
        }
    }
}
