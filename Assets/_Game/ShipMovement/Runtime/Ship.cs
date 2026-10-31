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
        public void TakeDamage(int damage) { if(healthSystem)healthSystem.TakeDamage(damage); }
    }
}
