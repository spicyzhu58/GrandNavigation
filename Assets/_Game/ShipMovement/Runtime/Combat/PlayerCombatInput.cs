using UnityEngine;
using UnityEngine.InputSystem;
namespace GrandNavigation.Gameplay
{
    [DefaultExecutionOrder(60)]
    public sealed class PlayerCombatInput : MonoBehaviour
    {
        public PlayerShip player;public Camera view;public CombatFireControl fireControl;public CombatAimController aim;public CombatCamera cameraRig;
        bool observeMouse,fireRequested;
        void Update()
        {
            observeMouse=fireRequested=false;
            var mouse=Mouse.current;var keys=Keyboard.current;if(!player.IsAlive){aim.SetAiming(false);cameraRig.SetAiming(false);return;}
            if(keys!=null&&keys.escapeKey.wasPressedThisFrame){fireControl.Lock(null);aim.SetAiming(false);}
            if(mouse!=null&&mouse.rightButton.wasPressedThisFrame)
            {
                var ray=view.ScreenPointToRay(mouse.position.ReadValue());
                if(Physics.Raycast(ray,out var hit,2000,~0,QueryTriggerInteraction.Ignore))fireControl.Lock(hit.collider.GetComponentInParent<Ship>());
            }
            bool wasAiming=aim.IsAiming;
            bool held=keys!=null&&(keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed);
            aim.SetAiming(held);cameraRig.SetAiming(aim.IsAiming);
            if(mouse!=null)
            {
                if(aim.IsAiming){aim.Adjust(mouse.delta.ReadValue());cameraRig.Zoom(mouse.scroll.ReadValue().y);}
                else observeMouse=!wasAiming&&mouse.delta.ReadValue().sqrMagnitude>0;
                fireRequested=mouse.leftButton.wasPressedThisFrame;
            }
        }
        void LateUpdate()
        {
            // Orbit and camera return have finished for this frame, so the ray matches the visible sight.
            if(observeMouse&&Mouse.current!=null)aim.AimFromObservation(view,Mouse.current.position.ReadValue());
            if(fireRequested)player.FireWeapon();
        }
    }
}
