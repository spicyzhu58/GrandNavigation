using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class FeedbackEffects : MonoBehaviour
    {
        float lifetime, age;
        Vector3 initialScale, drift;
        public static void Puff(Vector3 position, Color color, Vector3 scale, float duration, Vector3 velocity)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Transient feedback";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position; go.transform.localScale = scale;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader); material.color = color;
            go.GetComponent<Renderer>().material = material;
            var effect = go.AddComponent<FeedbackEffects>();
            effect.lifetime = duration; effect.initialScale = scale; effect.drift = velocity;
        }
        public static void Burst(Vector3 position, bool water)
        {
            CombatAudio.PlayImpact(water);
            Puff(position + Vector3.up * .4f, water ? new Color(.6f,.9f,1) : new Color(1,.45f,.08f),
                water ? new Vector3(1,5,1) : Vector3.one * 2, water ? .7f : .25f, Vector3.up * 2);
            if (!water) Puff(position, Color.gray, Vector3.one * 1.5f, 1.2f, Vector3.up * 3);
        }
        public static void Muzzle(Vector3 position)
        {
            Puff(position, new Color(1,.8f,.2f), Vector3.one, .12f, Vector3.zero);
            Puff(position, new Color(.45f,.45f,.45f), Vector3.one, .8f, Vector3.up * 2);
        }
        void Update()
        {
            age += Time.deltaTime;
            transform.position += drift * Time.deltaTime;
            transform.localScale = initialScale * (1 + age / lifetime) * Mathf.Clamp01((lifetime-age)*6);
            if (age >= lifetime) Destroy(gameObject);
        }
        void OnDestroy() { var r=GetComponent<Renderer>(); if(r) Destroy(r.sharedMaterial); }
    }
}