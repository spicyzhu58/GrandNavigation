using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class TargetSelectionController : MonoBehaviour
    {
        public Health LockedTarget { get; private set; }
        public bool HasTarget => LockedTarget && LockedTarget.IsAlive;
        public Vector3 TargetPosition => HasTarget ? LockedTarget.transform.position : Vector3.zero;
        public void Select(Health target) => LockedTarget=target && target.IsAlive ? target : null;
        public bool SelectRay(Ray ray)
        {
            if(Physics.Raycast(ray,out var hit,1000,~0,QueryTriggerInteraction.Ignore))
            {
                var target=hit.collider.GetComponentInParent<Health>();
                if(target && target.IsAlive) { Select(target); return true; }
            }
            return false;
        }
        public void Clear() => LockedTarget=null;
        void Update() { if(LockedTarget && !LockedTarget.IsAlive) Clear(); }
    }
}