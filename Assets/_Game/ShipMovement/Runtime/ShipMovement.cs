using UnityEngine;
namespace GrandNavigation.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ShipMovement : MonoBehaviour
    {
        public MovementSettings settings=new MovementSettings();
        public float TargetSpeed { get; private set; }
        public float RudderAngle { get; private set; }
        public float TurnRate { get; private set; }
        public Vector3 WorldVelocity => body?body.linearVelocity:Vector3.zero;
        public Vector3 WorldPosition => body?body.position:transform.position;
        public Vector3 Forward => body?body.rotation*Vector3.forward:transform.forward;
        public float ActualSpeed => Vector3.Dot(WorldVelocity,Forward);
        public float LateralSpeed => Vector3.Dot(WorldVelocity,body?body.rotation*Vector3.right:transform.right);
        Rigidbody body;float throttleInput,steeringInput;
        void Awake()
        {
            body=GetComponent<Rigidbody>();body.useGravity=false;body.isKinematic=false;
            body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping=0;body.angularDamping=0;body.maxAngularVelocity=1;body.maxDepenetrationVelocity=2;
        }
        // Shared command interface for player input and future AI. No keyboard dependency here.
        public void SetThrottleInput(float value) => throttleInput=Mathf.Clamp(value,-1,1);
        public void SetSteeringInput(float value) => steeringInput=Mathf.Clamp(value,-1,1);
        public void SetTargetSpeed(float value) { TargetSpeed=Mathf.Clamp(value,-settings.maxReverseSpeed,settings.maxForwardSpeed);throttleInput=0; }
        void FixedUpdate()
        {
            float dt=Time.fixedDeltaTime;
            TargetSpeed=ShipMovementMath.AdjustTarget(TargetSpeed,throttleInput,dt,settings);
            RudderAngle=Mathf.MoveTowards(RudderAngle,steeringInput*settings.maxRudderAngle,settings.rudderRate*dt);
            Vector3 forward=body.rotation*Vector3.forward;
            Vector3 current=body.linearVelocity;current.y=0;
            float actual=Vector3.Dot(current,forward);
            float next=ShipMovementMath.StepSpeed(actual,TargetSpeed,dt,settings);
            Vector3 lateral=current-forward*actual;
            Vector3 velocity=forward*next+lateral*Mathf.Exp(-settings.lateralDamping*dt);
            velocity=Vector3.ClampMagnitude(velocity,next<0?settings.maxReverseSpeed:settings.maxForwardSpeed);
            body.linearVelocity=velocity;
            float desired=ShipMovementMath.DesiredTurnRate(actual,RudderAngle,settings);
            TurnRate=Mathf.Abs(actual)<.05f?0:Mathf.MoveTowards(TurnRate,desired,settings.turnAcceleration*dt);
            body.angularVelocity=Vector3.up*(TurnRate*Mathf.Deg2Rad);
        }
        void OnCollisionEnter(Collision collision) => RejectInwardVelocity(collision);
        void OnCollisionStay(Collision collision) => RejectInwardVelocity(collision);
        void RejectInwardVelocity(Collision collision)
        {
            Vector3 velocity=body.linearVelocity;
            for(int i=0;i<collision.contactCount;i++)
            {
                Vector3 normal=collision.GetContact(i).normal;normal.y=0;
                if(normal.sqrMagnitude<.001f)continue;
                normal.Normalize();float inward=Vector3.Dot(velocity,normal);
                if(inward<0)velocity-=normal*inward;
            }
            velocity.y=0;body.linearVelocity=velocity;
        }
    }
}

