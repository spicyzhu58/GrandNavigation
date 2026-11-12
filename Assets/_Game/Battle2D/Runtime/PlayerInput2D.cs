using UnityEngine;
using UnityEngine.InputSystem;
namespace GrandNavigation.Battle2D
{
    public sealed class PlayerInput2D : MonoBehaviour
    {
        public ShipActor2D ship;
        public Camera view;
        public ShipActor2D Target {get;private set;}
        Weapon2D weapon;
        void Start(){weapon=GetComponent<Weapon2D>();}
        public void Select(Vector2 point)
        {
            foreach(var hit in Physics2D.OverlapPointAll(point))
            {
                var other=hit.GetComponentInParent<ShipActor2D>();
                if(other&&other.Alive&&other.team!=ship.team&&Vector2.Distance(ship.transform.position,other.transform.position)<=weapon.range&&EnemyAI2D.Visible(ship,other)) {Target=other;return;}
            }
        }
        void Update()
        {
            if(!ship.Alive)return;var k=Keyboard.current;var mouse=Mouse.current;
            if(Target&&(!Target.Alive||!Target.isActiveAndEnabled))Target=null;
            if(k!=null){ship.Movement.Input((k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0),(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0));if(k.escapeKey.wasPressedThisFrame)Target=null;}
            if(mouse!=null&&view)
            {
                Vector2 point=view.ScreenToWorldPoint(mouse.position.ReadValue());
                if(mouse.rightButton.wasPressedThisFrame)Select(point);
                if(mouse.leftButton.wasPressedThisFrame)weapon.Fire(point);
            }
        }
    }
}
