using UnityEngine;
using GrandNavigation.Gameplay.AI;
namespace GrandNavigation.Battle2D
{
    public sealed class EnemyAI2D : MonoBehaviour
    {
        public ShipActor2D player;
        public EnemyAIProfile profile;
        public EnemyState State {get;private set;}=EnemyState.Patrol;
        ShipActor2D ship;Weapon2D weapon;Vector2 anchor,lastSeen,previous;float nextDecision,entered,seenAt=-999,nextProgress,recoverUntil;int route,side=1;
        readonly System.Random random=new();
        public void MarkDestroyed(){State=EnemyState.Destroyed;enabled=false;}
        void Start(){ship=GetComponent<ShipActor2D>();weapon=GetComponent<Weapon2D>();anchor=previous=transform.position;entered=Time.time;nextProgress=Time.time+5;}
        public static bool Visible(ShipActor2D source,ShipActor2D target)
        {
            Vector2 start=source.transform.position,delta=(Vector2)target.transform.position-start;
            foreach(var hit in Physics2D.RaycastAll(start,delta.normalized,delta.magnitude))
            {if(hit.collider.isTrigger)continue;var actor=hit.collider.GetComponentInParent<ShipActor2D>();if(actor==source||actor==target)continue;return false;}
            return true;
        }
        void Update()
        {
            if(!ship.Alive){State=EnemyState.Destroyed;enabled=false;return;}
            bool visible=player&&player.Alive&&Vector2.Distance(transform.position,player.transform.position)<=profile.detectionRange&&Visible(ship,player);
            if(visible){lastSeen=player.transform.position;seenAt=Time.time;}
            bool valid=player&&player.Alive&&Time.time-seenAt<profile.targetForgetDelay;
            float distance=Vector2.Distance(transform.position,lastSeen);
            if(Time.time>=nextDecision)
            {
                nextDecision=Time.time+profile.decisionInterval;
                // 2D profile sets retreatHealthRatio=0: living ships use only Patrol/Approach/Engage.
                var next=EnemyAILogic.Decide(State,(float)ship.Health.CurrentHealth/ship.Health.maximumHealth,valid,visible,distance,Time.time-entered,profile);
                if(next!=State){State=next;entered=Time.time;}
            }
            Vector2 destination;
            if(State==EnemyState.Patrol)
            {
                var offset=new[]{new Vector2(35,0),new Vector2(0,-35),new Vector2(-35,0),new Vector2(0,35)};
                destination=anchor+offset[route];if(Vector2.Distance(transform.position,destination)<12)route=(route+1)%4;
            }
            else if(State==EnemyState.Engage&&distance<profile.preferredCombatRange+profile.combatRangeTolerance)
            {
                Vector2 to=(lastSeen-(Vector2)transform.position).normalized;
                destination=(Vector2)transform.position+new Vector2(-to.y,to.x)*side*50;
            }
            else destination=lastSeen;
            Drive(destination,State==EnemyState.Patrol?profile.patrolSpeed:State==EnemyState.Approach?profile.approachSpeed:profile.combatSpeed);
            if(State==EnemyState.Engage&&visible&&Time.time-entered>=profile.reactionDelay&&weapon.ReloadRemaining<=0)
            {
                Vector3 error=EnemyAILogic.DiskError(random,profile.aimErrorRadius);
                Vector2 aim=(Vector2)player.transform.position+player.Movement.Body.linearVelocity*(distance/weapon.speed)+new Vector2(error.x,error.z);
                weapon.Fire(aim);
            }
        }
        void Drive(Vector2 destination,float speed)
        {
            Vector2 position=transform.position,desired=(destination-position).normalized;
            float look=Mathf.Max(18,Mathf.Abs(ship.Movement.Speed)*2);
            bool blocked=Physics2D.CircleCast(position,5,desired,look,1<<6);
            if(blocked)
            {
                float best=-999;Vector2 choice=-desired;
                for(int i=-6;i<=6;i++)
                {
                    Vector2 direction=Quaternion.Euler(0,0,i*30)*desired;
                    var hit=Physics2D.CircleCast(position,5,direction,look,1<<6);
                    float clear=hit?hit.distance:look;
                    float score=clear/look*4+Vector2.Dot(direction,desired)+i*side*.01f;
                    if(score>best){best=score;choice=direction;}
                }
                desired=choice;speed=Mathf.Min(speed,5);
            }
            if(Time.time>=nextProgress)
            {
                if(Vector2.Distance(previous,position)<3){recoverUntil=Time.time+1.5f;side=-side;route=(route+1)%4;}
                previous=position;nextProgress=Time.time+5;
            }
            if(Time.time<recoverUntil){speed=-2;desired=Quaternion.Euler(0,0,side*60)*transform.up;}
            float error=-Vector2.SignedAngle(transform.up,desired);
            ship.Movement.Drive(speed,Mathf.Clamp(error/35,-1,1)*(speed<0?-1:1));
        }
    }
}
