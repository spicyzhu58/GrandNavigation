using UnityEngine;
namespace GrandNavigation.Gunnery
{
    [RequireComponent(typeof(Health),typeof(Rigidbody))]
    public class TargetMotion : MonoBehaviour
    {
        public enum MotionMode { Stationary, Lateral, Diagonal }
        public MotionMode mode=MotionMode.Lateral;
        [Min(0)] public float speed=8;
        public Vector3 spawnPosition=new Vector3(0,0,160);
        public float boundary=185;
        Rigidbody body; Health health; bool boundaryStopped;
        public Vector3 Velocity => !health || !health.IsAlive || boundaryStopped ? Vector3.zero :
            mode==MotionMode.Stationary ? Vector3.zero :
            (mode==MotionMode.Lateral ? Vector3.right : new Vector3(1,0,-.5f).normalized)*Mathf.Max(0,speed);
        void Awake() { body=GetComponent<Rigidbody>(); health=GetComponent<Health>(); body.isKinematic=true; body.useGravity=false; }
        void FixedUpdate()
        {
            if(!health.IsAlive)
            {
                foreach(var c in GetComponentsInChildren<Collider>()) c.enabled=false;
                body.MovePosition(new Vector3(body.position.x,Mathf.MoveTowards(body.position.y,-8,Time.fixedDeltaTime*1.7f),body.position.z));
                return;
            }
            Vector3 velocity=Velocity;
            Vector3 next=body.position+velocity*Time.fixedDeltaTime;
            if(Mathf.Abs(next.x)>boundary || Mathf.Abs(next.z)>boundary) { boundaryStopped=true; return; }
            body.MovePosition(next);
            if(velocity.sqrMagnitude>.01f) body.MoveRotation(Quaternion.LookRotation(velocity));
        }
        public void ResetTarget()
        {
            if(!body) body=GetComponent<Rigidbody>();
            if(!health) health=GetComponent<Health>();
            boundaryStopped=false; body.position=spawnPosition; body.rotation=Quaternion.Euler(0,90,0);
            transform.SetPositionAndRotation(spawnPosition,body.rotation);
            foreach(var c in GetComponentsInChildren<Collider>()) c.enabled=true;
            health.Restore(); Physics.SyncTransforms();
        }
        public void SetMode(MotionMode value,float valueSpeed) { mode=value; speed=valueSpeed; boundaryStopped=false; }
    }
}