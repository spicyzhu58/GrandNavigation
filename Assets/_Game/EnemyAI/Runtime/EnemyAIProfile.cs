using UnityEngine;
namespace GrandNavigation.Gameplay.AI
{
    [CreateAssetMenu(menuName="Grand Navigation/Enemy AI Profile")]
    public sealed class EnemyAIProfile : ScriptableObject
    {
        [Header("Perception / FSM (UU, seconds)")]
        public float detectionRange=1200, engageEnterRange=900, engageExitRange=1000;
        public float preferredCombatRange=750, combatRangeTolerance=100;
        public float decisionInterval=1, minStateTime=3, targetForgetDelay=8;
        [Range(0,1)] public float retreatHealthRatio=.2f;
        [Header("Target speeds (UU/s)")]
        public float patrolSpeed=10, approachSpeed=15, combatSpeed=15, retreatSpeed=15;
        public float waypointRadius=80, allyArrivalRadius=180;
        [Header("Broadside / fire")]
        [Range(0,180)] public float preferredBroadsideAngle=80;
        public float broadsideSwitchDelay=20, reactionDelay=2, aimErrorRadius=24, aimTolerance=3;
        [Header("Navigation (look ahead is seconds)")]
        public float avoidLookAhead=12, obstacleClearance=60, stuckTimeout=8, minProgressDistance=25;
        public string Validate(float maximumSpeed, int hp)
        {
            foreach(var field in GetType().GetFields())
                if(field.FieldType==typeof(float) && (!float.IsFinite((float)field.GetValue(this)) || (float)field.GetValue(this)<0))return field.Name+" must be finite and nonnegative";
            if(hp<=0)return "maximumHealth must be positive";
            if(!(detectionRange>engageExitRange && engageExitRange>engageEnterRange && engageEnterRange>0))return "Detection > Exit > Enter > 0 required";
            if(!(preferredCombatRange>combatRangeTolerance && preferredCombatRange<=engageEnterRange && preferredCombatRange+combatRangeTolerance<engageExitRange && combatRangeTolerance>0))return "Invalid combat distance band";
            if(!(decisionInterval>0 && minStateTime>=decisionInterval && targetForgetDelay>decisionInterval && broadsideSwitchDelay>=decisionInterval && stuckTimeout>decisionInterval))return "Invalid FSM timing";
            if(!(retreatHealthRatio>0 && retreatHealthRatio<1))return "Retreat ratio must be between 0 and 1";
            foreach(float speed in new[]{patrolSpeed,approachSpeed,combatSpeed,retreatSpeed})if(speed<=0||speed>maximumSpeed)return "AI speed exceeds movement capability or is zero";
            if(waypointRadius<=0||allyArrivalRadius<=0||avoidLookAhead<=0||obstacleClearance<=0||minProgressDistance<=0||preferredBroadsideAngle>180)return "Invalid navigation parameter";
            return null;
        }
    }
    public enum EnemyState { Patrol, Approach, Engage, Retreat, Destroyed }
    public static class EnemyAILogic
    {
        public static EnemyState Decide(EnemyState state, float healthRatio, bool valid, bool visible, float distance, float age, EnemyAIProfile p)
        {
            if(state==EnemyState.Destroyed || healthRatio<=0)return EnemyState.Destroyed;
            if(state==EnemyState.Retreat || healthRatio<=p.retreatHealthRatio)return EnemyState.Retreat;
            if(state!=EnemyState.Patrol && !valid)return EnemyState.Patrol;
            if(state==EnemyState.Patrol && valid)return EnemyState.Approach;
            if(age<p.minStateTime)return state;
            if(state==EnemyState.Approach && visible && distance<=p.engageEnterRange)return EnemyState.Engage;
            if(state==EnemyState.Engage && distance>=p.engageExitRange)return EnemyState.Approach;
            return state;
        }
        public static bool HostilePlayer(Ship self, Ship other) => self && other is PlayerShip && other.isActiveAndEnabled && other.IsAlive && other.team!=self.team;
        public static Vector3 DiskError(System.Random random,float radius)
        {
            float r=Mathf.Sqrt((float)random.NextDouble())*radius, a=(float)random.NextDouble()*Mathf.PI*2;
            return new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
        }
        public static bool Stuck(float elapsed,float projectedProgress,float timeout,float minimum) => elapsed>=timeout && projectedProgress<minimum;
        public static float Distance(Vector3 a, Vector3 b) => Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    }
}
