using UnityEngine;
using UnityEngine.InputSystem;
namespace GrandNavigation.Gameplay
{
    [DefaultExecutionOrder(10)]
    public sealed class PlayerCombatInput : MonoBehaviour
    {
        public PlayerShip player;public Camera view;public CombatFireControl fireControl;public CombatAimController aim;public CombatCamera cameraRig;
        void Update()
        {
            var mouse=Mouse.current;var keys=Keyboard.current;if(!player.IsAlive){aim.SetAiming(false);cameraRig.SetAiming(false);return;}
            if(keys!=null&&keys.escapeKey.wasPressedThisFrame){fireControl.Lock(null);aim.SetAiming(false);}
            if(mouse!=null&&mouse.rightButton.wasPressedThisFrame)
            {
                var ray=view.ScreenPointToRay(mouse.position.ReadValue());
                if(Physics.Raycast(ray,out var hit,2000,~0,QueryTriggerInteraction.Ignore))fireControl.Lock(hit.collider.GetComponentInParent<Ship>());
            }
            bool held=keys!=null&&(keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed);
            aim.SetAiming(held);cameraRig.SetAiming(aim.IsAiming);
            if(aim.IsAiming&&mouse!=null)aim.Adjust(mouse.delta.ReadValue());
            if(mouse!=null&&mouse.leftButton.wasPressedThisFrame)player.FireWeapon();
        }
    }
}
