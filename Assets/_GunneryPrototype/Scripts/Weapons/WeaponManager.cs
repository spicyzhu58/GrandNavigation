using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class WeaponManager : MonoBehaviour
    {
        public PrototypeSession session;
        public Transform shooter;
        public TurretController[] turrets;
        public ProjectileController projectilePrefab;
        public bool dispersionEnabled=true;
        [Min(0)] public float dispersionRadius=3;
        public Vector3 LastSalvoCenter { get; private set; }
        public int LastSalvoCount { get; private set; }
        public string Status { get; private set; }="Enter aim view to fire";
        void OnEnable()
        {
            if(session) { session.FireRequested+=FireSalvo; session.ResetRequested+=ResetWeapons; }
        }
        void OnDisable()
        {
            if(session) { session.FireRequested-=FireSalvo; session.ResetRequested-=ResetWeapons; }
        }
        public void ResetWeapons()
        {
            foreach(var turret in turrets) if(turret) turret.ResetReload();
            LastSalvoCount=0; Status="Weapons ready";
        }
        public void FireSalvo() => TryFireSalvo();
        public int TryFireSalvo()
        {
            LastSalvoCount=0;
            if(!session.aim.IsAiming || !session.selection.HasTarget || !session.aim.HasBallisticSolution() || !projectilePrefab)
            { Status="CANNOT FIRE: lock target and choose a valid impact point"; return 0; }
            LastSalvoCenter=session.aim.PlayerAimPoint;
            int batteries=Mathf.Min(3,turrets.Length);
            for(int i=0;i<batteries;i++)
            {
                var turret=turrets[i]; if(!turret) continue;
                turret.Track(0); // Re-check readiness against the click's current world point.
                if(!turret.CanFire) continue;
                int fired=0;
                foreach(var muzzle in turret.muzzles)
                {
                    if(fired>=2 || LastSalvoCount>=6 || !muzzle) break;
                    Vector2 scatter=dispersionEnabled ? Random.insideUnitCircle*Mathf.Max(0,dispersionRadius) : Vector2.zero;
                    Vector3 impact=LastSalvoCenter+new Vector3(scatter.x,0,scatter.y);
                    Vector3 horizontal=impact-muzzle.position; horizontal.y=0;
                    if(horizontal.magnitude>session.prediction.maxRange ||
                        !BallisticSolver.TrySolve(muzzle.position,impact,session.prediction.muzzleSpeed,session.prediction.gravity,out var velocity,out _)) continue;
                    var shell=Instantiate(projectilePrefab,muzzle.position,Quaternion.LookRotation(velocity));
                    if(!shell.Launch(muzzle.position,velocity,session.prediction.gravity,shooter))
                    { Destroy(shell.gameObject); continue; }
                    FeedbackEffects.Muzzle(muzzle.position); fired++; LastSalvoCount++;
                }
                if(fired>0) turret.MarkFired();
            }
            if(LastSalvoCount>0)
            {
                session.cameraRig.Shake(.3f); CombatAudio.PlayShot();
                Status="SALVO: "+LastSalvoCount+" / 6 shells";
            }
            else Status="CANNOT FIRE: turrets traversing / reloading / no solution";
            return LastSalvoCount;
        }
    }
}