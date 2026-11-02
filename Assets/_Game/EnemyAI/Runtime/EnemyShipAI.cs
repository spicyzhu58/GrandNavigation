using UnityEngine;
namespace GrandNavigation.Gameplay.AI
{
    [DisallowMultipleComponent,RequireComponent(typeof(EnemyShip),typeof(EnemyNavigation))]
    public sealed class EnemyShipAI : MonoBehaviour
    {
        public EnemyAIProfile profile;
        public Transform[] waypoints=new Transform[0];
        [SerializeField] LayerMask sightMask=~((1<<2)|(1<<4)|(1<<5));
        [SerializeField] bool drawGizmos=true;
        public EnemyState State {get;private set;}
        public Ship Target {get;private set;}
        public Ship RetreatAlly {get;private set;}
        public bool Visible {get;private set;}
        public bool CanFire {get;private set;}
        public int Broadside {get;private set;}=1;
        public string TransitionReason {get;private set;}="Spawn";
        public string FireStatus {get;private set;}="Not engaging";
        public Vector3 LastKnownPosition {get;private set;}
        public Vector3 SalvoError {get;private set;}
        public float StateAge=>Time.time-enteredAt;
        public float MemoryAge=>Time.time-confirmedAt;
        public Ship Ship=>ship;
        public EnemyNavigation Navigation=>navigation;
        Ship ship; PlayerShip player; EnemyNavigation navigation;
        float enteredAt,nextDecision,confirmedAt,visibleSince,blockedSideSince=-1;
        bool initialized,wasVisible,hasThreat;int waypoint;
        readonly System.Random random=new System.Random();
        void Start()
        {
            ship=GetComponent<Ship>();navigation=GetComponent<EnemyNavigation>();
            if(!profile || !ship || !ship.Health || !ship.Weapon || !ship.Movement){Fail("Missing profile / Ship components");return;}
            string error=profile.Validate(ship.Movement.settings.maxForwardSpeed,ship.Health.maximumHealth);
            if(error!=null){Fail(error);return;}
            if(!ship.Weapon.projectile || ship.Weapon.turrets.Length==0){Fail("Missing projectile / turrets");return;}
            foreach(var t in ship.Weapon.turrets)
                if(!t || t.muzzles==null || t.muzzles.Length!=2 || t.barrels==null || t.barrels.Length!=2){Fail("Invalid twin turret references");return;}
            initialized=true;ship.Damaged+=OnDamaged;ship.Health.Died+=OnDeath;
            player=FindFirstObjectByType<PlayerShip>();navigation.Initialize(ship);
            Target=null;RetreatAlly=null;hasThreat=false;wasVisible=false;confirmedAt=float.NegativeInfinity;
            SalvoError=EnemyAILogic.DiskError(random,profile.aimErrorRadius);
            State=EnemyState.Patrol;enteredAt=Time.time;nextDecision=Time.time;
            Enter(State);CheckHealth();
        }
        void Fail(string reason){Debug.LogError(name+" AI disabled: "+reason,this);var m=GetComponent<ShipMovement>();if(m){m.SetTargetSpeed(0);m.SetSteeringInput(0);}enabled=false;}
        void OnEnable()
        {
            if(!initialized || !ship || !ship.IsAlive)return;
            State=EnemyState.Patrol;Target=null;RetreatAlly=null;hasThreat=false;wasVisible=Visible=false;
            enteredAt=nextDecision=Time.time;confirmedAt=float.NegativeInfinity;blockedSideSince=-1;
            SalvoError=EnemyAILogic.DiskError(random,profile.aimErrorRadius);
            navigation.Initialize(ship);Enter(State);CheckHealth();
        }
        void OnDestroy(){if(ship){ship.Damaged-=OnDamaged;if(ship.Health)ship.Health.Died-=OnDeath;}}
        void OnDisable(){if(initialized){navigation.Stop();ship.Weapon.ClearAim();CanFire=false;}}
        void OnDeath()=>Change(EnemyState.Destroyed,"T06: HP zero");
        void CheckHealth()
        {
            if(!ship.IsAlive){OnDeath();return;}
            if(State!=EnemyState.Destroyed && State!=EnemyState.Retreat && (float)ship.Health.CurrentHealth/ship.Health.maximumHealth<=profile.retreatHealthRatio)
                Change(EnemyState.Retreat,"T04: low health (latched)");
        }
        void OnDamaged(int amount,Ship source)
        {
            if(!initialized||!isActiveAndEnabled)return;
            CheckHealth();
            if(State==EnemyState.Destroyed)return;
            if(amount>0 && EnemyAILogic.HostilePlayer(ship,source))
            {
                LastKnownPosition=source.WorldPosition;confirmedAt=Time.time;hasThreat=true;
                if(State!=EnemyState.Retreat){Target=source;if(State==EnemyState.Patrol)Change(EnemyState.Approach,"T01: player damage confirmed");}
            }
        }
        bool ClearLine(Vector3 origin,Vector3 point,Ship target)
        {
            Vector3 d=point-origin;if(d.sqrMagnitude<.01f)return true;
            foreach(var hit in Physics.RaycastAll(origin,d.normalized,d.magnitude,sightMask,QueryTriggerInteraction.Ignore))
            {var other=hit.collider.GetComponentInParent<Ship>();if(other!=ship && other!=target)return false;}
            return true;
        }
        public bool CanSee(Ship candidate)
        {
            if(!EnemyAILogic.HostilePlayer(ship,candidate)||EnemyAILogic.Distance(ship.WorldPosition,candidate.WorldPosition)>profile.detectionRange)return false;
            Vector3 origin=ship.WorldPosition+Vector3.up*5;
            foreach(var c in candidate.GetComponentsInChildren<Collider>())
            {
                if(!c.enabled||c.isTrigger)continue;Bounds b=c.bounds;
                for(int i=-1;i<=1;i++)
                    if(ClearLine(origin,c.ClosestPoint(b.center+candidate.WorldForward*(b.extents.magnitude*.8f*i)),candidate))return true;
            }
            return false;
        }
        void Update()
        {
            if(!initialized)return;
            CheckHealth();if(State==EnemyState.Destroyed)return;
            if(Target && !EnemyAILogic.HostilePlayer(ship,Target)){Target=null;ship.Weapon.ClearAim();}
            Visible=Target && CanSee(Target);
            if(Visible){LastKnownPosition=Target.WorldPosition;confirmedAt=Time.time;hasThreat=true;if(!wasVisible)visibleSince=Time.time;}
            wasVisible=Visible;
            if(Time.time>=nextDecision)
            {
                nextDecision=Time.time+profile.decisionInterval;
                if(State==EnemyState.Patrol)
                {
                    if(!player)player=FindFirstObjectByType<PlayerShip>();
                    if(CanSee(player)){Target=player;LastKnownPosition=player.WorldPosition;confirmedAt=Time.time;hasThreat=true;Visible=true;visibleSince=Time.time;wasVisible=true;}
                }
                bool valid=Target && EnemyAILogic.HostilePlayer(ship,Target) && MemoryAge<profile.targetForgetDelay;
                float distance=hasThreat?EnemyAILogic.Distance(ship.WorldPosition,LastKnownPosition):float.PositiveInfinity;
                var next=EnemyAILogic.Decide(State,(float)ship.Health.CurrentHealth/ship.Health.maximumHealth,valid,Visible,distance,StateAge,profile);
                if(next!=State)Change(next,next==EnemyState.Patrol?"T05: target lost":next==EnemyState.Approach?"T01/T03: acquire / range": "T02: engage range and hold time");
                if(State==EnemyState.Retreat)ChooseAlly();
            }
            Drive();
        }
        void Change(EnemyState next,string reason)
        {
            if(State==EnemyState.Destroyed||State==next)return;
            Exit(State);State=next;enteredAt=Time.time;TransitionReason=reason;Enter(next);
        }
        void Exit(EnemyState state){if(navigation)navigation.Stop();if(ship)ship.Weapon.ClearAim();CanFire=false;}
        void Enter(EnemyState state)
        {
            if(state==EnemyState.Patrol)
            {
                Target=null;Visible=wasVisible=false;ship.Weapon.ClearAim();
                float closest=float.PositiveInfinity;
                for(int i=0;i<waypoints.Length;i++)if(waypoints[i]){float d=EnemyAILogic.Distance(ship.WorldPosition,waypoints[i].position);if(d<closest){closest=d;waypoint=i;}}
            }
            if(state==EnemyState.Engage)
            {
                Vector3 toward=(LastKnownPosition-ship.WorldPosition).normalized;
                float left=SideScore(toward,-1),right=SideScore(toward,1);Broadside=left>right?-1:1;blockedSideSince=-1;
            }
            if(state==EnemyState.Retreat){Target=null;Visible=wasVisible=false;ChooseAlly();}
            if(state==EnemyState.Destroyed){Target=null;RetreatAlly=null;Visible=false;CanFire=false;navigation.Stop();ship.Weapon.ClearAim();}
        }
        float SideScore(Vector3 toward,int side)
        {
            Vector3 heading=Quaternion.Euler(0,side*profile.preferredBroadsideAngle,0)*toward;
            float score=navigation.DirectionClear(heading,profile)?3:0;
            float targetYaw=Vector3.SignedAngle(heading,toward,Vector3.up);
            foreach(var turret in ship.Weapon.turrets)if(turret && GunneryMath.InArc(targetYaw,turret.neutralYaw,turret.traverseArc))score++;
            return score+Vector3.Dot(ship.WorldForward,heading);
        }
        void ChooseAlly()
        {
            Ship best=RetreatAlly && RetreatAlly.isActiveAndEnabled && RetreatAlly.IsAlive && RetreatAlly.team==ship.team?RetreatAlly:null;
            float range=best?EnemyAILogic.Distance(ship.WorldPosition,best.WorldPosition):float.PositiveInfinity;
            foreach(var other in FindObjectsByType<Ship>(FindObjectsSortMode.InstanceID))
            {
                if(other==ship||!other.isActiveAndEnabled||!other.IsAlive||other.team!=ship.team)continue;
                float d=EnemyAILogic.Distance(ship.WorldPosition,other.WorldPosition);
                if(d<range-.01f){best=other;range=d;}
            }
            RetreatAlly=best;
        }
        void Drive()
        {
            Vector3 destination=ship.WorldPosition;float speed=0;
            if(State==EnemyState.Patrol)
            {
                if(waypoints!=null && waypoints.Length>0)
                {
                    waypoint=Mathf.Clamp(waypoint,0,waypoints.Length-1);
                    for(int i=0;i<waypoints.Length;i++)
                    {if(waypoints[waypoint] && EnemyAILogic.Distance(ship.WorldPosition,waypoints[waypoint].position)>profile.waypointRadius)break;waypoint=(waypoint+1)%waypoints.Length;}
                    if(waypoints[waypoint]){destination=waypoints[waypoint].position;speed=profile.patrolSpeed;}
                }
            }
            else if(State==EnemyState.Approach){destination=LastKnownPosition;speed=profile.approachSpeed;}
            else if(State==EnemyState.Engage)
            {
                Vector3 toward=LastKnownPosition-ship.WorldPosition;toward.y=0;float distance=toward.magnitude;toward.Normalize();
                Vector3 side=Quaternion.Euler(0,Broadside*profile.preferredBroadsideAngle,0)*toward;
                if(!navigation.DirectionClear(side,profile))
                {
                    if(blockedSideSince<0)blockedSideSince=Time.time;
                    if(Time.time-blockedSideSince>=profile.broadsideSwitchDelay && navigation.DirectionClear(Quaternion.Euler(0,-Broadside*profile.preferredBroadsideAngle,0)*toward,profile)){Broadside=-Broadside;blockedSideSince=-1;}
                }
                else blockedSideSince=-1;
                float angle=profile.preferredBroadsideAngle;
                if(distance>profile.preferredCombatRange+profile.combatRangeTolerance)angle=Mathf.Min(angle,45);
                else if(distance<profile.preferredCombatRange-profile.combatRangeTolerance)angle=130;
                destination=ship.WorldPosition+Quaternion.Euler(0,Broadside*angle,0)*toward*400;speed=profile.combatSpeed;
            }
            else if(State==EnemyState.Retreat)
            {
                speed=profile.retreatSpeed;
                if(RetreatAlly && RetreatAlly.IsAlive && RetreatAlly.isActiveAndEnabled)
                {
                    destination=RetreatAlly.WorldPosition;
                    float d=EnemyAILogic.Distance(ship.WorldPosition,destination);
                    if(d<profile.allyArrivalRadius){speed=Mathf.Min(speed,RetreatAlly.WorldVelocity.magnitude)*Mathf.InverseLerp(profile.allyArrivalRadius*.5f,profile.allyArrivalRadius,d);}
                }
                else {Vector3 away=hasThreat?ship.WorldPosition-LastKnownPosition:ship.WorldForward;away.y=0;if(away.sqrMagnitude<1)away=ship.WorldForward;destination=ship.WorldPosition+away.normalized*400;}
            }
            navigation.Drive(destination,speed,profile);
        }
        void LateUpdate()
        {
            CanFire=false;
            if(!initialized||State==EnemyState.Destroyed||!ship.IsAlive)return;
            if(!Target || !EnemyAILogic.HostilePlayer(ship,Target) || !CanSee(Target) || State==EnemyState.Retreat || State==EnemyState.Patrol)
            {ship.Weapon.ClearAim();FireStatus="No visible hostile target";return;}
            var w=ship.Weapon;var reference=w.turrets[0].muzzles[0];
            if(!CombatBallistics.Intercept(reference.position,Target.WorldPosition,Target.WorldVelocity,ship.WorldVelocity,w.muzzleSpeed,w.gravity,w.maximumRange,out var point,out _,w.horizontalRange))
            {w.ClearAim();FireStatus="No ballistic solution";return;}
            w.SetAim(point+SalvoError);
            if(State!=EnemyState.Engage || Time.time-visibleSince<profile.reactionDelay){FireStatus="Approach / reaction delay";return;}
            foreach(var turret in w.turrets)if(turret && TurretReady(turret))CanFire=true;
            FireStatus=CanFire?"READY":"Reload / traverse / obstructed";
            if(CanFire && w.TryFireAimed(TurretReady)){FireStatus="SALVO "+w.LastSalvoCount;SalvoError=EnemyAILogic.DiskError(random,profile.aimErrorRadius);}
        }
        bool TurretReady(CombatTurret t)
        {
            if(!ship.IsAlive||t.ReloadRemaining>0)return false;
            t.Track(0);
            foreach(var m in t.muzzles)
            {
                if(!m || !ship.Weapon.Solve(m.position,ship.Weapon.AimPoint,out var relative,out float time)||!t.Safe(relative)||!t.Safe(m.forward)||Vector3.Angle(m.forward,relative)>profile.aimTolerance)return false;
                // Straight line required by the design; arc segments additionally protect terrain / allies.
                if(!ClearLine(m.position,Target.WorldPosition+Vector3.up,Target))return false;
                Vector3 velocity=relative+ship.WorldVelocity,from=m.position;
                int steps=Mathf.Max(8,Mathf.CeilToInt(time/.2f));
                for(int i=1;i<=steps;i++)
                {
                    Vector3 to=CombatBallistics.Position(m.position,velocity,ship.Weapon.gravity,time*i/steps);
                    if(!ClearLine(from,to,Target))return false;from=to;
                }
            }
            return true;
        }
        void OnDrawGizmosSelected()
        {
            if(!drawGizmos||!profile)return;
            Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(transform.position,profile.detectionRange);
            Gizmos.color=Color.red;Gizmos.DrawWireSphere(transform.position,profile.engageEnterRange);
            Gizmos.color=Color.magenta;Gizmos.DrawWireSphere(transform.position,profile.engageExitRange);
            Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(transform.position,profile.preferredCombatRange);
            foreach(var w in waypoints)if(w)Gizmos.DrawWireSphere(w.position,profile.waypointRadius);
            if(navigation)Gizmos.DrawRay(transform.position,navigation.DesiredDirection*100);
        }
    }
}
