using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public class Weapon : MonoBehaviour
    {
        public Projectile projectile;
        public CombatTurret[] turrets=new CombatTurret[0];
        [Header("Fixed relative muzzle speed / world gravity")]
        [Min(1)] public float muzzleSpeed=65;
        [Min(.1f)] public float gravity=9.81f;
        [Min(1)] public float maximumRange=300;
        [Min(1)] public int damage=25;
        public bool friendlyFire;
        [Tooltip("Use horizontal distance for effective range; enabled by the large-map scene only.")] public bool horizontalRange;
        [Header("Gaussian sea-plane dispersion, independent of fire control")]
        public bool dispersionEnabled=true;
        [Min(0)] public float baseSigma=1,sigmaPer100m=.8f;
        public Ship Owner {get;private set;}
        public bool HasAim {get;private set;} public Vector3 AimPoint {get;private set;}
        public Vector3 LastSalvoCentre {get;private set;}
        public int LastSalvoCount {get;private set;}
        public string Status {get;private set;}="READY";
        readonly System.Random random=new System.Random();
        void Awake()=>Owner=GetComponent<Ship>();
        void Start(){foreach(var turret in turrets)if(turret)turret.Configure(this);}
        public void SetAim(Vector3 point){if(!CombatBallistics.Finite(point))return;AimPoint=new Vector3(point.x,0,point.z);HasAim=true;}
        public bool Solve(Vector3 origin,Vector3 point,out Vector3 relative,out float time)
        {
            relative=Vector3.zero;time=0;
            return Owner&&InRange(origin,point)&&CombatBallistics.Solve(origin,point,Owner.WorldVelocity,muzzleSpeed,gravity,out relative,out time);
        }
        public bool InRange(Vector3 origin,Vector3 point)=> (horizontalRange?Vector3.ProjectOnPlane(point-origin,Vector3.up).magnitude:Vector3.Distance(origin,point))<=maximumRange;
        public Vector2 Scatter(float distance)=>dispersionEnabled?GunneryMath.Gaussian(random,Mathf.Max(0,baseSigma)+Mathf.Max(0,distance)*Mathf.Max(0,sigmaPer100m)/100):Vector2.zero;
        public void ClearAim() => HasAim = false;
        // AI supplies an extra per-turret gate; the player immediate-fire path is unchanged.
        public bool TryFireAimed(System.Predicate<CombatTurret> ready)
        {
            LastSalvoCount=0;
            if(!enabled||!Owner||!Owner.IsAlive||!projectile||!HasAim||ready==null)return false;
            LastSalvoCentre=AimPoint;
            for(int i=0;i<Mathf.Min(3,turrets.Length);i++)
                if(turrets[i] && ready(turrets[i]))LastSalvoCount+=turrets[i].Fire(LastSalvoCentre);
            return LastSalvoCount>0;
        }
        public virtual bool TryFire()
        {
            LastSalvoCount=0;
            if(!enabled||!Owner||!Owner.IsAlive||!projectile||!HasAim){Status="CANNOT FIRE: no weapon / invalid aim";return false;}
            LastSalvoCentre=AimPoint;
            for(int i=0;i<Mathf.Min(3,turrets.Length);i++)if(turrets[i])LastSalvoCount+=turrets[i].Fire(LastSalvoCentre);
            Status=LastSalvoCount>0?$"SALVO {LastSalvoCount}/6 (actual barrel directions)":"CANNOT FIRE: reloading / unsafe barrels / invalid solution";
            return LastSalvoCount>0;
        }
        public void Spawn(Transform muzzle)
        {
            var shell=Instantiate(projectile,muzzle.position,muzzle.rotation);
            shell.Launch(muzzle.position,Projectile.ComposeWorldVelocity(muzzle.forward*muzzleSpeed,Owner.WorldVelocity),gravity,Owner,damage,friendlyFire);
            CombatEffects.Flash(muzzle.position,new Color(1,.65f,.12f),.7f,.12f);
        }
    }
}
