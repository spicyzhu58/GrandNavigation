using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class ShipSinking : MonoBehaviour
    {
        public Transform visual;
        [Min(.1f)] public float sinkDuration=3;
        Ship ship;bool sinking;float age;
        void Start(){ship=GetComponent<Ship>();ship.Health.Died+=Begin;}
        void OnDestroy(){if(ship&&ship.Health)ship.Health.Died-=Begin;}
        void Begin()
        {
            sinking=true;ship.Movement.enabled=false;ship.Weapon.enabled=false;
            var input=GetComponent<PlayerShipInput>();if(input)input.enabled=false;
            var body=GetComponent<Rigidbody>();body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
        }
        void Update(){if(!sinking)return;age+=Time.deltaTime;if(visual)visual.localPosition+=Vector3.down*(5/sinkDuration*Time.deltaTime);if(age>=sinkDuration)gameObject.SetActive(false);}
    }
}
