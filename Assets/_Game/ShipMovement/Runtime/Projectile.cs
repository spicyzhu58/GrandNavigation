using UnityEngine;
namespace GrandNavigation.Gameplay
{
    // Reuses the prototype's analytic stepping and swept sphere collision model.
    public class Projectile : MonoBehaviour
    {
        [Min(.01f)] public float radius=.18f;
        [Min(1)] public float lifetime=15;
        [Min(0),Tooltip("Optional absolute X/Z cleanup limit; zero disables it for old prototypes.")] public float worldBounds;
        public bool Activated {get;private set;} public bool Resolved {get;private set;}
        public Vector3 InitialVelocity {get;private set;} public Vector3 Origin {get;private set;}
        public Vector3 ImpactPoint {get;private set;}
        public static int SpawnedCount {get;private set;}
        public static int DamageCount {get;private set;}
        public static int SplashCount {get;private set;}
        Ship owner;int damage;float gravity,age;bool friendlyFire;
        public virtual void Launch()=>Activated=true;
        public void Launch(Vector3 origin,Vector3 worldVelocity,float g,Ship source,int amount,bool allies)
        {
            if(!CombatBallistics.Finite(origin)||!CombatBallistics.Finite(worldVelocity)||!CombatBallistics.Finite(g)||g<=0){Destroy(gameObject);return;}
            Origin=origin;InitialVelocity=worldVelocity;gravity=g;owner=source;damage=amount;friendlyFire=allies;
            transform.position=origin;Activated=true;SpawnedCount++;
        }
        public static Vector3 ComposeWorldVelocity(Vector3 relative,Vector3 platform)=>relative+platform;
        public void ResolveHit(IDamageable target,int amount){if(Resolved)return;Resolved=true;if(target is Ship ship)ship.TakeDamage(amount,owner);else target?.TakeDamage(amount);}
        void FixedUpdate()
        {
            if(!Activated||Resolved||gravity<=0)return;
            float next=age+Time.fixedDeltaTime;
            var from=CombatBallistics.Position(Origin,InitialVelocity,gravity,age);
            var to=CombatBallistics.Position(Origin,InitialVelocity,gravity,next);
            bool water=to.y<=0;
            if(water){next=(InitialVelocity.y+Mathf.Sqrt(InitialVelocity.y*InitialVelocity.y+2*gravity*Mathf.Max(0,Origin.y)))/gravity;to=CombatBallistics.Position(Origin,InitialVelocity,gravity,next);to.y=0;}
            Vector3 delta=to-from;RaycastHit? nearest=null;
            if(delta.sqrMagnitude>.000001f)
                foreach(var hit in Physics.SphereCastAll(from,radius,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                {
                    if(owner&&(hit.transform==owner.transform||hit.transform.IsChildOf(owner.transform)))continue;
                    if(hit.transform==transform||hit.transform.IsChildOf(transform))continue;
                    if(!nearest.HasValue||hit.distance<nearest.Value.distance)nearest=hit;
                }
            if(nearest.HasValue)
            {
                var hit=nearest.Value;var ship=hit.collider.GetComponentInParent<Ship>();ImpactPoint=hit.point;
                if(ship&&ship.IsAlive&&ship!=owner&&(friendlyFire||!owner||ship.team!=owner.team))
                {ResolveHit(ship,damage);DamageCount++;CombatEffects.Hit(hit.point,damage);}
                else {Resolved=true;CombatEffects.Flash(hit.point,Color.yellow,1,.25f);}
                Destroy(gameObject);return;
            }
            transform.position=to;age=next;
            var velocity=InitialVelocity+Vector3.down*gravity*age;if(velocity.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(velocity);
            if(water){Resolved=true;ImpactPoint=to;SplashCount++;CombatEffects.Splash(to);Destroy(gameObject);}
            else if(age>lifetime||(worldBounds>0&&(Mathf.Abs(to.x)>worldBounds||Mathf.Abs(to.z)>worldBounds))){Resolved=true;Destroy(gameObject);}
        }
    }
}
