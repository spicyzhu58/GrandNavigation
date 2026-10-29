using UnityEngine;
namespace GrandNavigation.Gunnery
{
    [RequireComponent(typeof(Health),typeof(Rigidbody))]
    public class TargetMotion : MonoBehaviour
    {
        public enum MotionMode { Stationary, Lateral, Diagonal, Maneuvering, PatrolLinear, VariableSpeed, VariableManeuver }
        public enum TurnPattern { Sequence, Alternating, LeftOnly, RightOnly }
        public MotionMode mode=MotionMode.Lateral;
        [Min(0)] public float speed=8;
        public Vector3 spawnPosition=new Vector3(0,0,160);
        public float boundary=185;
        [Header("Repeatable maneuver test")]
        [Min(0)] public float maneuverSpeed=10;
        [Min(.05f)] public float straightDuration=5;
        [Min(.05f)] public float turnDuration=1.5f;
        [Range(0,180)] public float turnAngle=15;
        public TurnPattern turnPattern=TurnPattern.Sequence;
        public float[] turnSequence={-15,20};
        [Header("Combat training profiles")]
        public string callsign="Target";
        public Vector2 patrolX=new Vector2(-95,95), patrolZ=new Vector2(100,220);
        [Min(.1f)] public float acceleration=2.5f, speedStageDuration=4.2f;
        public float[] speedSequence={5,14,7,12};
        [Min(0)] public float speedPhaseOffset=0;
        [Range(0,360)] public float spawnHeading=90;
        public float CurrentSpeed { get; private set; }
        float throttleTime;
        public bool IsTurning { get; private set; }
        public int SequenceIndex { get; private set; }
        Rigidbody body; Health health; bool boundaryStopped;
        MotionMode previousMode;
        float heading=90, phaseTime, activeTurn;
        Vector3 maneuverVelocity;
        float Safe(float v,float fallback) => BallisticSolver.Finite(v)?v:fallback;
        public Vector3 Velocity => !health || !health.IsAlive || boundaryStopped ? Vector3.zero :
            mode==MotionMode.Stationary ? Vector3.zero :
            (mode==MotionMode.Maneuvering || IsTrainingProfile) ? maneuverVelocity :
            (mode==MotionMode.Lateral ? Vector3.right : new Vector3(1,0,-.5f).normalized)*Mathf.Max(0,Safe(speed,0));
        public bool IsTrainingProfile => mode==MotionMode.PatrolLinear || mode==MotionMode.VariableSpeed || mode==MotionMode.VariableManeuver;
        void Awake()
        {
            body=GetComponent<Rigidbody>(); health=GetComponent<Health>(); body.isKinematic=true; body.useGravity=false;
            ResetManeuver();
        }
        void ResetManeuver()
        {
            heading=transform.eulerAngles.y; phaseTime=0; SequenceIndex=0; IsTurning=false;
            previousMode=mode; throttleTime=0; CurrentSpeed=Mathf.Max(0,Safe(mode==MotionMode.PatrolLinear?speed:maneuverSpeed,10));
            maneuverVelocity=Quaternion.Euler(0,heading,0)*Vector3.forward*CurrentSpeed;
        }
        float NextTurn()
        {
            if(turnPattern==TurnPattern.Sequence && turnSequence!=null && turnSequence.Length>0)
                return Mathf.Clamp(Safe(turnSequence[SequenceIndex%turnSequence.Length],0),-180,180);
            float angle=Mathf.Clamp(Safe(turnAngle,15),0,180);
            return turnPattern==TurnPattern.LeftOnly ? -angle :
                turnPattern==TurnPattern.RightOnly ? angle : (SequenceIndex%2==0 ? -angle : angle);
        }
        void AdvanceHeading(float dt)
        {
            float remaining=dt;
            for(int guard=0;remaining>.000001f && guard<32;guard++)
            {
                float duration=Mathf.Max(.05f,Safe(IsTurning?turnDuration:straightDuration,IsTurning?1.5f:5));
                float step=Mathf.Min(remaining,Mathf.Max(0,duration-phaseTime));
                if(IsTurning) heading+=activeTurn/duration*step;
                phaseTime+=step; remaining-=step;
                if(phaseTime>=duration-.000001f)
                {
                    phaseTime=0;
                    if(IsTurning) { IsTurning=false; SequenceIndex++; }
                    else { IsTurning=true; activeTurn=NextTurn(); }
                }
            }
        }
        void FixedUpdate()
        {
            if(!health.IsAlive)
            {
                foreach(var c in GetComponentsInChildren<Collider>()) c.enabled=false;
                body.MovePosition(new Vector3(body.position.x,Mathf.MoveTowards(body.position.y,-8,Time.fixedDeltaTime*1.7f),body.position.z));
                maneuverVelocity=Vector3.zero; return;
            }
            if(previousMode!=mode) { boundaryStopped=false; ResetManeuver(); }
            float dt=Time.fixedDeltaTime;
            if(IsTrainingProfile)
            {
                if(mode==MotionMode.VariableManeuver) AdvanceHeading(dt);
                float wanted=Mathf.Max(0,Safe(speed,8));
                if(mode!=MotionMode.PatrolLinear)
                {
                    float stage=Mathf.Max(.1f,Safe(speedStageDuration,4.2f));
                    int index=Mathf.FloorToInt((throttleTime+Mathf.Max(0,speedPhaseOffset))/stage);
                    wanted=speedSequence!=null && speedSequence.Length>0 ? Mathf.Clamp(Safe(speedSequence[index%speedSequence.Length],8),0,20) : 8;
                }
                CurrentSpeed=Mathf.MoveTowards(CurrentSpeed,wanted,Mathf.Max(.1f,Safe(acceleration,2.5f))*dt);
                throttleTime+=dt;
                Vector3 direction=Quaternion.Euler(0,heading,0)*Vector3.forward;
                Vector3 old=body.position,next=old+direction*CurrentSpeed*dt;
                float xmin=Mathf.Min(patrolX.x,patrolX.y),xmax=Mathf.Max(patrolX.x,patrolX.y);
                float zmin=Mathf.Min(patrolZ.x,patrolZ.y),zmax=Mathf.Max(patrolZ.x,patrolZ.y);
                bool reflected=false;
                if(next.x>xmax) { next.x=2*xmax-next.x; direction.x=-direction.x; reflected=true; }
                else if(next.x<xmin) { next.x=2*xmin-next.x; direction.x=-direction.x; reflected=true; }
                if(next.z>zmax) { next.z=2*zmax-next.z; direction.z=-direction.z; reflected=true; }
                else if(next.z<zmin) { next.z=2*zmin-next.z; direction.z=-direction.z; reflected=true; }
                if(reflected) heading=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                maneuverVelocity=(next-old)/dt;
                body.MovePosition(next); body.MoveRotation(Quaternion.Euler(0,heading,0)); return;
            }
            if(mode==MotionMode.Maneuvering)
            {
                AdvanceHeading(dt);
                Vector3 direction=Quaternion.Euler(0,heading,0)*Vector3.forward;
                Vector3 old=body.position, next=old+direction*Mathf.Max(0,Safe(maneuverSpeed,10))*dt;
                float limit=Mathf.Clamp(Safe(boundary,185),20,190);
                float zMin=Mathf.Max(-115,-limit),zMax=Mathf.Min(255,limit);
                bool reflected=false;
                if(next.x>limit) { next.x=2*limit-next.x; direction.x=-direction.x; reflected=true; }
                else if(next.x< -limit) { next.x=-2*limit-next.x; direction.x=-direction.x; reflected=true; }
                if(next.z>zMax) { next.z=2*zMax-next.z; direction.z=-direction.z; reflected=true; }
                else if(next.z<zMin) { next.z=2*zMin-next.z; direction.z=-direction.z; reflected=true; }
                if(reflected) heading=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                maneuverVelocity=(next-old)/dt; // Report actual world displacement, including a boundary step.
                body.MovePosition(next); body.MoveRotation(Quaternion.Euler(0,heading,0)); return;
            }
            Vector3 velocity=Velocity;
            Vector3 destination=body.position+velocity*dt;
            if(Mathf.Abs(destination.x)>boundary || Mathf.Abs(destination.z)>boundary) { boundaryStopped=true; return; }
            body.MovePosition(destination);
            if(velocity.sqrMagnitude>.01f) body.MoveRotation(Quaternion.LookRotation(velocity));
        }
        public void ResetTarget()
        {
            if(!body) body=GetComponent<Rigidbody>();
            if(!health) health=GetComponent<Health>();
            boundaryStopped=false; body.position=spawnPosition; body.rotation=Quaternion.Euler(0,spawnHeading,0);
            transform.SetPositionAndRotation(spawnPosition,body.rotation); ResetManeuver();
            foreach(var c in GetComponentsInChildren<Collider>()) c.enabled=true;
            health.Restore(); Physics.SyncTransforms();
        }
        public void SetMode(MotionMode value,float valueSpeed)
        {
            mode=value; speed=valueSpeed;
            if(mode==MotionMode.Maneuvering) maneuverSpeed=valueSpeed;
            boundaryStopped=false; ResetManeuver();
        }
    }
}
