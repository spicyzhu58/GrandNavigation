using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class CombatEffects : MonoBehaviour
    {
        public Material flashMaterial,splashMaterial;
        [Min(.1f)] public float splashLifetime=1.5f;
        static CombatEffects current;
        void Awake()=>current=this;
        void OnDestroy(){if(current==this)current=null;}
        public static void Flash(Vector3 position,Color color,float size,float duration)
        {
            if(!current)return;var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Impact flash";Destroy(go.GetComponent<Collider>());
            go.transform.position=position;go.transform.localScale=Vector3.one*size;
            go.GetComponent<Renderer>().sharedMaterial=current.flashMaterial;Destroy(go,duration);
        }
        public static void Splash(Vector3 p)
        {
            if(!current)return;var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name="Water column";Destroy(go.GetComponent<Collider>());
            go.transform.position=p+Vector3.up*1.8f;go.transform.localScale=new Vector3(.65f,1.8f,.65f);go.GetComponent<Renderer>().sharedMaterial=current.splashMaterial;
            Destroy(go,current.splashLifetime);
        }
        public static void Hit(Vector3 p,int damage)
        {
            Flash(p,Color.yellow,2,.3f);if(!current)return;
            var go=new GameObject("Damage number");go.transform.position=p+Vector3.up*4;var t=go.AddComponent<TextMesh>();t.text=damage.ToString();t.characterSize=.6f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.yellow;
            go.AddComponent<DamageBillboard>();Destroy(go,1.2f);
        }
    }
    public sealed class DamageBillboard : MonoBehaviour
    {
        void LateUpdate(){if(Camera.main)transform.rotation=Camera.main.transform.rotation;transform.position+=Vector3.up*Time.deltaTime;}
    }
}
