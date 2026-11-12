using UnityEngine;
using GrandNavigation.Gameplay;
namespace GrandNavigation.Battle2D
{
    public sealed class Weapon2D : MonoBehaviour
    {
        public Projectile2D projectile;
        public float speed=100,range=300,reload=3,scatterSigma=1.3f;
        public int damage=25,salvo=6;
        public bool CombatEnabled=true;
        public float ReloadRemaining=>Mathf.Max(0,readyAt-Time.time);
        public int LastSalvo {get;private set;}
        ShipActor2D ship;float readyAt;readonly System.Random random=new();
        void Awake(){ship=GetComponent<ShipActor2D>();}
        public bool Fire(Vector2 point)
        {
            LastSalvo=0;
            if(!enabled||!CombatEnabled||!ship.Alive||!projectile||ReloadRemaining>0)return false;
            Vector2 origin=(Vector2)transform.position+(Vector2)transform.up*3;
            if((point-origin).sqrMagnitude<1||(point-origin).magnitude>range)return false;
            for(int i=0;i<Mathf.Clamp(salvo,1,6);i++)
            {
                Vector2 offset=GunneryMath.Gaussian(random,Mathf.Max(0,scatterSigma));
                Vector2 direction=(point+offset-origin).normalized;
                var shell=Instantiate(projectile,origin,Quaternion.identity);
                shell.Launch(ship,direction*speed+ship.Movement.Body.linearVelocity,damage,range/speed);LastSalvo++;
            }
            readyAt=Time.time+Mathf.Max(.1f,reload);return true;
        }
    }
}
