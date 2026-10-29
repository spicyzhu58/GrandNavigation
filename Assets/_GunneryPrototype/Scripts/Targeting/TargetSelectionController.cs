using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class TargetSelectionController : MonoBehaviour
    {
        public Health LockedTarget { get; private set; }
        public event System.Action SelectionChanged;
        public bool HasTarget => LockedTarget && LockedTarget.IsAlive;
        public Vector3 TargetPosition => HasTarget ? LockedTarget.transform.position : Vector3.zero;
        public void Select(Health target)
        {
            Health next=target && target.IsAlive ? target : null;
            if(LockedTarget==next) return;
            LockedTarget=next; SelectionChanged?.Invoke();
        }
        public bool SelectRay(Ray ray)
        {
            if(Physics.Raycast(ray,out var hit,1000,~0,QueryTriggerInteraction.Ignore))
            {
                var target=hit.collider.GetComponentInParent<Health>();
                if(target && target.IsAlive) { Select(target); return true; }
            }
            return false;
        }
        public void Clear() { LockedTarget=null; SelectionChanged?.Invoke(); }
        void Update() { if(LockedTarget && !LockedTarget.IsAlive) Clear(); }
    }
}