using UnityEngine;
using System.Collections.Generic;
namespace GrandNavigation.Gameplay.AI
{
    // Local swept clearance with a persistent passing side. All motion remains in ShipMovement.
    [RequireComponent(typeof(ShipMovement))]
    public sealed class EnemyNavigation : MonoBehaviour
    {
        [SerializeField] LayerMask obstacleMask=~((1<<2)|(1<<4)|(1<<5));
        [SerializeField,Min(1)] float mapHalfExtent=1280;
        [SerializeField,Min(1)] float steeringAngle=35;
        [SerializeField,Min(.1f)] float recoveryDuration=3, retryDelay=5;
        [SerializeField,Range(1,5)] int maximumRecoveryAttempts=3;
        public bool Avoiding {get;private set;}
        public Vector3 DesiredDirection {get;private set;}
        public float StuckTime {get;private set;}
        public string Status {get;private set;}="CLEAR";
        readonly List<Collider> staticObstacles=new List<Collider>();
        readonly List<Vector3> route=new List<Vector3>();
        Vector3 routedGoal;float routeAt;
        Ship ship; ShipMovement movement; float hullRadius, windowStart, progress, recoveryUntil, retryUntil;
        Vector3 previous, progressDirection; int passingSide, attempts;
        public void Initialize(Ship owner)
        {
            ship=owner;movement=ship.Movement;
            var box=GetComponent<BoxCollider>();hullRadius=box?Mathf.Max(box.bounds.extents.x,box.bounds.extents.z):18;
            previous=ship.WorldPosition;windowStart=Time.time;progress=0;attempts=0;passingSide=0;recoveryUntil=retryUntil=0;
            route.Clear();routeAt=0;staticObstacles.Clear();
            foreach(var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if(c.enabled && !c.isTrigger && !c.attachedRigidbody && (obstacleMask.value&(1<<c.gameObject.layer))!=0 && c.bounds.size.x<mapHalfExtent && c.bounds.size.z<mapHalfExtent)staticObstacles.Add(c);
        }
        bool Own(Collider c)=>c.GetComponentInParent<Ship>()==ship;
        float ClearDistance(Vector3 direction,float distance,float radius)
        {
            Vector3 origin=ship.WorldPosition;origin.y=2;
            float clear=distance;
            foreach(var c in Physics.OverlapSphere(origin,radius,obstacleMask,QueryTriggerInteraction.Ignore))
            {
                if(Own(c))continue;
                Vector3 closest=c.ClosestPoint(origin), away=origin-closest;away.y=0;
                if(away.sqrMagnitude<.01f || Vector3.Dot(direction,away.normalized)<-.05f)clear=0;
            }
            foreach(var hit in Physics.SphereCastAll(origin,radius,direction,distance,obstacleMask,QueryTriggerInteraction.Ignore))
                {
                if(Own(hit.collider))continue;
                // Unity may report distance zero for an initial clearance overlap even
                // when travelling out of it. The overlap pass already rejects inward motion.
                if(hit.distance<=.001f)
                {
                    Vector3 away=origin-hit.collider.ClosestPoint(origin);away.y=0;
                    if(away.sqrMagnitude>.01f && Vector3.Dot(direction,away.normalized)>=-.05f)continue;
                }
                clear=Mathf.Min(clear,hit.distance);
            }
            float limit=mapHalfExtent-radius;
            if(Mathf.Abs(direction.x)>.001f)clear=Mathf.Min(clear,Mathf.Max(0,((direction.x>0?limit:-limit)-origin.x)/direction.x));
            if(Mathf.Abs(direction.z)>.001f)clear=Mathf.Min(clear,Mathf.Max(0,((direction.z>0?limit:-limit)-origin.z)/direction.z));
            return clear;
        }
        public bool DirectionClear(Vector3 direction,EnemyAIProfile profile)=>ClearDistance(direction.normalized,Mathf.Max(40,Mathf.Abs(movement.ActualSpeed)*profile.avoidLookAhead),hullRadius+profile.obstacleClearance)>40;
        public void Drive(Vector3 destination,float speed,EnemyAIProfile p)
        {
            if(speed>0)destination=RoutePoint(destination,p);
            Vector3 delta=destination-ship.WorldPosition;delta.y=0;
            if(delta.sqrMagnitude<1||speed<=0){Stop();windowStart=Time.time;progress=0;previous=ship.WorldPosition;return;}
            Vector3 desired=delta.normalized;
            progressDirection=desired;
            if(retryUntil>0 && Time.time>=retryUntil)
            {
                retryUntil=0;windowStart=Time.time;progress=0;previous=ship.WorldPosition;
            }
            progress+=Vector3.Dot(ship.WorldPosition-previous,progressDirection);previous=ship.WorldPosition;
            StuckTime=Time.time-windowStart;
            if(StuckTime>=p.stuckTimeout && Time.time>=recoveryUntil && Time.time>=retryUntil)
            {
                if(EnemyAILogic.Stuck(StuckTime,progress,p.stuckTimeout,p.minProgressDistance))
                {
                    attempts++;passingSide=passingSide==0?1:-passingSide;
                    if(attempts<=maximumRecoveryAttempts)recoveryUntil=Time.time+recoveryDuration;
                    else retryUntil=Time.time+retryDelay; // Further reversals require actual progress first.
                }
                else attempts=0;
                windowStart=Time.time;progress=0;progressDirection=desired;
            }
            if(Time.time<retryUntil){Stop();Status="BLOCKED: waiting to replan";return;}
            float radius=hullRadius+p.obstacleClearance;
            float horizon=Mathf.Max(radius*1.5f,Mathf.Max(speed,Mathf.Abs(movement.ActualSpeed))*p.avoidLookAhead);
            float currentClear=ClearDistance(movement.Forward,horizon,radius);
            float desiredHorizon=Mathf.Min(horizon,delta.magnitude);
            float wantedClear=ClearDistance(desired,desiredHorizon,radius);
            bool detour=wantedClear<desiredHorizon*.9f;
            Avoiding=currentClear<horizon*.9f || detour;
            if(Avoiding)
            {
                // Do not reject a safe corner waypoint because the straight ray beyond
                // that corner intersects the island; the next path segment turns there.
                if(detour)
                {
                float best=float.NegativeInfinity;Vector3 selected=desired;int selectedSide=passingSide;
                for(int i=-6;i<=6;i++)
                {
                    float angle=i*30;Vector3 candidate=Quaternion.Euler(0,angle,0)*desired;
                    float clear=ClearDistance(candidate,horizon,radius);
                    float turn=Vector3.SignedAngle(movement.Forward,candidate,Vector3.up);
                    int side=turn<0?-1:1;
                    float score=clear/horizon*5+Vector3.Dot(candidate,desired)-Mathf.Abs(turn)/180;
                    if(passingSide!=0 && side!=passingSide)score-=1.2f;
                    if(score>best){best=score;selected=candidate;selectedSide=side;}
                }
                desired=selected;passingSide=selectedSide;
                }
                float braking=movement.ActualSpeed*movement.ActualSpeed/(2*Mathf.Max(.1f,movement.settings.brakingDeceleration));
                float available=ClearDistance(movement.Forward,braking+radius,hullRadius+3);
                speed*=Mathf.Clamp01(available/Mathf.Max(1,braking+radius));
                // A small forward crawl supplies rudder authority only if the real hull has space.
                if(speed<1 && ClearDistance(movement.Forward,15,hullRadius+2)>10)speed=1;
                if(ClearDistance(desired,Mathf.Min(horizon,delta.magnitude),hullRadius+3)<5)speed=0;
            }
            else passingSide=0;
            if(Time.time<recoveryUntil)
            {
                Avoiding=true;Status="RECOVERY: reverse and replan";
                if(ClearDistance(-movement.Forward,25,hullRadius+3)>20){speed=-Mathf.Min(3,movement.settings.maxReverseSpeed);desired=Quaternion.Euler(0,passingSide*60,0)*movement.Forward;}
                else speed=0;
            }
            else Status=Avoiding?"AVOIDING":"CLEAR";
            DesiredDirection=desired;
            float error=Vector3.SignedAngle(movement.Forward,desired,Vector3.up);
            movement.SetTargetSpeed(speed);
            movement.SetSteeringInput(Mathf.Clamp(error/steeringAngle,-1,1)*(speed<0?-1:1));
        }
        // Tiny visibility graph for the fixed primitive islands; local sweeps still handle
        // moving ships and braking. Bounds are read from actual saved colliders, never presets.
        static bool Inside(Vector3 p,Bounds b)=>p.x>b.min.x && p.x<b.max.x && p.z>b.min.z && p.z<b.max.z;
        static Vector3 Outside(Vector3 p,Bounds b)
        {
            if(!Inside(p,b))return p;
            float[] d={p.x-b.min.x,b.max.x-p.x,p.z-b.min.z,b.max.z-p.z};int k=0;
            for(int i=1;i<4;i++)if(d[i]<d[k])k=i;
            if(k==0)p.x=b.min.x-2;else if(k==1)p.x=b.max.x+2;else if(k==2)p.z=b.min.z-2;else p.z=b.max.z+2;
            return p;
        }
        static bool Crosses(Vector3 a,Vector3 b,Bounds box)
        {
            float lo=0,hi=1;Vector3 d=b-a;
            for(int axis=0;axis<2;axis++)
            {
                float start=axis==0?a.x:a.z,delta=axis==0?d.x:d.z;
                float min=axis==0?box.min.x:box.min.z,max=axis==0?box.max.x:box.max.z;
                if(Mathf.Abs(delta)<.0001f){if(start<min||start>max)return false;continue;}
                float t0=(min-start)/delta,t1=(max-start)/delta;if(t0>t1){float temp=t0;t0=t1;t1=temp;}
                lo=Mathf.Max(lo,t0);hi=Mathf.Min(hi,t1);if(lo>hi)return false;
            }
            return hi>=0 && lo<=1;
        }
        Vector3 RoutePoint(Vector3 destination,EnemyAIProfile p)
        {
            float margin=hullRadius+p.obstacleClearance+15,limit=mapHalfExtent-margin;
            destination.x=Mathf.Clamp(destination.x,-limit,limit);destination.z=Mathf.Clamp(destination.z,-limit,limit);
            Vector3 start=ship.WorldPosition;start.y=0;destination.y=0;
            var boxes=new List<Bounds>();
            foreach(var c in staticObstacles)if(c && c.enabled)
            {var b=c.bounds;b.Expand(new Vector3(margin*2,0,margin*2));boxes.Add(b);}
            foreach(var box in boxes)
            {
                // An existing spawn can be inside the safety margin but outside the real hull.
                if(Inside(start,box))return Outside(start,box);
                destination=Outside(destination,box);
            }
            bool Clear(Vector3 a,Vector3 b){foreach(var box in boxes)if(Crosses(a,b,box))return false;return true;}
            if(Clear(start,destination)){route.Clear();return destination;}
            if(Time.time>=routeAt || EnemyAILogic.Distance(destination,routedGoal)>40 || route.Count==0)
            {
                routedGoal=destination;routeAt=Time.time+1;route.Clear();
                var nodes=new List<Vector3>{start,destination};
                foreach(var b in boxes)
                    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    {var v=new Vector3(b.center.x+x*(b.extents.x+2),0,b.center.z+z*(b.extents.z+2));if(Mathf.Abs(v.x)<limit&&Mathf.Abs(v.z)<limit)nodes.Add(v);}
                var cost=new float[nodes.Count];var previousNode=new int[nodes.Count];var used=new bool[nodes.Count];
                for(int i=0;i<cost.Length;i++){cost[i]=float.PositiveInfinity;previousNode[i]=-1;}cost[0]=0;
                for(int step=0;step<nodes.Count;step++)
                {
                    int u=-1;for(int i=0;i<nodes.Count;i++)if(!used[i] && (u<0||cost[i]<cost[u]))u=i;
                    if(u<0||float.IsPositiveInfinity(cost[u]))break;used[u]=true;if(u==1)break;
                    for(int v=0;v<nodes.Count;v++)if(!used[v] && Clear(nodes[u],nodes[v]))
                    {float candidate=cost[u]+Vector3.Distance(nodes[u],nodes[v]);if(candidate<cost[v]){cost[v]=candidate;previousNode[v]=u;}}
                }
                if(previousNode[1]>=0){int i=1;while(i!=0 && i>=0){route.Insert(0,nodes[i]);i=previousNode[i];}}
            }
            while(route.Count>1 && EnemyAILogic.Distance(start,route[0])<Mathf.Max(25,p.obstacleClearance*.5f))route.RemoveAt(0);
            return route.Count>0?route[0]:destination;
        }
        public void Stop(){if(!movement)return;movement.SetTargetSpeed(0);movement.SetSteeringInput(0);}
    }
}
