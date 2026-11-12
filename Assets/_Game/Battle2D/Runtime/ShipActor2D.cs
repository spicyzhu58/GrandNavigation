using UnityEngine;
using GrandNavigation.Gameplay;
namespace GrandNavigation.Battle2D
{
    public enum ShipKind2D { Player, Battleship, Destroyer }
    [RequireComponent(typeof(HealthSystem),typeof(ShipMovement2D))]
    public sealed class ShipActor2D : MonoBehaviour, IDamageable
    {
        public ShipKind2D kind;
        public int team;
        public HealthSystem Health {get;private set;}
        public ShipMovement2D Movement {get;private set;}
        public bool Alive=>Health&&Health.IsAlive;
        void Awake(){Health=GetComponent<HealthSystem>();Movement=GetComponent<ShipMovement2D>();}
        void Start(){Health.Died+=Sunk;}
        public void TakeDamage(int damage)=>Health.TakeDamage(damage);
        void Sunk()
        {
            Movement.Stop();Movement.enabled=false;
            var ai=GetComponent<EnemyAI2D>();if(ai)ai.MarkDestroyed();
            var weapon=GetComponent<Weapon2D>();if(weapon)weapon.enabled=false;
            foreach(var c in GetComponentsInChildren<Collider2D>())c.enabled=false;
            foreach(var sprite in GetComponentsInChildren<SpriteRenderer>())sprite.color=new Color(.15f,.18f,.22f,.45f);
        }
        void OnDestroy(){if(Health)Health.Died-=Sunk;}
    }
}
