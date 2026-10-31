using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class CombatTurret : MonoBehaviour
    {
        public Transform[] barrels;public Transform[] muzzles;
        [Header("Local traverse / rear turrets use 180 degrees")]
        [Range(-180,180)] public float neutralYaw;
        [Range(1,359)] public float traverseArc=260;
        [Min(1)] public float traverseSpeed=28,elevationSpeed=22;
        [Range(.1f,5)] public float aimTolerance=.7f;
        [Range(-15,0)] public float minimumElevation=-5;
        [Range(1,80)] public float maximumElevation=60;
        [Min(.1f)] public float reloadSeconds=5;
        public float ReloadRemaining=>Mathf.Max(0,readyAt-Time.time);
        public bool Aligned {get;private set;} public bool SolutionValid {get;private set;}
        public float LocalYaw=>neutralYaw+traverse;
        float traverse,readyAt;Weapon weapon;
        public void Configure(Weapon owner){weapon=owner;traverse=0;transform.localRotation=Quaternion.Euler(0,neutralYaw,0);}
        void Update(){if(weapon&&weapon.Owner&&weapon.Owner.IsAlive&&weapon.HasAim)Track(Time.deltaTime);}
        public bool Safe(Vector3 direction)
        {
            Vector3 local=transform.parent.InverseTransformDirection(direction);
            return GunneryMath.InArc(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,neutralYaw,traverseArc);
        }
        public void Track(float dt)
        {
            if(!weapon||muzzles==null||muzzles.Length==0)return;
            SolutionValid=weapon.Solve(muzzles[0].position,weapon.AimPoint,out var velocity,out _);
            Vector3 direction=SolutionValid?velocity:weapon.AimPoint-transform.position;
            Vector3 local=transform.parent.InverseTransformDirection(direction);
            float desired=Mathf.DeltaAngle(neutralYaw,Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg);
            traverse=Mathf.MoveTowards(traverse,Mathf.Clamp(desired,-traverseArc*.5f,traverseArc*.5f),traverseSpeed*dt);
            transform.localRotation=Quaternion.Euler(0,neutralYaw+traverse,0);
            foreach(var barrel in barrels)
            {
                float elevation=SolutionValid?Mathf.Atan2(velocity.y,new Vector2(velocity.x,velocity.z).magnitude)*Mathf.Rad2Deg:0;
                float current=-Mathf.DeltaAngle(0,barrel.localEulerAngles.x);
                barrel.localRotation=Quaternion.Euler(-Mathf.MoveTowards(current,Mathf.Clamp(elevation,minimumElevation,maximumElevation),elevationSpeed*dt),0,0);
            }
            Aligned=SolutionValid&&Safe(direction)&&Vector3.Angle(muzzles[0].forward,direction)<=aimTolerance;
        }
        // Unscattered sea-plane impact of the actual barrel, including platform velocity.
        public static bool SeaImpact(Vector3 origin,Vector3 worldVelocity,float gravity,out Vector3 point)
        {
            point=Vector3.zero;
            if(!CombatBallistics.Finite(origin)||!CombatBallistics.Finite(worldVelocity)||!float.IsFinite(gravity)||gravity<=0||origin.y<0)return false;
            float discriminant=worldVelocity.y*worldVelocity.y+2*gravity*origin.y;
            float time=(worldVelocity.y+Mathf.Sqrt(discriminant))/gravity;
            if(!float.IsFinite(time)||time<=0)return false;
            point=CombatBallistics.Position(origin,worldVelocity,gravity,time);point.y=0;
            return CombatBallistics.Finite(point);
        }
        public bool TryGetImpact(int muzzleIndex,out Vector3 point)
        {
            point=Vector3.zero;
            if(!weapon||!weapon.Owner||!weapon.Owner.IsAlive||muzzles==null||muzzleIndex>=muzzles.Length||muzzleIndex<0||!muzzles[muzzleIndex])return false;
            var muzzle=muzzles[muzzleIndex];
            return Safe(muzzle.forward)&&SeaImpact(muzzle.position,muzzle.forward*weapon.muzzleSpeed+weapon.Owner.WorldVelocity,weapon.gravity,out point);
        }
        public int Fire(Vector3 centre)
        {
            if(!weapon||ReloadRemaining>0)return 0;int count=0;
            foreach(var muzzle in muzzles)if(!muzzle||!Safe(muzzle.forward))return 0;
            // Recompute readiness without moving the turret when the command arrives.
            Track(0);
            for(int i=0;i<Mathf.Min(2,muzzles.Length);i++)
            {
                var muzzle=muzzles[i];var barrel=barrels[i];if(!Safe(muzzle.forward))continue;
                float range=Vector3.Distance(muzzle.position,centre);Vector2 scatter=weapon.Scatter(range);
                var before=barrel.rotation;
                if(Aligned)
                {
                    Vector3 destination=centre+new Vector3(scatter.x,0,scatter.y);
                    bool valid=true;
                    // Muzzle rotates around the barrel pivot: solve again after adjusting its pose.
                    for(int j=0;j<6;j++)
                    {
                        if(!weapon.Solve(muzzle.position,destination,out var relative,out _)){valid=false;break;}
                        float elevation=Mathf.Asin(Mathf.Clamp(relative.normalized.y,-1,1))*Mathf.Rad2Deg;
                        if(!Safe(relative)||elevation<minimumElevation||elevation>maximumElevation){valid=false;break;}
                        barrel.rotation=Quaternion.LookRotation(relative,Vector3.up);
                    }
                    if(!valid){barrel.rotation=before;continue;}
                }
                else
                {
                    float yaw=Mathf.Atan2(scatter.x,Mathf.Max(10,range))*Mathf.Rad2Deg;
                    float pitch=Mathf.Atan2(scatter.y,Mathf.Max(10,range))*Mathf.Rad2Deg;
                    barrel.rotation=before*Quaternion.Euler(-pitch,yaw,0);
                }
                if(!Safe(muzzle.forward)){barrel.rotation=before;continue;}
                weapon.Spawn(muzzle);count++;
            }
            if(count>0)readyAt=Time.time+reloadSeconds;
            return count;
        }
        void OnValidate(){traverseArc=Mathf.Clamp(traverseArc,1,359);reloadSeconds=Mathf.Max(.1f,reloadSeconds);traverseSpeed=Mathf.Max(1,traverseSpeed);}
    }
}
