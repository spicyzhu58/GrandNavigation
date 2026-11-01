using UnityEngine;
using UnityEngine.InputSystem;
namespace GrandNavigation.Gameplay
{
    [DefaultExecutionOrder(60)]
    public sealed class PlayerCombatInput : MonoBehaviour
    {
        public PlayerShip player;public Camera view;public CombatFireControl fireControl;public CombatAimController aim;public CombatCamera cameraRig;
        [Header("Target selection")]
        [SerializeField,Min(0),Tooltip("Screen pixels from the projected hull, independent of firing aim.")] float lockAssistRadius=55;
        [SerializeField,Min(1),Tooltip("Player-to-target horizontal distance in world units.")] float maximumLockDistance=1200;
        [SerializeField,Min(1200),Tooltip("Exact selection ray length; extended if required by camera offset.")] float preciseRayDistance=2000;
        [SerializeField] bool enableLockAssist=true;
        [SerializeField] bool checkLockOcclusion=true;
        [SerializeField] bool enemiesOnly=true;
        [SerializeField,Tooltip("Selectable ship colliders; Ignore Raycast, Water and UI excluded by default.")] LayerMask targetLayers=~((1<<2)|(1<<4)|(1<<5));
        [SerializeField,Tooltip("Solid vision blockers; triggers ignored. Put nonblocking helpers on Ignore Raycast or Water.")] LayerMask obstructionLayers=~((1<<2)|(1<<4)|(1<<5));
        void OnValidate(){lockAssistRadius=Mathf.Max(0,lockAssistRadius);maximumLockDistance=Mathf.Max(1,maximumLockDistance);preciseRayDistance=Mathf.Max(1200,preciseRayDistance);}
        public Ship SelectTargetAt(Vector2 pointer)
        {
            Ship selected=TargetSelectionHelper.Select(view,pointer,player,maximumLockDistance,preciseRayDistance,lockAssistRadius,
                enableLockAssist,checkLockOcclusion,enemiesOnly,targetLayers,obstructionLayers);
            if(selected)fireControl.Lock(selected);
            else if(fireControl.Target&&!TargetSelectionHelper.Eligible(fireControl.Target,player,maximumLockDistance,enemiesOnly))fireControl.Lock(null);
            return selected;
        }
        bool observeMouse,fireRequested;
        void Update()
        {
            observeMouse=fireRequested=false;
            var mouse=Mouse.current;var keys=Keyboard.current;if(!player.IsAlive){aim.SetAiming(false);cameraRig.SetAiming(false);return;}
            if(keys!=null&&keys.escapeKey.wasPressedThisFrame){fireControl.Lock(null);aim.SetAiming(false);}
            if(mouse!=null&&mouse.rightButton.wasPressedThisFrame)
            {
                SelectTargetAt(mouse.position.ReadValue());
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
