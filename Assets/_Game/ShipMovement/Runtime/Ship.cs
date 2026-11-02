using UnityEngine;
namespace GrandNavigation.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthSystem),typeof(Weapon),typeof(ShipMovement))]
    public class Ship : MonoBehaviour, IDamageable
    {
        [SerializeField] HealthSystem healthSystem;
        [SerializeField] Weapon weapon;
        public HealthSystem Health => healthSystem;
        public bool IsAlive => healthSystem && healthSystem.IsAlive;
        [Tooltip("Ships with the same team cannot damage each other by default.")] public int team;
        public Weapon Weapon => weapon;
        public ShipMovement Movement { get; private set; }
        public Vector3 WorldVelocity => Movement?Movement.WorldVelocity:Vector3.zero;
        public Vector3 WorldPosition => transform.position;
        public Vector3 WorldForward => transform.forward;
        protected virtual void Awake()
        {
            if(!healthSystem)healthSystem=GetComponent<HealthSystem>();
            if(!weapon)weapon=GetComponent<Weapon>();
            Movement=GetComponent<ShipMovement>();
        }
        protected virtual void Reset() { healthSystem=GetComponent<HealthSystem>();weapon=GetComponent<Weapon>(); }
        public event System.Action<int, Ship> Damaged;
        public void TakeDamage(int damage) => TakeDamage(damage, null);
        // Optional source preserves IDamageable and existing anonymous damage callers.
        public void TakeDamage(int damage, Ship source)
        {
            if (!healthSystem || !IsAlive || damage <= 0) return;
            int before = healthSystem.CurrentHealth;
            healthSystem.TakeDamage(damage);
            Damaged?.Invoke(before - healthSystem.CurrentHealth, source);
        }
    }
}
