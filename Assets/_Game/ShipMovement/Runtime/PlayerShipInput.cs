using UnityEngine;
using UnityEngine.InputSystem;
namespace GrandNavigation.Gameplay
{
    [RequireComponent(typeof(PlayerShip),typeof(ShipMovement))]
    public sealed class PlayerShipInput : MonoBehaviour
    {
        [SerializeField] bool enableSpaceWeaponShortcut=true;
        ShipMovement movement;PlayerShip player;
        void Awake(){movement=GetComponent<ShipMovement>();player=GetComponent<PlayerShip>();}
        void Update()
        {
            var k=Keyboard.current;
            if(k==null){movement.SetThrottleInput(0);movement.SetSteeringInput(0);return;}
            movement.SetThrottleInput((k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
            movement.SetSteeringInput((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0));
            // Architecture hook only: Weapon has no Projectile assigned in this scene.
            if(enableSpaceWeaponShortcut && k.spaceKey.wasPressedThisFrame)player.FireWeapon();
        }
        void OnDisable(){if(movement){movement.SetThrottleInput(0);movement.SetSteeringInput(0);}}
    }
}
