using UnityEngine;
namespace GrandNavigation.Gameplay
{
    [System.Serializable]
    public sealed class MovementSettings
    {
        [Min(0)] public float maxForwardSpeed=25,maxReverseSpeed=8,targetSpeedRate=10;
        [Min(0)] public float forwardAcceleration=4,brakingDeceleration=6,reverseAcceleration=2.5f;
        [Range(1,60)] public float maxRudderAngle=35;
        [Min(0)] public float rudderRate=90,maxTurnRate=16;
        [Range(0,1)] public float reverseTurnEfficiency=.6f;
        [Min(.1f)] public float fullTurnSpeed=12,turnAcceleration=24,lateralDamping=1.4f;
    }
    public static class ShipMovementMath
    {
        public static float AdjustTarget(float target,float input,float dt,MovementSettings s) =>
            Mathf.Clamp(target+Mathf.Clamp(input,-1,1)*s.targetSpeedRate*Mathf.Max(0,dt),-s.maxReverseSpeed,s.maxForwardSpeed);
        public static float StepSpeed(float actual,float target,float dt,MovementSettings s)
        {
            target=Mathf.Clamp(target,-s.maxReverseSpeed,s.maxForwardSpeed);
            // A reversal consumes a braking phase first; acceleration into reverse starts next step.
            if(actual*target<0) return Mathf.MoveTowards(actual,0,s.brakingDeceleration*dt);
            float rate=Mathf.Abs(target)<Mathf.Abs(actual)?s.brakingDeceleration:target<0?s.reverseAcceleration:s.forwardAcceleration;
            return Mathf.Clamp(Mathf.MoveTowards(actual,target,rate*dt),-s.maxReverseSpeed,s.maxForwardSpeed);
        }
        public static float DesiredTurnRate(float speed,float rudder,MovementSettings s)
        {
            if(Mathf.Abs(speed)<.05f)return 0;
            float authority=Mathf.Clamp01(Mathf.Abs(speed)/Mathf.Max(.1f,s.fullTurnSpeed));
            return Mathf.Clamp(rudder/s.maxRudderAngle,-1,1)*s.maxTurnRate*authority*(speed<0?-s.reverseTurnEfficiency:1);
        }
    }
}
