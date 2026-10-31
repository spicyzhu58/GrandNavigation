using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class CombatAimController : MonoBehaviour
    {
        public PlayerShip player;public CombatFireControl fireControl;
        [Min(.001f)] public float metresPerPixel=.15f;
        [Min(1)] public float initialRange=160;
        public Vector3 PlayerAimPoint {get;private set;}
        public bool IsAiming {get;private set;}
        Vector3 forward,right;bool initializedFromTarget;
        void Start()=>SetPoint(player.WorldPosition+player.WorldForward*initialRange);
        public void SetPoint(Vector3 point){if(!CombatBallistics.Finite(point))return;PlayerAimPoint=new Vector3(point.x,0,point.z);player.Weapon.SetAim(PlayerAimPoint);}
        public void SetAiming(bool value)
        {
            value=value&&player.IsAlive&&fireControl.Target&&fireControl.Target.IsAlive;
            if(value==IsAiming)return;
            IsAiming=value;
            if(value)
            {
                if(!initializedFromTarget&&fireControl.Valid){SetPoint(fireControl.PredictedIntercept);initializedFromTarget=true;}
                forward=Vector3.ProjectOnPlane(PlayerAimPoint-player.WorldPosition,Vector3.up).normalized;
                if(forward.sqrMagnitude<.01f)forward=player.WorldForward;
                right=Vector3.Cross(Vector3.up,forward);
            }
        }
        public void Adjust(Vector2 delta){if(IsAiming)SetPoint(PlayerAimPoint+(right*delta.x+forward*delta.y)*metresPerPixel);}
    }
}
