using UnityEngine;
namespace GrandNavigation.Gameplay
{
    // Minimal course extension point; this prototype does not simulate or spawn moving gunfire.
    public class Projectile : MonoBehaviour
    {
        public bool Activated { get; private set; }
        public virtual void Launch() => Activated=true;
        public static Vector3 ComposeWorldVelocity(Vector3 muzzleRelativeWorldVelocity,Vector3 shipWorldVelocity) =>
            muzzleRelativeWorldVelocity+shipWorldVelocity;
        public void ResolveHit(IDamageable target,int damage) { target?.TakeDamage(damage); }
    }
}
