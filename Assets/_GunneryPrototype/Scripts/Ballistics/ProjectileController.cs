using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class ProjectileController : MonoBehaviour
    {
        public float radius = .18f, damage = 40, maxLifetime = 12, worldLimit = 500;
        [Min(.1f)] public float splashLifetime=1.5f;
        public static int SpawnedCount { get; private set; }
        public static int HitCount { get; private set; }
        public static int SplashCount { get; private set; }
        public static Vector3 LastImpact { get; private set; }
        public Vector3 InitialVelocity { get; private set; }
        public Vector3 Origin { get; private set; }
        Transform owner; float gravity, age; bool initialized, resolved;
        public bool Resolved => resolved;
        public static void ResetStatistics() { SpawnedCount=HitCount=SplashCount=0; LastImpact=Vector3.zero; }
        public bool Launch(Vector3 origin, Vector3 velocity, float g, Transform shooter)
        {
            if (!BallisticSolver.Finite(origin) || !BallisticSolver.Finite(velocity) || g <= 0 || !BallisticSolver.Finite(g)) return false;
            Origin=origin; InitialVelocity=velocity; gravity=g; owner=shooter;
            transform.position=origin; initialized=true; SpawnedCount++;
            return true;
        }
        void FixedUpdate()
        {
            if (!initialized || resolved) return;
            float nextTime=age+Time.fixedDeltaTime;
            Vector3 from=BallisticSolver.Position(Origin,InitialVelocity,gravity,age);
            Vector3 to=BallisticSolver.Position(Origin,InitialVelocity,gravity,nextTime);
            bool water=to.y <= 0;
            if (water)
            {
                float waterTime=(InitialVelocity.y + Mathf.Sqrt(InitialVelocity.y*InitialVelocity.y+2*gravity*Mathf.Max(Origin.y,0)))/gravity;
                to=BallisticSolver.Position(Origin,InitialVelocity,gravity,waterTime); to.y=0;
            }
            Vector3 delta=to-from;
            RaycastHit? nearest=null;
            if(delta.sqrMagnitude > .000001f)
                foreach(var hit in Physics.SphereCastAll(from,radius,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                {
                    if(owner && (hit.transform==owner || hit.transform.IsChildOf(owner))) continue;
                    if(hit.transform==transform || hit.transform.IsChildOf(transform)) continue;
                    if(!nearest.HasValue || hit.distance < nearest.Value.distance) nearest=hit;
                }
            if(nearest.HasValue)
            {
                var hit=nearest.Value; resolved=true; LastImpact=hit.point;
                var hp=hit.collider.GetComponentInParent<Health>();
                if(hp && hp.IsAlive) { hp.Damage(damage,hit.point); HitCount++; }
                else FeedbackEffects.Burst(hit.point,false);
                Destroy(gameObject); return;
            }
            transform.position=to;
            Vector3 tangent=InitialVelocity+Vector3.down*gravity*nextTime;
            if(tangent.sqrMagnitude>.001f) transform.rotation=Quaternion.LookRotation(tangent);
            age=nextTime;
            if(water) { resolved=true; SplashCount++; LastImpact=to; FeedbackEffects.Burst(to,true,splashLifetime); Destroy(gameObject); }
            else if(age>maxLifetime || new Vector2(to.x,to.z).magnitude>worldLimit) { resolved=true; Destroy(gameObject); }
        }
    }
}