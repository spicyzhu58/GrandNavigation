using UnityEngine;
namespace GrandNavigation.Gameplay.Levels
{
    [ExecuteAlways] public sealed class LevelIsland : MonoBehaviour
    {
        public Vector3 size=new Vector3(220,35,140);
        public Transform solid;
        // Scene transforms are authoritative. Entering Play never repositions geometry.
        void OnEnable()=>ApplySize();
        void OnValidate()=>Apply();
        bool ValidSolid()=>solid&&solid!=transform&&solid.parent==transform;
        void ApplySize()
        {
            size=new Vector3(Mathf.Max(1,size.x),Mathf.Max(1,size.y),Mathf.Max(1,size.z));
            if(ValidSolid())solid.localScale=size;
        }
        public void Apply()
        {
            ApplySize();
            if(Application.isPlaying||!ValidSolid())return;
            // Height controls the vertical centre only; never erase authored horizontal positions.
            Vector3 position=solid.localPosition;position.y=size.y*.5f;
            if(solid.localPosition!=position)solid.localPosition=position;
        }
    }
}
