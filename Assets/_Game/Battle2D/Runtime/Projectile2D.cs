using UnityEngine;
namespace GrandNavigation.Battle2D
{
    [RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D))]
    public sealed class Projectile2D : MonoBehaviour
    {
        public float radius=.45f;
        public bool Resolved {get;private set;}
        ShipActor2D owner;Vector2 velocity;float remaining;int damage;Rigidbody2D body;
        public void Launch(ShipActor2D source,Vector2 speed,int amount,float lifetime)
        {
            owner=source;velocity=speed;damage=amount;remaining=lifetime;body=GetComponent<Rigidbody2D>();
            body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;GetComponent<CircleCollider2D>().isTrigger=true;
        }
        void FixedUpdate()
        {
            if(Resolved||!body)return;
            Vector2 start=body.position,delta=velocity*Time.fixedDeltaTime;
            RaycastHit2D nearest=default;
            foreach(var hit in Physics2D.CircleCastAll(start,radius,delta.normalized,delta.magnitude))
            {
                if(hit.collider.isTrigger||hit.collider.GetComponentInParent<ShipActor2D>()==owner)continue;
                if(!nearest.collider||hit.distance<nearest.distance)nearest=hit;
            }
            if(nearest.collider)
            {
                Resolved=true;var ship=nearest.collider.GetComponentInParent<ShipActor2D>();
                if(ship&&ship.Alive&&ship!=owner&&ship.team!=owner.team)ship.TakeDamage(damage);
                Destroy(gameObject);return;
            }
            body.MovePosition(start+delta);remaining-=Time.fixedDeltaTime;
            if(remaining<=0||Mathf.Abs(start.x)>230||Mathf.Abs(start.y)>230){Resolved=true;Destroy(gameObject);}
        }
    }
}
