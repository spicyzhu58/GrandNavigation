using System.Collections;
using UnityEngine;
namespace GrandNavigation.MainMenuV2
{
    public sealed class MenuCameraRig : MonoBehaviour
    {
        public Camera view;
        public Transform pivot;
        public MenuCameraPose menuPose,preparationPose;
        [Min(.1f)] public float duration=3.5f;
        [Min(0)] public float transitionClearance=12;
        public AnimationCurve easing=AnimationCurve.EaseInOut(0,0,1,1);
        public bool IsMoving { get; private set; }
        public void Initialize() => Apply(menuPose,menuPose,0);
        public void Apply(MenuCameraPose from,MenuCameraPose to,float progress)
        {
            float t=Mathf.Clamp01(easing.Evaluate(Mathf.Clamp01(progress)));
            Vector3 a=from.transform.position-pivot.position,b=to.transform.position-pivot.position;
            float angle=Mathf.LerpAngle(Mathf.Atan2(a.x,a.z)*Mathf.Rad2Deg,Mathf.Atan2(b.x,b.z)*Mathf.Rad2Deg,t)*Mathf.Deg2Rad;
            float radius=Mathf.Lerp(new Vector2(a.x,a.z).magnitude,new Vector2(b.x,b.z).magnitude,t)+Mathf.Sin(t*Mathf.PI)*transitionClearance;
            Vector3 position=pivot.position+new Vector3(Mathf.Sin(angle)*radius,Mathf.Lerp(a.y,b.y,t),Mathf.Cos(angle)*radius);
            Vector3 target=Vector3.Lerp(from.lookTarget.position,to.lookTarget.position,t);
            Quaternion offsetA=Quaternion.Inverse(from.LookRotation)*from.transform.rotation;
            Quaternion offsetB=Quaternion.Inverse(to.LookRotation)*to.transform.rotation;
            view.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position,Vector3.up)*Quaternion.Slerp(offsetA,offsetB,t));
            view.fieldOfView=Mathf.Lerp(from.fieldOfView,to.fieldOfView,t);
        }
        public IEnumerator Move(MenuCameraPose from,MenuCameraPose to)
        {
            IsMoving=true;
            float elapsed=0,seconds=Mathf.Max(.1f,duration);
            while(elapsed<seconds)
            {
                // Bound recovery frames so refocusing the editor cannot skip the orbit.
                elapsed+=Mathf.Min(Time.unscaledDeltaTime,1f/15f);Apply(from,to,elapsed/seconds);yield return null;
            }
            Apply(from,to,1);IsMoving=false;
        }
    }
}


