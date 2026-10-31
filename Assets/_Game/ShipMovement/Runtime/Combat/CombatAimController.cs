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
        Vector3 forward,right;
        void Start()=>SetPoint(player.WorldPosition+player.WorldForward*initialRange);
        public void SetPoint(Vector3 point){if(!CombatBallistics.Finite(point))return;PlayerAimPoint=new Vector3(point.x,0,point.z);player.Weapon.SetAim(PlayerAimPoint);}
        public void SetAiming(bool value)
        {
            value=value&&player.IsAlive&&fireControl.Target&&fireControl.Target.IsAlive;
            if(value==IsAiming)return;
            IsAiming=value;
            if(value)
            {
                forward=Vector3.ProjectOnPlane(PlayerAimPoint-player.WorldPosition,Vector3.up).normalized;
                if(forward.sqrMagnitude<.01f)forward=player.WorldForward;
                right=Vector3.Cross(Vector3.up,forward);
            }
        }
        public void SetViewBasis(Vector3 direction)
        {
            direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            if(direction.sqrMagnitude<.01f)return;
            forward=direction;right=Vector3.Cross(Vector3.up,forward);
        }
        // Called only for mouse input, never because a lock, prediction or camera blend changed.
        public void AimFromObservation(Camera camera,Vector2 screenPosition)
        {
            if(!camera||IsAiming||!player.IsAlive)return;
            Ray ray=camera.ScreenPointToRay(screenPosition);Vector3 point;
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))point=ray.GetPoint(distance);
            else
            {
                Vector3 horizontal=Vector3.ProjectOnPlane(ray.direction,Vector3.up).normalized;
                if(horizontal.sqrMagnitude<.01f)return;
                point=player.WorldPosition+horizontal*player.Weapon.maximumRange;
            }
            Vector3 offset=Vector3.ProjectOnPlane(point-player.WorldPosition,Vector3.up);
            SetPoint(player.WorldPosition+Vector3.ClampMagnitude(offset,player.Weapon.maximumRange));
        }
        public void Adjust(Vector2 delta){if(IsAiming)SetPoint(PlayerAimPoint+(right*delta.x+forward*delta.y)*metresPerPixel);}
    }
}
