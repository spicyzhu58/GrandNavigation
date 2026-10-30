using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class HealthSystem : MonoBehaviour
    {
        [Min(1)] public int maximumHealth=100;
        public int CurrentHealth { get; private set; }
        void Awake() => Restore();
        public void Restore() => CurrentHealth=Mathf.Max(1,maximumHealth);
        public void TakeDamage(int damage) => CurrentHealth=Mathf.Max(0,CurrentHealth-Mathf.Max(0,damage));
    }
}
