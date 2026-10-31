using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class TestTargetMotion : MonoBehaviour
    {
        [Range(0,15)] public float speed=8;
        [Min(20)] public float halfWidth=130;
        Ship ship;bool returning;
        void Start()=>ship=GetComponent<Ship>();
        void FixedUpdate()
        {
            if(!ship.IsAlive)return;
            if(Mathf.Abs(ship.WorldPosition.x)>halfWidth)returning=true;
            if(Mathf.Abs(ship.WorldPosition.x)<halfWidth-25)returning=false;
            ship.Movement.SetTargetSpeed(speed);ship.Movement.SetSteeringInput(returning?1:0);
        }
    }
}
