using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class Health : MonoBehaviour
    {
        public float maxHP = 600;
        public float CurrentHP { get; private set; }
        public bool IsAlive => CurrentHP > 0;
        public int HitCount { get; private set; }
        public float LastDamage { get; private set; }
        public float LastHitTime { get; private set; } = -100;
        void Awake() => Restore();
        public void Restore() { CurrentHP = maxHP; HitCount = 0; LastDamage = 0; LastHitTime = -100; }
        public void Damage(float amount, Vector3 point)
        {
            if (!IsAlive || amount <= 0) return;
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            HitCount++; LastDamage = amount; LastHitTime = Time.time;
            FeedbackEffects.Burst(point, false);
        }
    }
}