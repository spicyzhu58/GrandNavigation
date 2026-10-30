using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public class Weapon : MonoBehaviour
    {
        [Tooltip("Optional test component; left empty in the movement scene.")]
        public Projectile projectile;
        public virtual bool TryFire()
        {
            if(!projectile)return false;
            projectile.Launch();return true;
        }
    }
}
