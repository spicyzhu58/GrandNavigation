using UnityEngine;
namespace GrandNavigation.Gameplay.Levels
{
    [ExecuteAlways] public sealed class LevelIsland : MonoBehaviour
    {
        public Vector3 size=new Vector3(220,35,140);
        public Transform solid;
        void OnEnable()=>Apply();
        void OnValidate()=>Apply();
        public void Apply()
        {
            size=new Vector3(Mathf.Max(1,size.x),Mathf.Max(1,size.y),Mathf.Max(1,size.z));
            if(!solid)return;
            solid.localPosition=Vector3.up*size.y*.5f;solid.localRotation=Quaternion.identity;solid.localScale=size;
        }
    }
}